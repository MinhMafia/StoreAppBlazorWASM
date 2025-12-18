# Flowchart Quản Lý Phiếu Nhập Hàng

## 1. Xem Danh Sách Phiếu Nhập (GET /api/imports)

```mermaid
flowchart TD
    A[Bắt đầu - Truy cập /admin/import-receipts] --> B[Render giao diện danh sách]
    B --> C[Gửi GET /api/imports với params:<br/>- page, pageSize<br/>- search, status<br/>- sortBy]

    C --> D{Backend xử lý}
    D --> E[Gọi ImportReceiptService.GetImportsPagedAsync]

    E --> F[Query từ imports table với:<br/>- LEFT JOIN suppliers<br/>- LEFT JOIN users staff<br/>- LEFT JOIN import_items]

    F --> G{Áp dụng bộ lọc}
    G -->|search không null| H[Filter theo ImportNumber<br/>hoặc SupplierName]
    G -->|status không null| I[Filter theo Status]
    G -->|Không có filter| J[Lấy tất cả]

    H --> K[Áp dụng sắp xếp sortBy]
    I --> K
    J --> K

    K --> L[Phân trang với Skip & Take]
    L --> M[Select sang ImportListItemDTO:<br/>- Id, ImportNumber<br/>- SupplierName, StaffName<br/>- TotalAmount<br/>- TotalItems, TotalQuantity<br/>- Status, CreatedAt]

    M --> N[Tính TotalItems = COUNT items<br/>Tính TotalPages]
    N --> O[Trả về PaginationResult]

    O --> P{Client nhận response}
    P -->|Success 200| Q[Hiển thị bảng với:<br/>- Mã phiếu<br/>- Nhà cung cấp<br/>- Nhân viên<br/>- Tổng tiền<br/>- SL sản phẩm/số lượng<br/>- Trạng thái<br/>- Ngày tạo]
    P -->|Error| R[Hiển thị thông báo lỗi]

    Q --> S[Kết thúc]
    R --> S
```

## 2. Xem Chi Tiết Phiếu Nhập (GET /api/imports/{id})

```mermaid
flowchart TD
    A[Bắt đầu - Click nút Xem chi tiết] --> B[Gửi GET /api/imports/id]

    B --> C[Backend: ImportReceiptService.GetImportDetailAsync]
    C --> D[Query Import từ DB:<br/>- Include Supplier<br/>- Include Staff<br/>- Include ImportItems<br/>  → ThenInclude Product]

    D --> E{Tìm thấy Import?}
    E -->|Không| F[Trả về 404 Not Found]
    E -->|Có| G[Map sang ImportDetailDTO]

    G --> H[Thông tin Import:<br/>- Id, ImportNumber<br/>- SupplierName<br/>- StaffName<br/>- TotalAmount, Status<br/>- Note, CreatedAt]

    H --> I[Map ImportItems sang list:<br/>- ProductId, ProductName<br/>- Quantity, UnitCost<br/>- TotalCost]

    I --> J[Trả về 200 OK với ImportDetailDTO]

    J --> K[Client: Hiển thị Modal chi tiết]
    K --> L[Hiển thị:<br/>- Thông tin phiếu nhập<br/>- Bảng danh sách sản phẩm<br/>- Tổng số lượng<br/>- Tổng giá trị]

    F --> M[Kết thúc]
    L --> M
```

## 3. Tạo Phiếu Nhập Mới (POST /api/imports)

