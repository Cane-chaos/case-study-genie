# CaseStudy Unity M4

Tổng quan dự án (Unity, UI Toolkit + C#):

- `_Project/` — thư mục nguồn chính (tránh nhầm với plugin tải về).
  - `Scripts/`
    - `Core/`: Các Manager điều phối luồng chính (SimulationManager, EventManager, v.v.).
    - `UI/`: Controller giao diện (UI Toolkit, events, binding).
    - `Models/`: Các lớp dữ liệu (CaseStudyData, PersonaConfig...).
    - `Networking/`: Gọi API tới Python Backend.
  - `UI/`
    - `Documents/`: File `.uxml` định nghĩa layout giao diện.
    - `Styles/`: File `.uss` (CSS cho Unity).
  - `Prefabs/`
    - `Environments/`: Sảnh khách sạn, phòng mẫu.
    - `Characters/`: Mr. Viktor và các nhân vật khác.

Hướng dẫn nhanh:
- Mở scene chính, chạy Play: `SimulationManager` giữ trạng thái và phát event qua `EventManager` cho UI.
- Thêm UI: tạo `.uxml` trong `UI/Documents`, style với `.uss` trong `UI/Styles`, và controller C# trong `Scripts/UI`.
- Dữ liệu case: định nghĩa trong `Scripts/Models`, load/parse rồi cấp cho UI.
- Kết nối backend: đặt mã gọi API tại `Scripts/Networking` (REST/gRPC tuỳ backend Python).

Ghi chú:
- Giữ đúng cấu trúc thư mục để tránh xung đột khi nhập asset/plugin.
- Prefabs môi trường/nhân vật nên tham chiếu script và style qua UI Toolkit thay vì hardcode.
