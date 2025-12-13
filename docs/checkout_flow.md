# Purchase Flow (Store / Checkout) – Summary and Notes

## Overall flow
- Customer browses and adds items to cart (local storage + sync to DB).
- On `/store/checkout`:
  - Requires login; auto-redirects to login if token missing/invalid.
  - Loads cart from local storage (selection persisted via `checkoutSelectedIds`).
  - Fetches profile via `api/me` to prefill name/email/phone/address.
  - Builds `OrderDTO` (status `pending`, payment `cash` by default) and submits to `api/orders/create`.
  - Persists order items via `api/orderitem/create`.
  - Reduces inventory via `api/inventory/reduce-multiple`.
  - Applies promotion if selected via `api/promotions/apply`.
  - Processes payment:
    - Cash/offline: `api/payment/offlinepayment` (now allowed for customer).
    - MoMo: `api/payment/momo/create` + IPN callbacks.
  - Redirects to Thank You page with `orderId` query.

## Authorization changes
- `OrdersController.getOrderByOrderId/{id}`: `[Authorize(Roles="admin,staff,customer")]`; customers can view their own orders (uses `customerId` claim or `NameIdentifier`; allows guest orders with no `CustomerId`).
- `InventoryController.reduce-multiple`: roles expanded to include `customer` (needed during checkout).
- `PaymentController.offlinepayment`: roles expanded to include `customer` (cash on delivery).

## Data mapping / DTOs
- `MeDTO` now includes `Phone` and `Address` to prefill checkout and profile.
- `MeController`: for role `customer`, reads/writes profile via `CustomerService` (table `customers`); staff/admin still via `UserService`.
- `OrderService.CreateOrderAsync`:
  - Auto-generates `Id`/`OrderNumber` if missing.
  - Fills `CustomerId` from token (`customerId` claim) when not provided; `StaffId` from token for admin/staff.

## Checkout UI logic (StoreCheckout.razor)
- Loads cart from local storage; optional filter by `checkoutSelectedIds`.
- Prefills customer info from `MeClientService`.
- On submit:
  1) `SaveFinalOrderAsync` (api/orders/create)
  2) `SaveListOrderItem` (api/orderitem/create)
  3) `ReduceMultipleAsync` (api/inventory/reduce-multiple)
  4) `ApplyPromotionAsync` if needed
  5) Payment (offline or MoMo)
- After success, navigates to `/store/thankyou?orderId={id}` which calls `getOrderByOrderId` + order items.

## Error handling notes
- Token 401 previously caused logout on `/api/me` (fixed by proper claim extraction and role-aware auth checks).
- 403 issues resolved by widening roles on inventory/payment/order detail endpoints and by matching `customerId` claim.
- Build locks: if `dotnet watch` is running, stop it to rebuild (pdb/exe lock).

## Files touched (key)
- Controllers: `OrdersController`, `InventoryController`, `PaymentController`, `MeController`.
- Services: `OrderService` (customer/staff IDs), `MeClientService` (claim parsing).
- DTO: `MeDTO`.
- Pages: `StoreCheckout.razor`, `StoreProfile.razor`.
