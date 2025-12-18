# Cart & Auth Routing Flow

## Overall

- Giỏ hàng lưu local storage key `store_cart` cho mọi user (kể cả chưa login).
- Nếu user là customer và có token: đồng bộ với server qua API `api/cart`/`api/cart/sync`.
- Khi user login từ luồng "Mua hàng" (flag `redirectAfterLoginCart`): giỏ local sẽ ghi đè server sau login.
- Logout: sync cart lên server trước, sau đó xóa local để tránh dính cart cũ.

## Dịch vụ chính

- `StoreApp.Client/Services/StoreCartService.cs`:
  - `AddToCart/UpdateQuantity/RemoveFromCart/ClearCart/GetCartItems/GetCartItemCount`.
  - `SyncToServerAsync()`: post local cart lên server nếu có token customer.
  - `PullFromServerAsync()`:
    - Nếu flag `redirectAfterLoginCart` và local có items: post local -> server, lưu local, xóa flag.
    - Ngược lại: lấy cart từ server, lưu vào local.
  - `ClearLocalCart()`: chỉ xóa local, không đẩy server (dùng khi logout).
  - `OnCartChanged`: event để UI (StoreNavMenu, StoreCart page) reload badge/số tiền.

## Luồng thêm giỏ hàng (guest hoặc logged)

1. `AddToCart` cập nhật local và raise `OnCartChanged`.
2. Nếu đang đăng nhập customer: đồng bộ lên server (`api/cart/sync`).
3. UI badge (StoreNavMenu) và trang giỏ (StoreCart.razor) subscribe `OnCartChanged` -> gọi `GetCartItems()` và tính lại tổng.

## Luồng login từ nút Mua hàng

- `StoreCart.razor`: chưa login bấm Mua hàng -> set flag `redirectAfterLoginCart=true`, điều hướng `/login`.
- `Login.razor`: sau login nếu flag tồn tại -> điều hướng `/store/cart` (flag được xóa trong Pull).
- `PullFromServerAsync`: thấy flag + local có dữ liệu -> post local lên server, lưu local, xóa flag.

## Luồng login thường

- Không đặt flag -> `PullFromServerAsync` sẽ lấy giỏ từ server (không ghi đè bằng local).

## Luồng checkout

- `StoreCheckout.razor` tự kiểm tra token customer (`IsCustomerLoggedIn`). Nếu chưa login => redirect `/store`.
- Khi load: tạo order tạm, lấy danh sách id sản phẩm từ query `ids` của giỏ hàng.

## Routing / bảo vệ route (AuthorizedRouteView)

- Public prefixes: `login`, `admin/login`, `register`, ``(root),`store`, `store/products`.
- Nếu đã có token mà vào login sai role => tự redirect theo role (admin/staff -> /admin/dashboard, customer -> /store).
- Route `admin/*`: cần role admin/staff, nếu không -> /admin/login.
- Route `store/*` (ngoài public trên): cần customer, nếu không -> /login.
- Route root: nếu có token -> redirect theo role; nếu không -> /store.
- Riêng `StoreCheckout.razor` vẫn tự chặn khách chưa login dù prefix store là public.

## Logout

- `StoreNavMenu.razor` HandleLogout: `SyncToServerAsync()`, `ClearLocalCart()`, xóa token/userRole/userName, điều hướng `/store`.

## Server side

- API giỏ hàng: `CartController` đọc/ghi bảng `user_carts`.
- Khi customer login, `PullFromServerAsync` áp dụng logic flag ở trên để đồng bộ đúng giỏ.