```mermaid
flowchart TD
    A[Bắt đầu - Click Tạo phiếu nhập] --> B[Hiển thị wizard/form:<br/>Bước 1 - Chọn nhà cung cấp]

    B --> C[User chọn 1 supplier]
    C --> D[Bước 2 - Chọn sản phẩm của supplier]

    D --> E[Hiển thị danh sách products<br/>của supplier đã chọn]
    E --> F[User chọn nhiều sản phẩm:<br/>- Checkbox để chọn<br/>- Nhập Quantity bắt buộc<br/>- Nhập UnitCost bắt buộc]

    F --> G[User nhập ghi chú tùy chọn]
    G --> H[Click Tạo phiếu nhập]

    H --> I{Validate Client}
    I -->|Không có supplier| J[Lỗi: Chọn nhà cung cấp]
    I -->|Không có sản phẩm| K[Lỗi: Chọn ít nhất 1 sản phẩm]
    I -->|Quantity <= 0| L[Lỗi: Số lượng phải > 0]
    I -->|UnitCost <= 0| M[Lỗi: Giá vốn phải > 0]
    I -->|Hợp lệ| N[Hiển thị cảnh báo:<br/>⚠️ Sản phẩm sẽ bị ẨN sau khi nhập]

    N --> O[User xác nhận]
    O --> P[Tạo CreateImportDTO:<br/>- SupplierId<br/>- Note<br/>- Items List]

    P --> Q[POST /api/imports với DTO]

    Q --> R[Backend: ImportReceiptService.CreateImportAsync]

    R --> S{Validate Server}
    S -->|Items rỗng| T[Trả về 500 - Lỗi validation]
    S -->|Quantity <= 0| T
    S -->|UnitCost < 0| T
    S -->|Hợp lệ| U[BEGIN TRANSACTION]

    U --> V[Tạo Import entity:<br/>- ImportNumber = IMP-yyyyMMddHHmmss<br/>- Status = completed<br/>- CreatedAt = Vietnam Time]

    V --> W[Lưu Import vào DB<br/>để lấy Import.Id]

    W --> X[Lặp qua từng ImportItem]

    X --> Y[Với mỗi Product:]
    Y --> Y1[1. Tạo ImportItem record:<br/>- ImportId, ProductId<br/>- Quantity, UnitCost<br/>- TotalCost = Quantity × UnitCost]

    Y1 --> Y2[2. Tìm/Tạo Inventory record]
    Y2 --> Y3[3. Cập nhật Inventory:<br/>inventory.Quantity += import.Quantity<br/>inventory.UpdatedAt = now]

    Y3 --> Y4[4. Cập nhật Product:<br/>product.Cost = UnitCost<br/>product.IsActive = FALSE<br/>product.UpdatedAt = now]

    Y4 --> Y5[5. Tạo InventoryAdjustment log:<br/>- ProductId, ChangeAmount = +Quantity<br/>- Reason = Nhập hàng từ IMP-...<br/>- UserId = StaffId]

    Y5 --> Z{Còn product nào?}
    Z -->|Có| Y
    Z -->|Không| AA[Tính TotalAmount = SUM ImportItem.TotalCost]

    AA --> AB[Cập nhật Import.TotalAmount]
    AB --> AC[COMMIT TRANSACTION]

    AC --> AD[Trả về 200 OK với:<br/>- message<br/>- importId<br/>- importNumber]

    AD --> AE[Client: Hiển thị thông báo thành công:<br/>✓ Tạo phiếu nhập thành công<br/>⚠️ Sản phẩm đã được ẨN<br/>Vui lòng kiểm tra giá bán]

    AE --> AF[Đóng modal và reload danh sách]

    J --> AG[Kết thúc]
    K --> AG
    L --> AG
    M --> AG
    T --> AG
    AF --> AG
```

## 4. Cập Nhật Trạng Thái Phiếu Nhập (PATCH /api/imports/{id}/status)

```mermaid
flowchart TD
    A[Bắt đầu - Admin thay đổi status] --> B[Gửi PATCH /api/imports/id/status<br/>Body: status]

    B --> C{Validate Role}
    C -->|Không phải admin| D[Trả về 403 Forbidden]
    C -->|Là admin| E[Backend: ImportReceiptService.UpdateStatusAsync]

    E --> F[Tìm Import theo Id]
    F --> G{Tìm thấy?}
    G -->|Không| H[Trả về 404 Not Found]
    G -->|Có| I[Cập nhật Import.Status]

    I --> J[Cập nhật Import.UpdatedAt]
    J --> K[Lưu vào DB]

    K --> L[Trả về 200 OK]
    L --> M[Client: Reload danh sách]

    D --> N[Kết thúc]
    H --> N
    M --> N
```

## 5. Luồng Import Excel Sản Phẩm (POST /api/import/products)

