# 📘 CASESTUDY ENGINE: QUY TRÌNH PHÁT TRIỂN NHÓM

## 1. NGUYÊN TẮC VÀNG (THE GOLDEN RULES)
- Không chạm vào Core: Chỉ Lead (bạn) mới được sửa các file trong `Scripts/Core/` (SimulationManager, EventManager, UIManager).
- Mỗi màn hình một thế giới: Mỗi Task chỉ làm việc trong đúng file `.uxml`, `.uss` và `Controller.cs` của mình.
- Đặt tên có Prefix: Mọi tài liệu liên quan đến Task phải bắt đầu bằng tên Task (Ví dụ: `Auth_Login.uxml`, `Auth_Controller.cs`).
- AI Prompting: Khi dùng AI hỗ trợ, luôn gửi kèm cấu trúc dự án và yêu cầu AI: "Viết code theo chuẩn UI Toolkit, sử dụng EventManager của dự án".

---

## 2. CẤU TRÚC THƯ MỤC LÀM VIỆC (WORKSPACE)
Mọi thành viên phải tuân thủ tuyệt đối sơ đồ này:

```
Assets/_Project/
├── UI/
│   ├── Documents/      (Chứa .uxml - Nơi thiết kế Layout)
│   ├── Styles/         (Chứa .uss - Nơi thiết kế CSS/Giao diện)
│   └── PanelSettings/  (Chung cho cả dự án)
├── Scripts/
│   ├── Core/           (KHÔNG TỰ Ý SỬA - Lead Only)
│   ├── Controllers/    (Nơi mỗi thành viên viết Logic cho Task của mình)
│   ├── Models/         (Định nghĩa dữ liệu JSON)
│   └── Networking/     (Các hàm gọi API dùng chung)
└── Prefabs/            (Mỗi màn hình đóng gói thành 1 Prefab để kéo vào Scene)
```

---

## 3. CHI TIẾT CÁC TASK (6 MODULES)

**Task 1: Home & Authentication (Cổng vào)**
- Mục tiêu: Tạo màn hình Login/Register, kết nối MongoDB qua FastAPI.
- File làm việc: `Auth_UI.uxml`, `Auth_Controller.cs`.
- Logic: Thu thập User/Pass -> Gửi API -> Nếu thành công, gọi `SimulationManager.Instance.ChangeState(SimulationState.Library)`.

**Task 2: Asset Library (Phòng trưng bày)**
- Mục tiêu: Hiển thị danh sách 3D Asset (Env & Persona) có trong Resources.
- File làm việc: `Library_UI.uxml`, `Library_Controller.cs`.
- Logic: Quét thư mục Resources -> Sinh ra các thẻ (Card) hiển thị tên/ảnh Asset -> Click vào để xem chi tiết.

**Task 3: AI Architect Creator (Xây dựng bằng Chat)**
- Mục tiêu: Giao diện chat để AI tự động sắp xếp môi trường.
- File làm việc: `AICreator_UI.uxml`, `AICreator_Controller.cs`.
- Logic: Gửi yêu cầu (Prompt) lên Backend -> Nhận danh sách vật thể & tọa độ -> Ra lệnh cho Unity Spawn vật thể theo tọa độ đó.

**Task 4: Case Designer (Kéo thả chuyên nghiệp) - Task khó nhất**
- Mục tiêu: Bộ công cụ thiết kế kịch bản (Chọn Env, đặt Persona, viết tính cách).
- File làm việc: `Designer_UI.uxml`, `Designer_Controller.cs`.
- Logic: Kéo vật thể 3D trong Scene -> Hiện bảng thuộc tính bên phải để nhập Persona Traits (Tính cách AI) -> Nút "Save Case" để đẩy JSON về MongoDB.

**Task 5: Interactive Simulator (Mô phỏng thực tế)**
- Mục tiêu: Chế độ chạy kịch bản, Chat trực tiếp với Agent.
- File làm việc: `Simulator_UI.uxml`, `Simulator_Controller.cs`.
- Logic: Khung chat (Dialogue) -> Gửi tin nhắn lên LangGraph -> Nhận câu trả lời + Lệnh Animation (Viktor giận dữ/vui vẻ) -> Thực thi trong Unity.

---

## 4. QUY TRÌNH LÀM VIỆC VỚI GIT (GITHUB WORKFLOW)
Để không bị lỗi khi gộp code, mỗi thành viên thực hiện theo các bước:

1. Tạo nhánh (Branch) mới: `git checkout -b feature/Task_Auth` (Thay tên Task của bạn).
2. Làm việc: Chỉ tạo file và sửa code trong phạm vi thư mục Task của mình.
3. Commit: `git add .` -> `git commit -m "Hoàn thành UI Login và kết nối API Auth"`.
4. Pull & Merge:
   - Trước khi đẩy code, phải chạy: `git pull origin main` để cập nhật code mới nhất của người khác.
   - Sau đó: `git push origin feature/Task_Auth`.
5. Tạo Pull Request (PR): Gửi yêu cầu lên GitHub để Lead (bạn) duyệt và gộp vào bản chính.

---

## 5. HƯỚNG DẪN DÙNG AI ĐỂ CODE "KHÔNG LỖI"
Khi yêu cầu AI viết code cho một Task, hãy dùng mẫu Prompt sau:

> "Tôi đang làm Task [Tên Task] trong dự án Unity 6 dùng UI Toolkit. Dự án có SimulationManager quản lý State và EventManager quản lý sự kiện. Hãy viết cho tôi file [Tên_Controller].cs để điều khiển file .uxml có tên là [Tên_UXML]. Đảm bảo code sử dụng RegisterCallback cho các nút bấm và gọi SimulationManager khi cần chuyển trang."
