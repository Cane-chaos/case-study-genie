# Unity API Guide

## Overview

Unity should talk to the backend using a per-session `session_id`. The backend keeps conversation state across turns, so each Unity conversation must reuse the same `session_id`.

Use `scenario_id` to select the case tree, for example: `check_in/scenario_1`.

## Text Chat

`POST /api/game/chat`

Request body:

```json
{
  "session_id": "session_1",
  "scenario_id": "check_in/scenario_1",
  "user_message": "Không được, phải có CCCD mới được check-in",
  "include_audio": false,
  "voice": null
}
```

Response fields:

- `reply`: text reply from the persona
- `current_node_id`: current case node
- `previous_node_id`: previous case node, if a transition happened
- `has_transitioned`: `true` when the backend moved to a new node
- `tension_level`: float from `0.0` to `1.0`
- `emotion`: one of `Idle`, `Agree`, `Angry`
- `is_terminal`: `true` when the case has ended
- `ending_type`: `none`, `good`, or `bad`
- `end_message`: terminal explanation message, if available

## Speech REST

`POST /api/game/chat-audio`

Form-data fields:

- `session_id`
- `scenario_id`
- `audio` file
- `include_audio` optional, default `true`
- `voice` optional

Backend flow:

1. Transcribe user audio to text.
2. Run the game logic.
3. Return text reply, emotion, and persona audio.

Response also includes:

- `transcript`
- `audio.format`
- `audio.mime_type`
- `audio.base64`

## WebSocket Streaming

`WS /api/game/ws`

Client events:

- `{"type":"start","session_id":"...","scenario_id":"...","include_audio":true}`
- `{"type":"user_text","text":"..."}`
- `{"type":"audio_start"}`
- binary audio chunks
- `{"type":"audio_end","filename":"input.webm","content_type":"audio/webm"}`

Server events:

- `ready`
- `transcript`
- `reply_text`
- `state`
- `reply_audio_start`
- `reply_audio_chunk`
- `reply_audio_end`
- `done`

## Unity Rules

- Do not hardcode `__global_service_refusal`.
- Stop the conversation when `is_terminal = true`.
- Use `emotion` to drive the face/animation state:
  - `Idle`: neutral
  - `Agree`: positive/cooperative
  - `Angry`: upset or defensive
- Use `tension_level` for intensity if needed.

## Typical Flow

1. Call `/api/game/start` with `scenario_id`.
2. Send user text or audio.
3. Read `reply`, `emotion`, `current_node_id`, and `is_terminal`.
4. If terminal, switch to scoring or end screen.

## Unity C# Examples

### 1. Text Chat With `UnityWebRequest`

```csharp
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ChatRequest
{
    public string session_id;
    public string scenario_id;
    public string user_message;
    public bool include_audio = false;
    public string voice = null;
}

public static class GameApiClient
{
    private const string BaseUrl = "http://localhost:9000/api/game";

    public static async Task<string> SendTextAsync(string sessionId, string scenarioId, string message)
    {
        var body = new ChatRequest
        {
            session_id = sessionId,
            scenario_id = scenarioId,
            user_message = message,
            include_audio = false
        };

        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest($"{BaseUrl}/chat", "POST");
        byte[] payload = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(payload);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception(req.error);

        return req.downloadHandler.text;
    }
}
```

### 2. Audio REST With `UnityWebRequest`

Upload a recorded file as `multipart/form-data`.

```csharp
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class GameAudioUploader : MonoBehaviour
{
    public IEnumerator SendAudio(string sessionId, string scenarioId, string filePath)
    {
        byte[] audioBytes = File.ReadAllBytes(filePath);
        var form = new WWWForm();
        form.AddField("session_id", sessionId);
        form.AddField("scenario_id", scenarioId);
        form.AddField("include_audio", "true");
        form.AddBinaryData("audio", audioBytes, "input.webm", "audio/webm");

        using var req = UnityWebRequest.Post("http://localhost:9000/api/game/chat-audio", form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }

        Debug.Log(req.downloadHandler.text);
    }
}
```

### 3. WebSocket Streaming

Use a Unity WebSocket package such as `NativeWebSocket`. The backend event names are fixed, so the client just needs to send JSON and receive JSON chunks.

```csharp
using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;

public class GameSocketClient : MonoBehaviour
{
    private WebSocket ws;

    private async void Start()
    {
        ws = new WebSocket("ws://localhost:9000/api/game/ws");

        ws.OnOpen += () =>
        {
            ws.SendText(JsonUtility.ToJson(new StartEvent
            {
                type = "start",
                session_id = "session_1",
                scenario_id = "check_in/scenario_1",
                include_audio = true
            }));
        };

        ws.OnMessage += (bytes) =>
        {
            string json = Encoding.UTF8.GetString(bytes);
            Debug.Log(json);
        };

        ws.OnError += (e) => Debug.LogError(e);
        ws.OnClose += (e) => Debug.Log("WebSocket closed");

        await ws.Connect();
    }

    [Serializable]
    public class StartEvent
    {
        public string type;
        public string session_id;
        public string scenario_id;
        public bool include_audio;
    }

    [Serializable]
    public class TextEvent
    {
        public string type = "user_text";
        public string text;
    }

    public async void SendUserText(string text)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        await ws.SendText(JsonUtility.ToJson(new TextEvent { text = text }));
    }

    private async void OnApplicationQuit()
    {
        if (ws != null) await ws.Close();
    }
}
```

### 4. WebSocket Audio Flow

1. Send `{"type":"audio_start"}`.
2. Stream binary microphone chunks to the socket.
3. Send `{"type":"audio_end","filename":"input.webm","content_type":"audio/webm"}`.
4. Listen for:
   - `transcript`
   - `reply_text`
   - `reply_audio_start`
   - `reply_audio_chunk`
   - `reply_audio_end`
   - `state`
   - `done`

## Notes For Unity

- Use `emotion` for visual state changes.
- Use `tension_level` for intensity.
- Use `is_terminal` to stop interaction and move to scoring.
- Do not depend on node IDs for terminal detection.
