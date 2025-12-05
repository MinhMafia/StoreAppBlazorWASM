# Cấu trúc phần Cửa Hàng Khách Hàng (Store)

## Cấu trúc thư mục

```
StoreApp.Client/
├── Layout/
│   ├── StoreLayout.razor          # Layout chính cho cửa hàng
│   ├── StoreLayout.razor.css
│   ├── StoreNavMenu.razor          # Menu điều hướng cửa hàng
│   └── StoreNavMenu.razor.css
│
├── Pages/Store/
│   ├── StoreHome.razor             # Trang chủ cửa hàng (/store)
│   ├── StoreHome.razor.css
│   ├── StoreProducts.razor         # Danh sách sản phẩm (/store/products)
│   ├── StoreProducts.razor.css
│   ├── StoreProductDetail.razor    # Chi tiết sản phẩm (/store/products/{id})
│   ├── StoreProductDetail.razor.css
│   ├── StoreCart.razor             # Giỏ hàng (/store/cart)
│   ├── StoreCart.razor.css
│   ├── StoreCheckout.razor         # Thanh toán (/store/checkout)
│   ├── StoreCheckout.razor.css
│   ├── StoreOrders.razor           # Đơn hàng của khách hàng (/store/orders)
│   ├── StoreOrders.razor.css
│   ├── StoreOrderDetail.razor      # Chi tiết đơn hàng (/store/orders/{id})
│   └── StoreOrderDetail.razor.css
│
├── Components/Store/
│   ├── ProductCard.razor           # Component hiển thị thẻ sản phẩm
│   ├── ProductCard.razor.css
│   ├── CartItem.razor              # Component hiển thị item trong giỏ hàng
│   └── CartItem.razor.css
│
└── Services/
    └── StoreCartService.cs         # Service quản lý giỏ hàng
```

## Các trang chính

### 1. StoreHome.razor (`/store`)
- Trang chủ cửa hàng
- Hero section
- Sản phẩm nổi bật
- Danh mục sản phẩm
- Khuyến mãi đặc biệt

### 2. StoreProducts.razor (`/store/products`)
- Danh sách tất cả sản phẩm
- Bộ lọc theo danh mục, giá
- Tìm kiếm sản phẩm
- Hiển thị dạng grid

### 3. StoreProductDetail.razor (`/store/products/{id}`)
- Chi tiết sản phẩm
- Hình ảnh sản phẩm
- Thông tin giá, mô tả
- Nút thêm vào giỏ hàng
- Sản phẩm liên quan

### 4. StoreCart.razor (`/store/cart`)
- Danh sách sản phẩm trong giỏ hàng
- Cập nhật số lượng
- Xóa sản phẩm
- Tóm tắt đơn hàng
- Nút thanh toán

### 5. StoreCheckout.razor (`/store/checkout`)
- Form thông tin khách hàng
- Địa chỉ giao hàng
- Phương thức thanh toán
- Tóm tắt đơn hàng
- Xử lý đặt hàng

### 6. StoreOrders.razor (`/store/orders`)
- Danh sách đơn hàng của khách hàng
- Trạng thái đơn hàng
- Tổng tiền
- Link xem chi tiết

### 7. StoreOrderDetail.razor (`/store/orders/{id}`)
- Chi tiết đơn hàng
- Thông tin giao hàng
- Danh sách sản phẩm đã đặt
- Thông tin thanh toán

## Components

### ProductCard.razor
- Hiển thị thẻ sản phẩm trong danh sách
- Hình ảnh, tên, giá
- Badge khuyến mãi
- Nút thêm vào giỏ hàng

### CartItem.razor
- Hiển thị một item trong giỏ hàng
- Hình ảnh, tên, giá
- Điều chỉnh số lượng
- Xóa item

## Services

### StoreCartService
- Quản lý giỏ hàng (thêm, xóa, cập nhật)
- Lưu trữ giỏ hàng (localStorage hoặc state)
- Event khi giỏ hàng thay đổi
- Đếm số lượng items trong giỏ

## Lưu ý

1. **Layout riêng biệt**: Sử dụng `StoreLayout` thay vì `MainLayout` để tách biệt hoàn toàn với admin
2. **Routes**: Tất cả routes bắt đầu với `/store` để phân biệt với admin
3. **Styling**: Sử dụng CSS riêng cho từng trang, có thể dùng Tailwind CSS hoặc Bootstrap
4. **State Management**: Giỏ hàng có thể lưu vào localStorage hoặc state management service
5. **API Integration**: Cần tích hợp với các API endpoints từ backend:
   - GET `/api/products` - Lấy danh sách sản phẩm
   - GET `/api/products/{id}` - Chi tiết sản phẩm
   - POST `/api/orders` - Tạo đơn hàng
   - GET `/api/orders` - Lấy đơn hàng của khách hàng
   - GET `/api/orders/{id}` - Chi tiết đơn hàng

## Bước tiếp theo

1. Implement logic cho các trang
2. Tích hợp với API backend
3. Thêm authentication nếu cần
4. Thêm validation cho forms
5. Thêm error handling
6. Thêm loading states
7. Tối ưu responsive design

