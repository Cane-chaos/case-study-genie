from fastapi import APIRouter, BackgroundTasks, UploadFile, File, HTTPException
from pydantic import BaseModel
import os
import uuid
from api_agent.services.tod_generator import generate_full_operation, expand_and_save_nodes, parse_operations

router = APIRouter(prefix="/admin", tags=["Admin"])

class CaseGenerationRequest(BaseModel):
    operation: str
    context: str = ""

@router.post("/generate-cases")
async def api_generate_case(req: CaseGenerationRequest, background_tasks: BackgroundTasks):
    """
    API dùng cho Admin để sinh thêm Case Study bằng Tree of Debate.
    Sử dụng BackgroundTasks để chạy nền.
    """
    def background_generation(op: str, ctx: str):
        print(f"Bắt đầu sinh cây kịch bản Multi-Agent cho '{op}'...")
        generate_full_operation(op, ctx)
        print(f"Đã sinh xong chuỗi kịch bản cho '{op}'.")

    background_tasks.add_task(background_generation, req.operation, req.context)
    
    return {"message": f"Hệ thống đang tiến hành sinh Case cho '{req.operation}' ở dưới nền..."}

@router.get("/viewer/operations")
async def viewer_get_operations():
    """
    Lấy danh sách các thư mục chứa kịch bản (operations slug).
    """
    data_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), "data")
    if not os.path.exists(data_dir):
        return {"operations": []}
        
    ops = [d for d in os.listdir(data_dir) if os.path.isdir(os.path.join(data_dir, d))]
    return {"operations": ops}

@router.get("/viewer/tree/{slug}")
async def viewer_get_tree(slug: str):
    """
    Lấy toàn bộ các node (file JSON) trong thư mục {slug}.
    """
    data_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), "data", slug)
    if not os.path.exists(data_dir):
        raise HTTPException(status_code=404, detail=f"Không tìm thấy dữ liệu cho {slug}")
        
    nodes = []
    import json
    for filename in os.listdir(data_dir):
        if filename.endswith(".json"):
            filepath = os.path.join(data_dir, filename)
            try:
                with open(filepath, "r", encoding="utf-8") as f:
                    node_data = json.load(f)
                    nodes.append(node_data)
            except Exception as e:
                print(f"Error loading {filename}: {e}")
                
    return {"nodes": nodes}

@router.post("/generate-from-file")
async def generate_from_file(background_tasks: BackgroundTasks, file: UploadFile = File(...)):
    """
    API dùng cho Admin để upload file PDF hoặc TXT.
    Hệ thống sẽ bóc tách danh sách nghiệp vụ và tự động chạy Tree of Debate cho từng nghiệp vụ.
    """
    os.makedirs("data", exist_ok=True)
    temp_path = f"data/temp_{uuid.uuid4().hex[:6]}_{file.filename}"
    
    # Đọc và lưu file tạm
    content = await file.read()
    with open(temp_path, "wb") as f:
        f.write(content)
        
    def background_file_processing(filepath: str, filename: str):
        print(f"Bắt đầu xử lý file: {filename}")
        
        # Đọc nội dung file (cơ bản hỗ trợ txt, với PDF thì cài thêm pypdf)
        text_content = ""
        if filename.endswith(".txt"):
            with open(filepath, "r", encoding="utf-8") as f:
                text_content = f.read()
        elif filename.endswith(".pdf"):
            try:
                import pypdf
                reader = pypdf.PdfReader(filepath)
                text_content = "\n".join(page.extract_text() for page in reader.pages if page.extract_text())
            except ImportError:
                print("Lỗi: Chưa cài đặt thư viện pypdf. Vui lòng chạy: uv pip install pypdf")
                return
            except Exception as e:
                print(f"Lỗi khi đọc PDF: {e}")
                return
        
        if not text_content:
            print("Không đọc được nội dung từ file.")
            return
            
        # Trích xuất danh sách nghiệp vụ
        operations = parse_operations(text_content)
        print(f"Tìm thấy {len(operations)} nghiệp vụ: {operations}")
        
        # Chạy sinh kịch bản cho từng nghiệp vụ
        for op in operations:
            print(f"Đang sinh chuỗi kịch bản cho: {op}")
            generate_full_operation(op)
                
        # Xóa file tạm
        try:
            os.remove(filepath)
        except:
            pass
            
        print("Hoàn tất quá trình sinh dữ liệu từ file!")

    background_tasks.add_task(background_file_processing, temp_path, file.filename)
    
    return {
        "message": f"Đã nhận file {file.filename}.",
        "detail": "Hệ thống đang đọc file, trích xuất nghiệp vụ và sinh Case Study ở dưới nền."
    }
