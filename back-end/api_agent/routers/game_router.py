import asyncio
import base64
import json
import os
from typing import Optional

from fastapi import APIRouter, Depends, File, Form, HTTPException, Query, UploadFile, WebSocket, WebSocketDisconnect
from pydantic import BaseModel
from langchain_core.messages import HumanMessage
from openai import OpenAI
from api_agent.dependencies.deps import get_state_loader
from api_agent.services.state_loader import StateLoader
from api_agent.services.game_agent import game_app
from api_agent.services.scoring_service import evaluate_session

router = APIRouter(prefix="/game", tags=["Game"])

try:
    audio_client = OpenAI()
except Exception as e:
    print(f"Lỗi khởi tạo OpenAI audio client: {e}")
    audio_client = None

class ChatRequest(BaseModel):
    session_id: str
    user_message: str
    scenario_id: str
    include_audio: bool = False
    voice: Optional[str] = None

def emotion_from_state(tension_level: float, ending_type: str, reply: str = "") -> str:
    """
    Unity chỉ nhận 1 trong 3 emotion: Idle, Agree, Angry.
    """
    reply_lower = reply.lower()
    if ending_type == "bad" or tension_level >= 0.7:
        return "Angry"
    if ending_type == "good":
        return "Agree"
    positive_markers = ("cảm ơn", "đồng ý", "được rồi", "tốt quá", "vâng", "ok")
    if tension_level <= 0.4 and any(marker in reply_lower for marker in positive_markers):
        return "Agree"
    return "Idle"

def voice_instructions_for_emotion(emotion: str) -> str:
    if emotion == "Angry":
        return "Nói tiếng Việt với giọng nam trưởng thành, bực bội, ngắn gọn, dằn giọng nhưng không la hét."
    if emotion == "Agree":
        return "Nói tiếng Việt với giọng nam trưởng thành, nhẹ nhõm, biết ơn, tốc độ vừa phải."
    return "Nói tiếng Việt với giọng nam trưởng thành, tự nhiên, hơi mệt và lịch sự."

def synthesize_reply_audio(reply: str, emotion: str, voice: Optional[str] = None) -> dict:
    if not audio_client:
        raise HTTPException(status_code=500, detail="OpenAI audio client chưa được cấu hình.")
    if not reply:
        return {"format": "mp3", "mime_type": "audio/mpeg", "base64": ""}

    speech = audio_client.audio.speech.create(
        model=os.getenv("OPENAI_TTS_MODEL", "gpt-4o-mini-tts"),
        voice=voice or os.getenv("OPENAI_TTS_VOICE", "alloy"),
        input=reply,
        response_format="mp3",
        instructions=voice_instructions_for_emotion(emotion),
    )
    audio_bytes = speech.read()
    return {
        "format": "mp3",
        "mime_type": "audio/mpeg",
        "base64": base64.b64encode(audio_bytes).decode("ascii")
    }

def iter_reply_audio_chunks(reply: str, emotion: str, voice: Optional[str] = None):
    if not audio_client:
        raise HTTPException(status_code=500, detail="OpenAI audio client chưa được cấu hình.")
    if not reply:
        return

    with audio_client.audio.speech.with_streaming_response.create(
        model=os.getenv("OPENAI_TTS_MODEL", "gpt-4o-mini-tts"),
        voice=voice or os.getenv("OPENAI_TTS_VOICE", "alloy"),
        input=reply,
        response_format="mp3",
        instructions=voice_instructions_for_emotion(emotion),
    ) as response:
        for chunk in response.iter_bytes(chunk_size=8192):
            if chunk:
                yield chunk

def transcribe_audio_bytes(audio_bytes: bytes, filename: str, content_type: str = "audio/webm") -> str:
    if not audio_client:
        raise HTTPException(status_code=500, detail="OpenAI audio client chưa được cấu hình.")
    if not audio_bytes:
        raise HTTPException(status_code=400, detail="Không nhận được dữ liệu audio.")

    transcript = audio_client.audio.transcriptions.create(
        model=os.getenv("OPENAI_STT_MODEL", "gpt-4o-mini-transcribe"),
        file=(filename, audio_bytes, content_type),
        language="vi",
    )
    if isinstance(transcript, str):
        return transcript
    return getattr(transcript, "text", str(transcript))

