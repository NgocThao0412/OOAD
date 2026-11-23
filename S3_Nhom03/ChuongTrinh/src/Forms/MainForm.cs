using OOAD.Bakery.Controls;
using OOAD.Bakery.Modules;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Forms;
public sealed class MainForm:Form
{
    private readonly Account account;
    private readonly Panel content=new(){Dock=DockStyle.Fill,Padding=new Padding(20),BackColor=Theme.Background};
    private readonly FlowLayoutPanel sidebar=new(){Dock=DockStyle.Left,Width=235,Padding=new Padding(10,12,10,10),
        BackColor=Theme.Sidebar,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};
    private readonly Dictionary<string,Button> buttons=new();
    private readonly (string label,string sql,string note)[] views =
    {
        ("Người dùng","SELECT tk.MaTK,tk.TenTK,tk.VaiTro,tk.TT,tk.NgayTao,nv.HoTen FROM TaiKhoan tk JOIN NhanVien nv ON nv.MaNV=tk.MaNV ORDER BY tk.MaTK DESC", "Danh sách tài khoản và nhân viên từ CSDL."),
        ("Khách hàng","SELECT * FROM KhachHang ORDER BY MaKH DESC", "Danh sách khách hàng từ CSDL."),
        ("Nhà cung cấp","SELECT * FROM NhaCungCap ORDER BY MaNCC DESC", "Danh sách nhà cung cấp từ CSDL."),
        ("Nhập kho","SELECT pn.MaPN,pn.NgayNhap,n.TenNCC,nv.HoTen,pn.TongTien,pn.TT FROM PhieuNhapKho pn JOIN NhaCungCap n ON n.MaNCC=pn.MaNCC JOIN NhanVien nv ON nv.MaNV=pn.MaNV ORDER BY pn.MaPN DESC", "Phiếu nhập và trạng thái, không tính phiếu nháp vào tồn kho."),
        ("Giá bán","SELECT c.MaCH,s.TenSP,c.TyLeLN,c.NgayAD,nv.HoTen FROM CauHinhGiaBan c JOIN SanPham s ON s.MaSP=c.MaSP JOIN NhanVien nv ON nv.MaNV=c.MaNV ORDER BY c.NgayAD DESC", "Lịch sử cấu hình tỷ lệ lợi nhuận từ CSDL."),
        ("Đơn hàng","SELECT d.MaDH,k.HoTen,d.NgayDat,d.NgayGiao,d.TongTien,d.PTTT,d.TTThanhToan,d.TTDon FROM DonHang d JOIN KhachHang k ON d.MaKH=k.MaKH ORDER BY d.MaDH DESC", "Danh sách đơn hàng theo trạng thái."),
        ("Giao nhận","SELECT g.MaGN,g.MaDH,g.NguoiNhan,g.DiaChiNhan,g.DonViVC,g.NguoiGiao,g.TTGiao,g.LyDoThatBai FROM GiaoNhan g ORDER BY g.MaGN DESC", "Danh sách giao nhận từ CSDL."),
        ("Tồn kho & cảnh báo",@"SELECT sp.MaSP,sp.TenSP,sp.NguongTon,COALESCE(SUM(lo.SLCon),0) AS TonHienTai,
         CASE WHEN COALESCE(SUM(lo.SLCon),0)<=sp.NguongTon THEN N'Cảnh báo' ELSE N'Bình thường' END AS CanhBao
         FROM SanPham sp LEFT JOIN LoHang lo ON sp.MaSP=lo.MaSP GROUP BY sp.MaSP,sp.TenSP,sp.NguongTon ORDER BY sp.TenSP", "Tồn hiện tại = SUM(LoHang.SLCon). Không trừ PhanBoXuatLo lần thứ hai."),
        ("Thống kê & báo cáo",@"SELECT sp.MaSP,sp.TenSP,
         COALESCE((SELECT SUM(ct.SLNhap) FROM ChiTietPhieuNhap ct JOIN PhieuNhapKho pn ON pn.MaPN=ct.MaPN WHERE ct.MaSP=sp.MaSP AND pn.TT=N'Hoàn tất'),0) AS TongNhapHoanTat,
         COALESCE((SELECT SUM(pb.SLXuat) FROM PhanBoXuatLo pb JOIN LoHang lo ON lo.MaLo=pb.MaLo WHERE lo.MaSP=sp.MaSP),0) AS TongXuatTheoLo,
         COALESCE((SELECT SUM(lo.SLCon) FROM LoHang lo WHERE lo.MaSP=sp.MaSP),0) AS TonHienTai
         FROM SanPham sp ORDER BY sp.TenSP", "Tổng hợp toàn thời gian; N-X-T theo kỳ và trả hàng sẽ triển khai cùng nghiệp vụ chứng từ.")
    };
    public MainForm(Account a)
    {
        account=a;
        Text="Sweet Bakery | Hệ thống quản lý bán bánh ngọt";
        StartPosition=FormStartPosition.CenterScreen;WindowState=FormWindowState.Maximized;
        MinimumSize=new Size(1100,720);BackColor=Theme.Background;
        Controls.Add(content);Controls.Add(sidebar);
        sidebar.SizeChanged+=(_,_)=>{foreach(Control b in sidebar.Controls)b.Width=Math.Max(190,sidebar.ClientSize.Width-28);};
        var brand=new Label{Text="✿ Sweet Bakery",Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=Theme.AccentDark,
            Width=203,Height=62,TextAlign=ContentAlignment.MiddleCenter};
        sidebar.Controls.Add(brand);
        var modules=new[]{"Trang chủ","Danh mục bánh","Sản phẩm bánh","Người dùng","Khách hàng","Nhà cung cấp",
            "Nhập kho","Giá bán","Đơn hàng","Giao nhận","Tồn kho & cảnh báo","Thống kê & báo cáo"};
        foreach(string name in modules)
        {
            var btn=Theme.Button("   "+name);
            btn.Height=43;btn.Width=200;btn.TextAlign=ContentAlignment.MiddleLeft;
            btn.Click+=(_,_)=>Navigate(name);sidebar.Controls.Add(btn);buttons[name]=btn;
        }
        var who=Theme.Label($"{account.UserName} ({account.Role})",9,true,Theme.AccentDark);
        who.Width=200;who.Height=36;sidebar.Controls.Add(who);
        var logout=Theme.Button("Đăng xuất");logout.Click+=(_,_)=>Close();sidebar.Controls.Add(logout);
        Navigate("Trang chủ");
    }
    private void Navigate(string name)
    {
        try
        {
            Control page = name switch
            {
                "Trang chủ" => new DashboardPage(()=>Navigate("Sản phẩm bánh"),()=>Navigate("Danh mục bánh")),
                "Danh mục bánh" => new CatalogPage(account,false),
                "Sản phẩm bánh" => new CatalogPage(account,true),
                "Người dùng" => new UsersPage(account),
                "Khách hàng" => new ContactsPage(account,"Khách hàng"),
                "Nhà cung cấp" => new ContactsPage(account,"Nhà cung cấp"),
                "Nhập kho" => new OperationPage(account,true),
                "Đơn hàng" => new OperationPage(account,false),
                "Thống kê & báo cáo" => new StockReportPage(),
                "Giá bán" => new PricePage(account),
                "Giao nhận" => new DeliveryPage(account),
                _=>BuildDataPage(name)
            };
            foreach(var (key,b) in buttons)
            {
                b.BackColor=key==name?Theme.Paper:Theme.Lavender;
                b.ForeColor=key==name?Theme.AccentDark:Theme.Text;
            }
            content.Controls.Clear();page.Dock=DockStyle.Fill;content.Controls.Add(page);
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private Control BuildDataPage(string name)
    {
        var view=views.FirstOrDefault(v=>v.label==name);
        if(string.IsNullOrWhiteSpace(view.sql))throw new InvalidOperationException("Không có màn hình: "+name);
        return new ReadOnlyPage(name,view.sql,view.note);
    }
}
