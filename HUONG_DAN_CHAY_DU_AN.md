# Hướng Dẫn Chạy Dự Án StoreApp Blazor WebAssembly

## Yêu Cầu Hệ Thống

1. **.NET SDK 9.0** hoặc cao hơn
   - Tải tại: https://dotnet.microsoft.com/download
   - Kiểm tra phiên bản: `dotnet --version`

2. **Node.js và npm** (để build Tailwind CSS)
   - Tải tại: https://nodejs.org/
   - Kiểm tra phiên bản: `node --version` và `npm --version`

3. **MySQL Server** (hoặc MariaDB)
   - Tải tại: https://dev.mysql.com/downloads/mysql/
   - Hoặc sử dụng XAMPP/WAMP có sẵn MySQL

4. **IDE** (tùy chọn nhưng khuyến nghị):
   - Visual Studio 2022
   - Visual Studio Code với extension C#
   - Rider

## Các Bước Chạy Dự Án

### Bước 1: Cài Đặt Dependencies

#### 1.1. Cài đặt Node.js packages (cho Tailwind CSS)
```bash
cd StoreApp
npm install
```

#### 1.2. Restore .NET packages
```bash
# Từ thư mục gốc của solution
dotnet restore
```

Hoặc từ Visual Studio: Click chuột phải vào Solution → Restore NuGet Packages

### Bước 2: Cấu Hình Database

#### 2.1. Tạo Database MySQL
Mở MySQL và chạy script SQL để tạo database:
```sql
CREATE DATABASE store_management;
```

Hoặc import file SQL có sẵn:
```bash
mysql -u root -p store_management < StoreApp/Data/store_management_full.sql
```

#### 2.2. Cập nhật Connection String
Mở file `StoreApp/appsettings.json` và kiểm tra connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=store_management;user=root;password=123456;SslMode=None;AllowPublicKeyRetrieval=True;"
  }
}
```

**Lưu ý**: Thay đổi `user`, `password` và `database` theo cấu hình MySQL của bạn.

### Bước 3: Build Tailwind CSS (Nếu cần)

Nếu bạn muốn build CSS trước khi chạy:
```bash
cd StoreApp
npm run css:build
```

Hoặc chạy watch mode để tự động build khi có thay đổi:
```bash
npm run css:watch
```

**Lưu ý**: Dự án đã có cấu hình tự động build CSS khi build .NET project (xem `StoreApp.csproj`).

### Bước 4: Chạy Dự Án

#### Cách 1: Chạy từ Visual Studio
1. Mở file `StoreApp.sln` trong Visual Studio
2. Đặt `StoreApp` làm Startup Project (click chuột phải → Set as Startup Project)
3. Nhấn `F5` hoặc click nút Run

#### Cách 2: Chạy từ Command Line
```bash
# Từ thư mục gốc của solution
cd StoreApp
dotnet run
```

Hoặc với HTTPS:
```bash
dotnet run --launch-profile https
```

### Bước 5: Truy Cập Ứng Dụng

Sau khi chạy thành công, mở trình duyệt và truy cập:

- **HTTP**: http://localhost:5273
- **HTTPS**: https://localhost:7041

**Lưu ý**: Nếu trình duyệt cảnh báo về chứng chỉ SSL, bạn có thể bỏ qua (chỉ trong môi trường Development).

## Cấu Trúc URL

### Phần Admin (Layout mặc định)
- `/` - Trang chủ Admin
- `/dashboard` - Dashboard
- `/products` - Quản lý sản phẩm
- `/categories` - Quản lý danh mục
- `/customers` - Quản lý khách hàng
- `/promotions` - Quản lý khuyến mãi

### Phần Cửa Hàng Khách Hàng (Store)
- `/store` - Trang chủ cửa hàng
- `/store/products` - Danh sách sản phẩm
- `/store/products/{id}` - Chi tiết sản phẩm
- `/store/cart` - Giỏ hàng
- `/store/checkout` - Thanh toán
- `/store/orders` - Đơn hàng của khách hàng
- `/store/orders/{id}` - Chi tiết đơn hàng

## API Documentation (Swagger)

Khi chạy ở môi trường Development, bạn có thể truy cập Swagger UI:
- http://localhost:5273/swagger
- https://localhost:7041/swagger

## Xử Lý Lỗi Thường Gặp

### 1. Lỗi kết nối Database
```
Cannot connect to MySQL server
```
**Giải pháp**:
- Kiểm tra MySQL đã chạy chưa
- Kiểm tra connection string trong `appsettings.json`
- Kiểm tra user/password có đúng không

### 2. Lỗi Tailwind CSS không build
```
npm: command not found
```
**Giải pháp**:
- Cài đặt Node.js và npm
- Chạy `npm install` trong thư mục `StoreApp`

### 3. Lỗi Port đã được sử dụng
```
Address already in use
```
**Giải pháp**:
- Đổi port trong `launchSettings.json`
- Hoặc tắt ứng dụng đang chạy trên port đó

### 4. Lỗi thiếu packages
```
Package not found
```
**Giải pháp**:
- Chạy `dotnet restore`
- Hoặc trong Visual Studio: Tools → NuGet Package Manager → Restore

### 5. Lỗi Blazor WebAssembly không load
```
Failed to load resource
```
**Giải pháp**:
- Xóa thư mục `bin` và `obj` trong tất cả projects
- Chạy `dotnet clean` và `dotnet build` lại
- Xóa cache trình duyệt (Ctrl + Shift + Delete)

## Các Lệnh Hữu Ích

```bash
# Clean solution
dotnet clean

# Build solution
dotnet build

# Restore packages
dotnet restore

# Run với profile cụ thể
dotnet run --launch-profile https

# Xem danh sách profiles
dotnet run --list-profiles

# Build Tailwind CSS
cd StoreApp
npm run css:build

# Watch Tailwind CSS
npm run css:watch
```

## Cấu Hình Môi Trường

### Development
- File: `appsettings.Development.json`
- Swagger: Bật
- Logging: Chi tiết

### Production
- File: `appsettings.json`
- Swagger: Tắt (nên tắt trong production)
- Logging: Tối thiểu

## Lưu Ý Quan Trọng

1. **Database**: Đảm bảo MySQL đã được cài đặt và chạy trước khi start ứng dụng
2. **Port**: Mặc định HTTP: 5273, HTTPS: 7041 (có thể thay đổi trong `launchSettings.json`)
3. **Tailwind CSS**: Tự động build khi build project, nhưng có thể build thủ công nếu cần
4. **Hot Reload**: Blazor hỗ trợ hot reload, thay đổi code sẽ tự động refresh trong trình duyệt
5. **CORS**: Nếu cần gọi API từ domain khác, cấu hình CORS trong `Program.cs`

## Hỗ Trợ

Nếu gặp vấn đề, kiểm tra:
1. Logs trong console
2. Logs trong file (nếu có cấu hình file logging)
3. Browser Console (F12) để xem lỗi JavaScript/Blazor
4. Network tab trong Browser DevTools để kiểm tra API calls

