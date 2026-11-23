using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;
public sealed class OperationPage:UserControl
{
    private readonly bool receiving;
    private readonly Account account;
    private readonly DataGridView grid=Theme.Grid();
    public OperationPage(Account a,bool receipt)
    {
        account=a;receiving=receipt;BackColor=Theme.Background;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),RowCount=3};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title(receiving?"Quản lý nhập kho":"Quản lý đơn hàng"),0,0);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};
        var create=Theme.Button(receiving?"+ Nhập hàng":"+ Bán tại quầy",true);create.Width=150;
        create.Click+=(_,_)=>Add();bar.Controls.Add(create);
        var refresh=Theme.Button("Tải lại");refresh.Click+=(_,_)=>Reload();bar.Controls.Add(refresh);
        layout.Controls.Add(bar,0,1);layout.Controls.Add(grid,0,2);Reload();
    }
    private void Reload()
    {
        try
        {
            grid.DataSource=receiving?Db.Table(@"SELECT pn.MaPN,pn.NgayNhap,n.TenNCC,nv.HoTen,pn.TongTien,pn.TT
             FROM PhieuNhapKho pn JOIN NhaCungCap n ON n.MaNCC=pn.MaNCC JOIN NhanVien nv ON nv.MaNV=pn.MaNV ORDER BY pn.MaPN DESC"):
             Db.Table(@"SELECT dh.MaDH,k.HoTen,dh.NgayDat,dh.TongTien,dh.PTTT,dh.TTThanhToan,dh.TTDon
             FROM DonHang dh JOIN KhachHang k ON dh.MaKH=k.MaKH ORDER BY dh.MaDH DESC");
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void Add()
    {
        try
        {
            using var form=new Form();
            var ui=FormsUi.EditorLayout(form,receiving?"Lập phiếu nhập và tạo lô":"Lập đơn bán tại quầy",5);
            var parties=receiving?OperationsService.Suppliers():OperationsService.Customers();
            var products=OperationsService.Products();
            if(parties.Rows.Count==0 || products.Rows.Count==0)
                throw new InvalidOperationException("Cần có đối tác và sản phẩm đang bán trước khi lập chứng từ.");
            var party=FormsUi.Combo();party.DataSource=parties;
            party.ValueMember=receiving?"MaNCC":"MaKH";party.DisplayMember=receiving?"TenNCC":"HoTen";
            var product=FormsUi.Combo();product.DataSource=products;product.ValueMember="MaSP";product.DisplayMember="TenSP";
            var count=FormsUi.Field("1");var extra=FormsUi.Field(receiving?"0":"Tiền mặt");
            Control last;
            if(receiving)
            {
                last=new DateTimePicker{Dock=DockStyle.Fill,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(2)};
            }
            else
            {
                var paid=FormsUi.Combo();paid.Items.AddRange(new[]{"Đã thanh toán","Chưa thanh toán"});paid.SelectedIndex=0;last=paid;
            }
            FormsUi.AddField(ui,1,receiving?"Nhà cung cấp":"Khách hàng",party);
            FormsUi.AddField(ui,2,"Sản phẩm",product);
            FormsUi.AddField(ui,3,"Số lượng",count);
            FormsUi.AddField(ui,4,receiving?"Giá vốn / chiếc":"Phương thức TT",extra);
            FormsUi.AddField(ui,5,receiving?"Ngày hết hạn":"Trạng thái TT",last);
            FormsUi.SaveButton(ui,6,()=>
            {
                try
                {
                    if(!int.TryParse(count.Text,out int qty)||qty<=0)throw new ArgumentException("Số lượng phải là số nguyên dương.");
                    int partyId=Convert.ToInt32(party.SelectedValue),productId=Convert.ToInt32(product.SelectedValue);
                    int id;
                    if(receiving)
                    {
                        if(!decimal.TryParse(extra.Text,out decimal cost))throw new ArgumentException("Giá vốn không hợp lệ.");
                        id=OperationsService.Receive(account,partyId,productId,qty,cost,((DateTimePicker)last).Value);
                    }
                    else id=OperationsService.CreateCounterOrder(account,partyId,productId,qty,extra.Text,
                        ((ComboBox)last).Text=="Đã thanh toán");
                    MessageBox.Show($"Đã lưu chứng từ mã {id} vào SQL Server.","Thành công");
                    form.DialogResult=DialogResult.OK;form.Close();
                }
                catch(Exception ex){FormsUi.Error(ex);}
            });
            if(form.ShowDialog(FindForm())==DialogResult.OK)Reload();
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
}
public sealed class StockReportPage:UserControl
{
    private readonly DataGridView grid=Theme.Grid();
    private readonly DateTimePicker start=new(){Format=DateTimePickerFormat.Short};
    private readonly DateTimePicker end=new(){Format=DateTimePickerFormat.Short};
    public StockReportPage()
    {
        BackColor=Theme.Background;
        start.Value=DateTime.Today.AddDays(-30);end.Value=DateTime.Today;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),RowCount=3};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title("Báo cáo Nhập – Xuất – Tồn theo kỳ"),0,0);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};
        bar.Controls.Add(Theme.Label("Từ ngày:"));bar.Controls.Add(start);
        bar.Controls.Add(Theme.Label("Đến ngày:"));bar.Controls.Add(end);
        var button=Theme.Button("Xem báo cáo",true);button.Click+=(_,_)=>Reload();bar.Controls.Add(button);
        layout.Controls.Add(bar,0,1);layout.Controls.Add(grid,0,2);Reload();
    }
    private void Reload()
    {
        try{grid.DataSource=OperationsService.StockByPeriod(start.Value,end.Value);}
        catch(Exception ex){FormsUi.Error(ex);}
    }
}