```mermaid
flowchart TD
    A[Bắt đầu - Click Import Excel] --> B[User chọn file Excel/CSV]
    B --> C[Upload file qua form]

    C --> D{Validate file}
    D -->|File null/empty| E[Trả về 400 - File không được trống]
    D -->|Hợp lệ| F[POST /api/import/products với IFormFile]

    F --> G[Backend: ImportService.ImportProductsAsync]
    G --> H[Đọc file Excel với EPPlus]

    H --> I[Validate sheet Products tồn tại]
    I --> J{Sheet có dữ liệu?}
    J -->|Không| K[Throw ArgumentException]
    J -->|Có| L[Lặp qua từng dòng từ row 2]

    L --> M[Với mỗi dòng:]
    M --> M1{Validate từng field}
    M1 -->|ProductName rỗng| N[Bỏ qua dòng này]
    M1 -->|ProductName < 2 ký tự| N
    M1 -->|Price <= 0| N
    M1 -->|Hợp lệ| O[Parse dữ liệu:<br/>- ProductName<br/>- Sku auto nếu rỗng<br/>- CategoryId/Name<br/>- SupplierId/Name<br/>- UnitId/Name<br/>- Price, Cost<br/>- IsActive]

    O --> P{Kiểm tra Sku trùng}
    P -->|Trùng| Q[Cập nhật product cũ]
    P -->|Không trùng| R[Tạo product mới]

    Q --> S[Lưu vào danh sách thành công]
    R --> S

    S --> T{Còn dòng nào?}
    T -->|Có| M
    T -->|Không| U[Lưu tất cả vào DB<br/>SaveChangesAsync]

    U --> V[Tạo ImportResultDTO:<br/>- TotalRows<br/>- SuccessCount<br/>- FailedCount<br/>- Errors list]

    V --> W[Trả về 200 OK với result]
    W --> X[Client: Hiển thị kết quả:<br/>- Tổng dòng<br/>- Thành công<br/>- Thất bại<br/>- Chi tiết lỗi]

    E --> Y[Kết thúc]
    K --> Y
    N --> Y
    X --> Y
```

## 6. Tải Template Import (GET /api/import/template/products)

```mermaid
flowchart TD
    A[Bắt đầu - Click Tải template] --> B[Gửi GET /api/import/template/products?format=excel]

    B --> C[Backend: GenerateProductExcelTemplate]
    C --> D[Tạo ExcelPackage mới]

    D --> E[Sheet 1: Hướng dẫn<br/>- Giải thích các trường<br/>- Quy tắc nhập liệu<br/>- Lưu ý quan trọng]

    E --> F[Sheet 2: Products<br/>- Header columns<br/>- 1 dòng dữ liệu mẫu<br/>- Format cells]

    F --> G[Sheet 3-5: Reference data<br/>- Categories<br/>- Suppliers<br/>- Units]

    G --> H[Format styling:<br/>- Bold headers<br/>- Color coding<br/>- Auto-fit columns]

    H --> I[Trả về file Excel<br/>Content-Type: application/vnd.openxmlformats]

    I --> J[Client: Browser download file<br/>Product_Template.xlsx]

    J --> K[Kết thúc]
```

## Tổng Quan Quy Trình Nhập Hàng

```mermaid
flowchart TB
    A[Quản Lý Phiếu Nhập] --> B[Xem Danh Sách]
    A --> C[Tạo Phiếu Nhập Mới]
    A --> D[Import Excel]

    B --> B1[Phân trang]
    B --> B2[Tìm kiếm/Lọc]
    B --> B3[Xem chi tiết]
    B --> B4[Cập nhật trạng thái]

    C --> C1[Chọn nhà cung cấp]
    C1 --> C2[Chọn sản phẩm]
    C2 --> C3[Nhập số lượng & giá vốn]
    C3 --> C4[Xác nhận tạo phiếu]

    C4 --> C5[Backend xử lý:]
    C5 --> C5a[Tạo Import record]
    C5 --> C5b[Tạo ImportItems]
    C5 --> C5c[Cập nhật Inventory +]
    C5 --> C5d[Cập nhật Product Cost]
    C5 --> C5e[ẨN sản phẩm IsActive=false]
    C5 --> C5f[Tạo Adjustment Log]

    D --> D1[Tải template]
    D --> D2[Điền dữ liệu Excel]
    D --> D3[Upload file]
    D --> D4[Validate & Import]
    D --> D5[Hiển thị kết quả]
```

