using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;
public sealed class DashboardPage:UserControl
{
    public DashboardPage(Action products,Action categories)
    {
        BackColor=Theme.Background;
        var main=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=1,RowCount=5};
        main.RowStyles.Add(new RowStyle(SizeType.Absolute,66));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute,85));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute,120));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        main.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(main);
        main.Controls.Add(FormsUi.Title("Tổng quan hoạt động cửa hàng"),0,0);
        var welcome=new Label {Text="SWEET BAKERY   •   HỆ THỐNG QUẢN LÝ BÁN BÁNH NGỌT\nDữ liệu trên màn hình được truy vấn trực tiếp từ SQL Server.",
            Dock=DockStyle.Fill,BackColor=Theme.Lavender,ForeColor=Theme.AccentDark,
            Font=new Font("Segoe UI",13,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter};
        main.Controls.Add(welcome,0,1);
        var tiles=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=4};
        for(int i=0;i<4;i++)tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
        main.Controls.Add(tiles,0,2);
        var data=new (string label,string sql)[]{
            ("Tổng sản phẩm","SELECT COUNT(*) FROM SanPham WHERE TT=N'Đang bán'"),
            ("Đơn hàng hôm nay","SELECT COUNT(*) FROM DonHang WHERE CAST(NgayDat AS date)=CAST(GETDATE() AS date)"),
            ("Doanh thu hôm nay","SELECT COALESCE(SUM(TongTien),0) FROM DonHang WHERE TTThanhToan=N'Đã thanh toán' AND CAST(NgayDat AS date)=CAST(GETDATE() AS date) AND TTDon<>N'Đã hủy'"),
            ("Sản phẩm dưới ngưỡng","SELECT COUNT(*) FROM SanPham s WHERE s.TT=N'Đang bán' AND (SELECT COALESCE(SUM(l.SLCon),0) FROM LoHang l WHERE l.MaSP=s.MaSP)<=s.NguongTon")
        };
        for(int i=0;i<data.Length;i++)
        {
            object value=Db.Scalar(data[i].sql)??0;
            string displayed=i==2?$"{Convert.ToDecimal(value):N0} đ":value.ToString()??"0";
            var card=new Label{Text=$"{data[i].label}\n\n{displayed}",Dock=DockStyle.Fill,BackColor=i%2==0?Theme.PinkSoft:Theme.Lavender,
                ForeColor=Theme.AccentDark,Font=new Font("Segoe UI",14,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter,Margin=new Padding(5)};
            tiles.Controls.Add(card,i,0);
        }
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill};
        var toProduct=Theme.Button("Xem sản phẩm",true);toProduct.Click+=(_,_)=>products();buttons.Controls.Add(toProduct);
        var toCategories=Theme.Button("Xem danh mục");toCategories.Click+=(_,_)=>categories();buttons.Controls.Add(toCategories);
        main.Controls.Add(buttons,0,3);
        var grid=Theme.Grid();
        grid.DataSource=Db.Table(@"SELECT TOP 100 sp.MaSP AS [Mã],sp.TenSP AS [Sản phẩm],dm.TenDM AS [Danh mục],
         sp.GiaBan AS [Giá bán],COALESCE(SUM(lo.SLCon),0) AS [Tồn kho],sp.TT AS [Trạng thái]
         FROM SanPham sp JOIN DanhMuc dm ON sp.MaDM=dm.MaDM LEFT JOIN LoHang lo ON lo.MaSP=sp.MaSP
         GROUP BY sp.MaSP,sp.TenSP,dm.TenDM,sp.GiaBan,sp.TT ORDER BY sp.MaSP DESC");
        main.Controls.Add(grid,0,4);
    }
}
