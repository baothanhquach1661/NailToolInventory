# NailToolInventory

Ứng dụng quản lý tồn kho dụng cụ nail. ASP.NET Core MVC (.NET 10) + EF Core trên SQLite + ASP.NET Core Identity.

## Lệnh thường dùng

```bash
dotnet build                                    # build cả solution
dotnet test                                     # chạy unit test (xUnit)
dotnet run --project src/NailToolInventory.Web  # chạy app
```

App chạy ở `http://localhost:5254` (profile `http`) hoặc `https://localhost:7119` (profile `https`).

EF Core migrations dùng **local tool** `dotnet-ef` 10.0.11 (khai báo trong `dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <TenMigration> --project src/NailToolInventory.Web
dotnet ef database update --project src/NailToolInventory.Web
```

## Cấu trúc

- `src/NailToolInventory.Web/` — project web duy nhất. **Lưu ý:** file project tên là
  `NailToolInventory.csproj` (không phải `NailToolInventory.Web.csproj`), và namespace là
  `NailToolInventory.*` — **không có** `.Web` trong namespace.
- `tests/NailToolInventory.Tests/` — xUnit, hiện chỉ test tầng domain (`ProductTests.cs`).

Trong project web: `Models/` (domain entity), `Data/` (DbContext, migrations, seeder),
`Controllers/`, `ViewModels/` (một view model riêng cho mỗi màn hình), `Views/`.

## Kiến trúc & quy ước

**Domain model kiểu "rich domain"** — đây là quy ước quan trọng nhất của codebase:

- Entity (`Product`, `InventoryTransaction`) có **property với `private set`**, cộng một
  constructor `private` không tham số dành riêng cho EF Core.
- Mọi thay đổi trạng thái đi qua **method có nghiệp vụ**, không set property trực tiếp:
  `ReceiveStock()`, `IssueStock()`, `AdjustStock()`, `UpdateDetails()`, `Activate()`,
  `Deactivate()`, `IsLowStock()`.
- **Validation nằm trong domain**, ném exception (`ArgumentException`,
  `ArgumentOutOfRangeException`, `InvalidOperationException`) — không nằm ở controller.
  Ví dụ: không cho đổi tồn kho của sản phẩm inactive (`EnsureActiveForInventoryTransaction`).

**Controller pattern** (xem `ProductsController`):

- Inject `ApplicationDbContext` trực tiếp, không có repository/service layer.
- Query đọc dùng `.AsNoTracking()`; query để sửa thì không.
- Trong POST action: **luôn nạp lại dữ liệu hiển thị từ database**, không tin giá trị
  view model do trình duyệt gửi lên.
- Gọi method domain trong `try/catch`, bắt exception domain rồi đưa vào
  `ModelState.AddModelError(...)`.
- Thành công thì set `TempData["SuccessMessage"]` rồi `RedirectToAction(nameof(Index))`.

**Audit tồn kho:** mọi thay đổi số lượng phải tạo kèm một `InventoryTransaction` ghi lại
`QuantityBefore` / `QuantityAfter`, lưu chung trong một `SaveChangesAsync()`. Transaction có
`DeleteBehavior.Restrict` — không xoá được product còn lịch sử.

**Cấu hình EF** nằm tập trung trong `ApplicationDbContext.OnModelCreating` (Fluent API),
không dùng data annotation trên entity. Enum lưu xuống DB dưới dạng **string**
(`.HasConversion<string>()`).

**Comment trong code viết bằng tiếng Việt** ở những chỗ giải thích quyết định thiết kế —
giữ nguyên phong cách này khi thêm code mới.

## Xác thực & phân quyền

- ASP.NET Core Identity với `ApplicationUser`, hai role: `Admin` và `Staff`
  (hằng số trong `IdentitySeeder.AdminRole` / `.StaffRole`).
- `ProductsController` yêu cầu `[Authorize(Roles = "Admin,Staff")]`; các action ghi/sửa
  nhạy cảm siết thêm `[Authorize(Roles = "Admin")]`.
- `IdentitySeeder.SeedAsync` chạy lúc khởi động, tạo role + tài khoản admin.

⚠️ **Gotcha:** seeder đọc `SeedAdmin:Email` / `SeedAdmin:Password` / `SeedAdmin:FullName` từ
configuration và **ném exception làm app không khởi động được nếu thiếu**. Các giá trị này
nằm trong **user secrets** (`UserSecretsId` trong csproj), không có trong `appsettings.json`
và không commit vào git. Máy mới phải set trước khi chạy:

```bash
dotnet user-secrets set "SeedAdmin:Email" "<email>" --project src/NailToolInventory.Web
dotnet user-secrets set "SeedAdmin:Password" "<password>" --project src/NailToolInventory.Web
```

Password policy: tối thiểu 8 ký tự, cần chữ hoa + chữ thường + số, không bắt ký tự đặc biệt.

## Lưu ý khác

- `UseRazorSourceGenerator=false` trong csproj là **workaround có chủ đích** cho một lỗi của
  Razor source generator (commit `858aceb`) — đừng xoá.
- Database là file SQLite `NailToolInventory.db`, tạo ngay tại thư mục chạy app.
