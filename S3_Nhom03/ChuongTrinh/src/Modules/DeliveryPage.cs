using System.Data;
using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;
public sealed class DeliveryPage:UserControl
{
    private readonly Account user;
    private readonly DataGridView grid=Theme.Grid();
    public DeliveryPage(Account a)
    {
        user=a;BackColor=Theme.Background;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),RowCount=3};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,62));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title("Quản lý giao nhận"),0,0);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};
        var add=Theme.Button("+ Tạo giao nhận",true);add.Width=170;add.Click+=(_,_)=>Create();bar.Controls.Add(add);
        var status=Theme.Button("Cập nhật trạng thái");status.Width=190;status.Click+=(_,_)=>UpdateStatus();bar.Controls.Add(status);
        var reload=Theme.Button("Tải lại");reload.Click+=(_,_)=>Reload();bar.Controls.Add(reload);
        layout.Controls.Add(bar,0,1);layout.Controls.Add(grid,0,2);Reload();
    }
    private void Reload()
    {
        try{grid.DataSource=Db.Table("SELECT MaGN,MaDH,NguoiNhan,SDTNhan,DiaChiNhan,DonViVC,NguoiGiao,TienCOD,TTGiao,LyDoThatBai FROM GiaoNhan ORDER BY MaGN DESC");}
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void Create()
    {
        using var modal=new Form();var ui=FormsUi.EditorLayout(modal,"Thêm giao nhận",5);
        var order=FormsUi.Combo();order.DataSource=Db.Table(@"SELECT d.MaDH,k.HoTen FROM DonHang d JOIN KhachHang k ON d.MaKH=k.MaKH
            WHERE d.TTDon<>N'Đã hủy' AND NOT EXISTS(SELECT 1 FROM GiaoNhan g WHERE g.MaDH=d.MaDH) ORDER BY d.MaDH DESC");
        order.ValueMember="MaDH";order.DisplayMember="MaDH";
        var name=FormsUi.Field("",100);var phone=FormsUi.Field("",15);
        var address=FormsUi.Field("",255);var carrier=FormsUi.Field("",100);
        FormsUi.AddField(ui,1,"Mã đơn hàng",order);
        FormsUi.AddField(ui,2,"Tên người nhận",name);
        FormsUi.AddField(ui,3,"SĐT nhận",phone);
        FormsUi.AddField(ui,4,"Địa chỉ giao",address);
        FormsUi.AddField(ui,5,"Đơn vị vận chuyển",carrier);
        FormsUi.SaveButton(ui,6,()=>
        {
            try
            {
                if(order.SelectedValue is null || string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(carrier.Text))
                    throw new ArgumentException("Cần chọn đơn hàng, tên người nhận và đơn vị vận chuyển.");
                Db.Run(@"INSERT INTO GiaoNhan(MaDH,NguoiNhan,SDTNhan,DiaChiNhan,DonViVC)
                  VALUES(@dh,@name,@phone,@address,@carrier)",Db.P("@dh",Convert.ToInt32(order.SelectedValue)),
                  Db.P("@name",name.Text.Trim()),Db.P("@phone",phone.Text.Trim()),Db.P("@address",address.Text.Trim()),Db.P("@carrier",carrier.Text.Trim()));
                modal.DialogResult=DialogResult.OK;modal.Close();
            }
            catch(Exception ex){FormsUi.Error(ex);}
        });
        if(modal.ShowDialog(FindForm())==DialogResult.OK)Reload();
    }
    private void UpdateStatus()
    {
        if(grid.CurrentRow?.DataBoundItem is not DataRowView row)return;
        int id=Convert.ToInt32(row["MaGN"]);
        using var modal=new Form();var ui=FormsUi.EditorLayout(modal,"Cập nhật giao nhận",4);
        var next=FormsUi.Combo();next.Items.AddRange(new[]{"Chờ giao","Đang giao","Giao thành công","Giao thất bại"});
        next.SelectedItem=Convert.ToString(row["TTGiao"]);
        var courier=FormsUi.Field(Convert.ToString(row["NguoiGiao"])??"",100);
        var cod=FormsUi.Field(Convert.ToString(row["TienCOD"])??"0");
        var reason=FormsUi.Field(Convert.ToString(row["LyDoThatBai"])??"",255);
        FormsUi.AddField(ui,1,"Trạng thái",next);
        FormsUi.AddField(ui,2,"Người giao",courier);
        FormsUi.AddField(ui,3,"Tiền COD",cod);
        FormsUi.AddField(ui,4,"Lý do thất bại",reason);
        FormsUi.SaveButton(ui,5,()=>
        {
            try
            {
                if(!decimal.TryParse(cod.Text,out decimal amount)||amount<0)throw new ArgumentException("COD không hợp lệ.");
                if(string.IsNullOrWhiteSpace(courier.Text))throw new ArgumentException("Cần nhập người giao.");
                if(next.Text=="Giao thất bại" && string.IsNullOrWhiteSpace(reason.Text))throw new ArgumentException("Cần nhập lý do giao thất bại.");
                Db.Transaction<object?>((conn,tx)=>
                {
                    string previous=Convert.ToString(Db.CommandScalar(conn,tx,"SELECT TTGiao FROM GiaoNhan WITH (UPDLOCK,HOLDLOCK) WHERE MaGN=@id",Db.P("@id",id)))
                        ?? throw new InvalidOperationException("Không tìm thấy phiếu giao nhận.");
                    if(previous is "Giao thành công" or "Giao thất bại")throw new InvalidOperationException("Trạng thái kết thúc không được chuyển ngược.");
                    if(previous=="Chờ giao" && next.Text=="Giao thành công")throw new InvalidOperationException("Chọn Đang giao trước khi hoàn thành.");
                    Db.CommandRun(conn,tx,@"UPDATE GiaoNhan SET TTGiao=@new,NguoiGiao=@shipper,TienCOD=@cod,LyDoThatBai=@reason WHERE MaGN=@id",
                        Db.P("@new",next.Text),Db.P("@shipper",courier.Text.Trim()),Db.P("@cod",amount),
                        Db.P("@reason",next.Text=="Giao thất bại"?reason.Text.Trim():null),Db.P("@id",id));
                    Db.CommandRun(conn,tx,@"INSERT INTO LichSuGiaoNhan(MaGN,TTTruoc,TTSau,MaNV)
                     VALUES(@id,@before,@after,@staff)",Db.P("@id",id),Db.P("@before",previous),
                        Db.P("@after",next.Text),Db.P("@staff",user.EmployeeId));return null;
                });
                modal.DialogResult=DialogResult.OK;modal.Close();
            }
            catch(Exception ex){FormsUi.Error(ex);}
        });
        if(modal.ShowDialog(FindForm())==DialogResult.OK)Reload();
    }
}
