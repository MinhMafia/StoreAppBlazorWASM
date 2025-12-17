# Logic Nhập Hàng - Import Logic Documentation

## Tổng Quan

Hệ thống có 2 cách nhập hàng:

1. **Nhập hàng trực tiếp** (Inventory.razor) - Nhập nhanh từng sản phẩm
2. **Tạo phiếu nhập** (ImportReceipts.razor) - Nhập nhiều sản phẩm cùng lúc

## 🔐 Quy Tắc Nhập Hàng (Business Rules)

### 1. Validate Đầu Vào

```
✓ Số lượng (Quantity) > 0
✓ Giá vốn (UnitCost) >= 0
✓ Phải có ít nhất 1 sản phẩm trong phiếu nhập
```

### 2. Tự Động Ẩn Sản Phẩm

**Khi nào sản phẩm bị ẩn?**

- Ngay khi nhập hàng (dù từ Inventory hay ImportReceipts)
- `IsActive` được set = `false`

**Tại sao phải ẩn?**

- Sản phẩm vừa nhập có giá vốn mới
- Chưa đảm bảo giá bán >= giá vốn × 1.1 (markup 10%)
- Tránh bán lỗ

**Khi nào hiển thị lại?**

- Admin vào **Quản lý sản phẩm**
- Kiểm tra: `Giá bán >= Giá vốn × 1.1`
- Bật lại `IsActive = true`

### 3. Cập Nhật Dữ Liệu

#### a) Inventory (Tồn Kho)

```sql
inventory.quantity = inventory.quantity + import_quantity
inventory.updated_at = NOW()
```

#### b) Product Cost (Giá Vốn)

```sql
product.cost = unit_cost_from_import
product.is_active = false  -- Tự động ẩn
product.updated_at = NOW()
```

#### c) Inventory Adjustment Log

```sql
INSERT INTO inventory_adjustments (
    product_id,
    change_amount,
    reason,
    user_id,
    created_at
) VALUES (
    product_id,
    +quantity,  -- Số dương vì nhập hàng
    'Nhập hàng từ phiếu IMP-...',
    staff_id,
    NOW()
)
```

## 📋 Quy Trình Nhập Hàng

### Cách 1: Nhập Trực Tiếp (Inventory.razor)

```
1. Chọn sản phẩm → Click "Nhập hàng"
2. Nhập:
   - Số lượng (bắt buộc)
   - Giá vốn (tùy chọn)
   - Ghi chú (tùy chọn)
3. Validate:
   ✓ Quantity > 0
   ✓ Cost >= 0 (nếu có)
4. Backend tự động:
   ✓ Tăng inventory.quantity
   ✓ Cập nhật product.cost (nếu có)
   ✓ Ẩn sản phẩm (IsActive = 0)
   ✓ Tạo adjustment log
   ✓ Tạo phiếu nhập tự động
```

### Cách 2: Tạo Phiếu Nhập (ImportReceipts.razor)

```
Bước 1: Chọn Nhà Cung Cấp
- Hiển thị danh sách suppliers
- Click để chọn (1 supplier)

Bước 2: Chọn Sản Phẩm
- Hiển thị sản phẩm của supplier đã chọn
- Checkbox để chọn nhiều sản phẩm
- Nhập cho từng sản phẩm:
  * Số lượng (bắt buộc, > 0)
  * Đơn giá nhập (bắt buộc, >= 0, không được = 0)
- Ghi chú chung (tùy chọn)

Bước 3: Xác Nhận
- Hiển thị cảnh báo: "Sản phẩm sẽ bị ẨN sau khi nhập"
- Validate:
  ✓ Phải có supplier
  ✓ Phải có ít nhất 1 sản phẩm
  ✓ Quantity > 0
  ✓ UnitCost >= 0 và != 0
- Click "Tạo phiếu nhập"

Backend xử lý:
✓ Tạo Import record (status = 'completed')
✓ Tạo ImportItem records
✓ Với mỗi sản phẩm:
  - Tăng inventory.quantity
  - Cập nhật product.cost
  - Ẩn sản phẩm (IsActive = 0)
  - Tạo adjustment log
```

## 🔄 Backend Service Flow

### ImportReceiptService.CreateImportAsync()