## Luồng Dữ Liệu Chi Tiết

```mermaid
flowchart LR
    subgraph Input["Đầu vào"]
        A1[SupplierId]
        A2[List Products:<br/>- ProductId<br/>- Quantity<br/>- UnitCost]
        A3[Note optional]
    end

    subgraph Processing["Xử lý"]
        B1[Validate Input]
        B2[BEGIN Transaction]
        B3[Create Import]
        B4[Loop ImportItems]
        B5[Update Inventory]
        B6[Update Product Cost]
        B7[Deactivate Product]
        B8[Create Adjustment Log]
        B9[Calculate Total]
        B10[COMMIT Transaction]
    end

    subgraph Output["Đầu ra"]
        C1[Import record<br/>status=completed]
        C2[ImportItems records]
        C3[Inventory updated<br/>quantity increased]
        C4[Products updated<br/>cost + IsActive=false]
        C5[InventoryAdjustment logs]
    end

    Input --> Processing
    Processing --> Output

    B1 --> B2
    B2 --> B3
    B3 --> B4
    B4 --> B5
    B5 --> B6
    B6 --> B7
    B7 --> B8
    B8 --> B9
    B9 --> B10
```

## Quy Tắc Nghiệp Vụ Quan Trọng

```mermaid
flowchart TD
    A[Nhập Hàng Thành Công] --> B{Tự động xảy ra:}

    B --> C[1. Tăng Tồn Kho<br/>inventory.quantity += import.quantity]
    B --> D[2. Cập Nhật Giá Vốn<br/>product.cost = import.unitCost]
    B --> E[3. ẨN Sản Phẩm<br/>product.IsActive = FALSE]
    B --> F[4. Ghi Log<br/>inventory_adjustments]

    E --> G[⚠️ LÝ DO ẨN SẢN PHẨM:]
    G --> H[Giá vốn mới chưa được<br/>kiểm tra với giá bán]
    H --> I[Tránh bán lỗ<br/>Price >= Cost × 1.1]

    I --> J[Admin phải:]
    J --> K[1. Vào Quản lý sản phẩm]
    J --> L[2. Kiểm tra giá bán hợp lý]
    J --> M[3. BẬT lại IsActive = TRUE]
```

## So Sánh 2 Cách Nhập Hàng

```mermaid
flowchart TB
    subgraph Method1["Cách 1: Tạo Phiếu Nhập UI"]
        A1[Chọn nhà cung cấp]
        A2[Chọn nhiều sản phẩm]
        A3[Nhập số lượng/giá vốn<br/>cho từng sản phẩm]
        A4[Ghi chú chung]
        A5[Tạo phiếu một lần]
    end

    subgraph Method2["Cách 2: Import Excel"]
        B1[Download template]
        B2[Điền thông tin sản phẩm:<br/>- ProductName<br/>- SKU, Price, Cost<br/>- Category, Supplier, Unit]
        B3[Upload file]
        B4[Validate từng dòng]
        B5[Import hàng loạt]
    end

    subgraph Common["Xử lý chung"]
        C1[Validate dữ liệu]
        C2[Transaction processing]
        C3[Update Inventory]
        C4[Update Product]
        C5[Deactivate products]
        C6[Create logs]
    end

    Method1 --> Common
    Method2 --> Common
```

## Trạng Thái Phiếu Nhập

```mermaid
stateDiagram-v2
    [*] --> pending: Tạo phiếu nhập mới

    pending --> completed: Admin duyệt<br/>hoặc tự động
    pending --> cancelled: Admin hủy

    completed --> [*]: Hoàn tất
    cancelled --> [*]: Đã hủy

    note right of completed
        - Inventory đã cập nhật
        - Product cost đã cập nhật
        - Products bị ẨN
        - Logs đã tạo
    end note

    note right of cancelled
        - Không thay đổi dữ liệu
        - Chỉ đánh dấu status
    end note
```