def build_chat_payload(output_state: dict, current_state: dict, include_audio: bool = False, voice: Optional[str] = None) -> dict:
    messages = output_state.get("messages", [])
    agent_reply = ""
    for msg in reversed(messages):
        if msg.type == "ai" and not msg.content.startswith("[SUY NGHĨ NỘI TÂM]"):
            agent_reply = msg.content
            break

    old_node_id = output_state.get("previous_node_id") or (current_state.get("current_node_id") if current_state else None)
    new_node_id = output_state.get("current_node_id")
    has_transitioned = old_node_id != new_node_id if old_node_id else False
    case_data = output_state.get("case_data", {})
    agent_init = case_data.get("agent_init", {})
    ending_type = agent_init.get("ending_type", "none")
    is_terminal = ending_type in {"good", "bad"}
    tension_level = output_state.get("tension_level") or 0.0
    emotion = emotion_from_state(tension_level, ending_type, agent_reply)

    payload = {
        "reply": agent_reply,
        "current_node_id": new_node_id,
        "previous_node_id": old_node_id,
        "has_transitioned": has_transitioned,
        "tension_level": tension_level,
        "emotion": emotion,
        "is_terminal": is_terminal,
        "ending_type": ending_type,
        "end_message": case_data.get("metadata", {}).get("end_message")
    }
    if include_audio:
        payload["audio"] = synthesize_reply_audio(agent_reply, emotion, voice)
    return payload

def run_game_turn(session_id: str, scenario_id: str, user_message: str, loader: StateLoader, include_audio: bool = False, voice: Optional[str] = None) -> dict:
    config = {"configurable": {"thread_id": session_id}}
    try:
        current_state = game_app.get_state(config).values
    except Exception:
        current_state = {}

    if not current_state.get("case_data"):
        case_data = loader.fetch_scenario_node(scenario_id, "Root")
        if not case_data:
            raise HTTPException(status_code=404, detail="Không tìm thấy kịch bản.")
        inputs = {
            "messages": [HumanMessage(content=user_message)],
            "scenario_id": scenario_id,
            "current_node_id": case_data.get("_id", "Root"),
            "previous_node_id": None,
            "case_data": case_data,
            "next_node_id": None,
            "tension_level": case_data.get("agent_init", {}).get("base_tension", 0.0)
        }
    else:
        inputs = {"messages": [HumanMessage(content=user_message)]}

    output_state = game_app.invoke(inputs, config=config)
    return build_chat_payload(output_state, current_state, include_audio, voice)

@router.get("/start")
async def start_game(scenario_id: str = Query(..., description="ID kịch bản (VD: check_in/easy)"), loader: StateLoader = Depends(get_state_loader)):
    """
    Khi User vào game, Unity sẽ gọi API này để lấy dữ liệu dựng hình và thông tin ban đầu.
    """
    case_data = loader.fetch_scenario_node(scenario_id=scenario_id, node_id="Root")
    if not case_data:
        raise HTTPException(status_code=404, detail="Không tìm thấy kịch bản nào trong hệ thống.")
        
    unity_payload = loader.get_unity_init(case_data)
    return unity_payload

@router.get("/agent-init")
async def get_agent_init(scenario_id: str = Query(...), node_id: str = Query("Root"), loader: StateLoader = Depends(get_state_loader)):
    """
    API nội bộ (hoặc dùng lúc khởi tạo WebSocket) để lấy dữ liệu agent_init nạp vào LangGraph.
    """
    case_data = loader.fetch_scenario_node(scenario_id=scenario_id, node_id=node_id)
    if not case_data:
        raise HTTPException(status_code=404, detail="Không tìm thấy kịch bản.")
        
    agent_payload = loader.get_agent_init(case_data)
    return agent_payload

@router.post("/chat")
async def chat_with_agent(req: ChatRequest, loader: StateLoader = Depends(get_state_loader)):
    """
    API để User trò chuyện với Agent trong game.
    Sử dụng LangGraph StateGraph để giữ ngữ cảnh.
    """
    return run_game_turn(
        req.session_id,
        req.scenario_id,
        req.user_message,
        loader,
        include_audio=req.include_audio,
        voice=req.voice,
    )

