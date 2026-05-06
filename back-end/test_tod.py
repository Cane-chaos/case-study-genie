from api_agent.services.tod_generator import generate_full_operation

ctx = """Bước 1: Chào khách và gợi ý giúp đỡ
Chào mừng quý khách đến với khách sạn. Em có thể giúp gì được cho quý khách?
Bước 2: Tiếp nhận yêu cầu đăng ký khách sạn
1. Anh/chị đã đặt phòng trước chưa ạ?
2. Cho em xin họ tên và mã số đặt phòng ạ.
3. Em xin xác nhận lại thông tin: anh/chị là …… lưu trú từ hôm nay ngày …… đến ngày …… Anh/chị đặt …… phòng dành cho …… người, loại …… Tổng tiền phòng của anh/chị là …… và đã đặt cọc trước …… đồng. Như vậy có đúng không ạ?
Bước 3: Đăng ký khách sạn
1. Anh/chị vui lòng cung cấp CCCD ạ.
2. Anh/chị vui lòng thanh toán số tiền còn lại là …… triệu nhé.
3. Đây là phiếu đăng ký khách sạn, anh/chị đọc lại thông tin trên phiếu và ký xác nhận vào đây nhé.
Bước 4: Giao chìa khóa
Đây là thẻ chìa khoá, bên trong có chìa khoá phòng và số phòng của anh/chị là …… Giá phòng đã bao gồm ăn sáng tại nhà hàng nằm ở tầng 2 phục vụ từ 6h-9h ạ.
Bước 5: Giới thiệu các dịch vụ khác
Ngoài ra, nếu anh/chị muốn sử dụng dịch vụ nào có thể liên hệ với lễ tân thông qua SĐT là 001 nhé.
Bước 6: Giới thiệu nhân viên hành lý
- Đây là anh Henry sẽ giúp anh/chị mang hành lý lên phòng.
- Em có thể giúp gì thêm cho anh/chị được nữa không ạ? Chúc anh/chị có một kỳ nghỉ vui vẻ tại khách sạn. Mời anh/chị lên phòng.
Bước 7: Hoàn thiện hồ sơ đăng ký"""

generate_full_operation("Check-in", context=ctx)
