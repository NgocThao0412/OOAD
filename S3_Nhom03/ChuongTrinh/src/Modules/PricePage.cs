using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;
public sealed class PricePage:UserControl
{
    private readonly Account account;
    private readonly DataGridView grid=Theme.Grid();
    private readonly TextBox search=Theme.TextInput(250);
    public PricePage(Account a)
    {
        account=a;BackColor=Theme.Background;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),RowCount=4};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title("Quản lý cấu hình giá bán"),0,0);
        var label=Theme.Label("Giá vốn dùng để tính là giá vốn của lô FIFO còn hàng sớm nhất; tỷ lệ % là lãi cộng trên giá vốn.",9,false,Theme.Muted);
        layout.Controls.Add(label,0,1);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};bar.Controls.Add(search);
        var button=Theme.Button("Tra cứu");button.Click+=(_,_)=>Reload();bar.Controls.Add(button);
        var change=Theme.Button("Thiết lập tỷ lệ",true);change.Width=175;
        change.Enabled=AuthService.CanManageCatalog(account);change.Click+=(_,_)=>Configure();bar.Controls.Add(change);
        layout.Controls.Add(bar,0,2);layout.Controls.Add(grid,0,3);Reload();
    }
    private void Reload()
    {
        try{grid.DataSource=Db.Table(@"SELECT sp.MaSP,sp.TenSP,sp.GiaBan AS GiaBanHienTai,
             (SELECT TOP 1 lo.GiaVon FROM LoHang lo WHERE lo.MaSP=sp.MaSP AND lo.SLCon>0
              AND (lo.NgayHetHan IS NULL OR lo.NgayHetHan>=GETDATE()) ORDER BY lo.NgayNhap,lo.MaLo) AS GiaVonFIFO,
             (SELECT TOP 1 cg.TyLeLN FROM CauHinhGiaBan cg WHERE cg.MaSP=sp.MaSP ORDER BY cg.NgayAD DESC,cg.MaCH DESC) AS TyLeMoiNhat
             FROM SanPham sp WHERE sp.TenSP LIKE @q ORDER BY sp.MaSP DESC",Db.P("@q","%"+search.Text.Trim()+"%"));}
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void Configure()
    {
        if(grid.CurrentRow?.DataBoundItem is not System.Data.DataRowView row)return;
        int id=Convert.ToInt32(row["MaSP"]);
        using var form=new Form();var ui=FormsUi.EditorLayout(form,"Thiết lập tỷ lệ lợi nhuận",2);
        var text=FormsUi.Field(Convert.ToString(row["TyLeMoiNhat"])??"0");
        FormsUi.AddField(ui,1,"Tỷ lệ lãi (ví dụ 20 = 20%)",text);
        FormsUi.AddField(ui,2,"Sản phẩm",new Label{Text=Convert.ToString(row["TenSP"]),Dock=DockStyle.Fill});
        FormsUi.SaveButton(ui,3,()=>
        {
            try
            {
                if(!decimal.TryParse(text.Text,out decimal percent) || percent<0 || percent>10000)
                    throw new ArgumentException("Tỷ lệ lợi nhuận phải là số từ 0 đến 10000 (%).");
                // Luu 0.20 cho dau vao 20 (%) theo kieu DECIMAL(8,4).
                decimal rate=percent/100m;
                Db.Transaction<object?>((conn,tx)=>
                {
                    object? cost=Db.CommandScalar(conn,tx,@"SELECT TOP 1 GiaVon FROM LoHang WITH (UPDLOCK,HOLDLOCK)
                        WHERE MaSP=@id AND SLCon>0 AND (NgayHetHan IS NULL OR NgayHetHan>=GETDATE())
                        ORDER BY NgayNhap,MaLo",Db.P("@id",id));
                    if(cost is null)throw new InvalidOperationException("Chưa có lô còn hàng hợp lệ để xác định giá vốn FIFO.");
                    decimal price=Math.Round(Convert.ToDecimal(cost)*(1m+rate),2,MidpointRounding.AwayFromZero);
                    Db.CommandRun(conn,tx,"INSERT INTO CauHinhGiaBan(MaSP,TyLeLN,MaNV) VALUES(@id,@rate,@employee)",
                        Db.P("@id",id),Db.P("@rate",rate),Db.P("@employee",account.EmployeeId));
                    Db.CommandRun(conn,tx,"UPDATE SanPham SET GiaBan=@price WHERE MaSP=@id",Db.P("@price",price),Db.P("@id",id));
                    return null;
                });
                form.DialogResult=DialogResult.OK;form.Close();
            }
            catch(Exception ex){FormsUi.Error(ex);}
        });
        if(form.ShowDialog(FindForm())==DialogResult.OK)Reload();
    }
}