@router.post("/chat-audio")
async def chat_with_agent_audio(
    session_id: str = Form(...),
    scenario_id: str = Form(...),
    audio: UploadFile = File(...),
    include_audio: bool = Form(True),
    voice: Optional[str] = Form(None),
    loader: StateLoader = Depends(get_state_loader),
):
    """
    Unity gửi giọng nói của lễ tân lên. Backend transcribe -> chạy game -> trả text + emotion + audio của persona.
    """
    audio_bytes = await audio.read()
    transcript = await asyncio.to_thread(
        transcribe_audio_bytes,
        audio_bytes,
        audio.filename or "unity_audio.webm",
        audio.content_type or "audio/webm",
    )
    payload = await asyncio.to_thread(
        run_game_turn,
        session_id,
        scenario_id,
        transcript,
        loader,
        include_audio,
        voice,
    )
    payload["transcript"] = transcript
    return payload

@router.websocket("/ws")
async def game_voice_ws(websocket: WebSocket):
    """
    WebSocket listener cho Unity.

    Client events:
    - {"type":"start","session_id":"...","scenario_id":"...","include_audio":true}
    - {"type":"user_text","text":"..."}
    - {"type":"audio_start"}
    - binary audio chunks
    - {"type":"audio_end","filename":"input.webm","content_type":"audio/webm"}

    Server events:
    - transcript, reply_text, reply_audio, state, done, error
    """
    await websocket.accept()
    loader = StateLoader()
    session_id = None
    scenario_id = None
    include_audio = True
    voice = None
    audio_chunks: list[bytes] = []

    async def send_turn(user_text: str):
        if not session_id or not scenario_id:
            await websocket.send_json({"type": "error", "message": "Chưa start session."})
            return
        payload = await asyncio.to_thread(
            run_game_turn,
            session_id,
            scenario_id,
            user_text,
            loader,
            False,
            voice,
        )
        await websocket.send_json({"type": "reply_text", "text": payload["reply"], "emotion": payload["emotion"]})
        await websocket.send_json({
            "type": "state",
            "current_node_id": payload["current_node_id"],
            "previous_node_id": payload["previous_node_id"],
            "has_transitioned": payload["has_transitioned"],
            "tension_level": payload["tension_level"],
            "emotion": payload["emotion"],
            "is_terminal": payload["is_terminal"],
            "ending_type": payload["ending_type"],
            "end_message": payload["end_message"],
        })
        if include_audio and payload.get("reply"):
            await websocket.send_json({
                "type": "reply_audio_start",
                "format": "mp3",
                "mime_type": "audio/mpeg",
                "emotion": payload["emotion"],
            })
            seq = 0
            for chunk in iter_reply_audio_chunks(payload["reply"], payload["emotion"], voice):
                await websocket.send_json({
                    "type": "reply_audio_chunk",
                    "seq": seq,
                    "base64": base64.b64encode(chunk).decode("ascii"),
                })
                seq += 1
            await websocket.send_json({"type": "reply_audio_end", "chunks": seq})
        await websocket.send_json({"type": "done"})

    try:
        while True:
            message = await websocket.receive()
            if message.get("bytes") is not None:
                audio_chunks.append(message["bytes"])
                continue

            raw_text = message.get("text")
            if raw_text is None:
                continue
            data = json.loads(raw_text)
            event_type = data.get("type")

            if event_type == "start":
                session_id = data.get("session_id")
                scenario_id = data.get("scenario_id")
                include_audio = data.get("include_audio", True)
                voice = data.get("voice")
                await websocket.send_json({"type": "ready", "session_id": session_id, "scenario_id": scenario_id})
            elif event_type == "user_text":
                await send_turn(data.get("text", ""))
            elif event_type == "audio_start":
                audio_chunks = []
                await websocket.send_json({"type": "audio_ready"})
            elif event_type == "audio_end":
                audio_bytes = b"".join(audio_chunks)
                audio_chunks = []
                transcript = await asyncio.to_thread(
                    transcribe_audio_bytes,
                    audio_bytes,
                    data.get("filename", "unity_audio.webm"),
                    data.get("content_type", "audio/webm"),
                )
                await websocket.send_json({"type": "transcript", "text": transcript})
                await send_turn(transcript)
            else:
                await websocket.send_json({"type": "error", "message": f"Unknown event type: {event_type}"})
    except WebSocketDisconnect:
        return

@router.get("/score")
async def get_game_score(session_id: str = Query(..., description="ID của session để chấm điểm")):
    """
    Chấm điểm toàn diện cho phiên chơi dựa trên lịch sử LangGraph (chỉ gọi khi game đã kết thúc).
    """
    result = evaluate_session(session_id)
    if "error" in result:
        raise HTTPException(status_code=400, detail=result["error"])
    return result