```csharp
1. VALIDATE INPUT
   - Items.Any() must be true
   - Each item: Quantity > 0
   - Each item: UnitCost >= 0

2. BEGIN TRANSACTION

3. CREATE IMPORT
   - Generate import_number (IMP-yyyyMMddHHmmss)
   - Status = 'completed'
   - Save supplier_id, staff_id, note

4. FOR EACH PRODUCT:
   a) Create ImportItem
      - quantity, unit_cost, total_cost

   b) Update/Create Inventory
      - Tăng quantity
      - Update timestamp

   c) Update Product
      - cost = unit_cost
      - is_active = false  ← ẨN SẢN PHẨM
      - Update timestamp

   d) Create InventoryAdjustment
      - change_amount = +quantity
      - reason = "Nhập hàng từ phiếu..."
      - user_id = staff_id

5. CALCULATE TOTAL
   - total_amount = SUM(item.total_cost)

6. SAVE & COMMIT TRANSACTION

7. RETURN Import entity
```

## ⚠️ Cảnh Báo & Lưu Ý

### Hiển Thị Cho User

```
⚠️ LƯU Ý QUAN TRỌNG:
• Sau khi nhập hàng, TẤT CẢ sản phẩm sẽ tự động bị ẨN
• Giá vốn sản phẩm sẽ được cập nhật theo giá nhập
• Hệ thống sẽ tự động tăng số lượng tồn kho
• Bạn cần vào Quản lý sản phẩm để:
  - Kiểm tra giá bán
  - BẬT LẠI sản phẩm (nếu giá bán ≥ giá vốn × 1.1)
```

### Sau Khi Tạo Phiếu Nhập Thành Công

```
✓ Tạo phiếu nhập thành công!
• 5 sản phẩm, tổng 150 đơn vị
• Tất cả sản phẩm đã được ẨN tự động
• Vui lòng kiểm tra giá bán và BẬT LẠI sản phẩm
  trong Quản lý sản phẩm
```

## 📊 Database Schema Impact

### Tables Modified by Import

```sql
1. imports (tạo mới)
2. import_items (tạo mới)
3. inventory (update quantity)
4. products (update cost, is_active)
5. inventory_adjustments (tạo log)
```

### Foreign Key Constraints

```sql
imports.supplier_id → suppliers.id
imports.staff_id → users.id
import_items.import_id → imports.id
import_items.product_id → products.id
inventory_adjustments.product_id → products.id
inventory_adjustments.user_id → users.id
```

## 🧪 Test Cases

### Test 1: Validate Quantity

```
Input: quantity = 0
Expected: Error "Số lượng phải > 0"
```

### Test 2: Validate Cost

```
Input: unit_cost = -100
Expected: Error "Giá vốn không được âm"
```

### Test 3: Validate Cost Zero

```
Input: unit_cost = 0
Expected: Error "Vui lòng nhập giá vốn"
```

### Test 4: Product Deactivation

```
Before: product.is_active = true
Action: Create import with this product
After: product.is_active = false
Expected: Product hidden from customer view
```

### Test 5: Inventory Update

```
Before: inventory.quantity = 100
Action: Import 50 units
After: inventory.quantity = 150
Expected: Quantity increased correctly
```

### Test 6: Cost Update

```
Before: product.cost = 10000
Action: Import with unit_cost = 12000
After: product.cost = 12000
Expected: Cost updated to new import cost
```

### Test 7: Transaction Rollback

```
Action: Import fails at step 3/5
Expected: All changes rolled back
         No partial data saved
```

## 🔧 Configuration

### Backend

- ImportReceiptService uses transaction
- Auto-commit on success
- Auto-rollback on failure

### Frontend

- Real-time validation
- Warning messages
- Success confirmation with details

## 📝 Related Files

### Backend

- `StoreApp/Services/ImportReceiptService.cs` - Main import logic
- `StoreApp/Services/InventoryService.cs` - Inventory management
- `StoreApp/Repository/ImportRepository.cs` - Data access
- `StoreApp/Controllers/ImportsController.cs` - API endpoints

### Frontend

- `StoreApp.Client/Pages/Admin/ImportReceipts.razor` - Import management UI
- `StoreApp.Client/Pages/Admin/Inventory.razor` - Quick import UI
- `StoreApp.Client/Services/ImportClientService.cs` - API client

### Shared

- `StoreApp.Shared/DTO/ImportDTOs.cs` - Data transfer objects

## 🎯 Best Practices

1. **Luôn nhập giá vốn** - Không để cost = 0
2. **Kiểm tra giá bán** - Trước khi bật sản phẩm
3. **Đảm bảo markup** - Giá bán >= cost × 1.1
4. **Xem adjustment log** - Để audit trail
5. **Backup trước khi import** - Phòng trường hợp sai sót

## 🔐 Security

- Staff authentication required (JWT)
- Staff ID tracked in import records
- All changes logged in inventory_adjustments
- Transaction ensures data consistency
- Validation on both client and server

## 📈 Performance

- Batch processing for multiple products
- Single transaction for all updates
- Indexed foreign keys
- Efficient queries with Include()
- No N+1 query problems
