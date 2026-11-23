using System.Data;
using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;
public sealed class UsersPage:UserControl
{
    private readonly Account account;
    private readonly DataGridView grid=Theme.Grid();
    public UsersPage(Account a)
    {
        account=a;BackColor=Theme.Background;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),RowCount=3};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title("Quản lý tài khoản nhân viên"),0,0);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};
        var add=Theme.Button("+ Tạo tài khoản",true);add.Width=175;add.Enabled=a.Role=="Admin";
        add.Click+=(_,_)=>CreateUser();bar.Controls.Add(add);
        var lockAccount=Theme.Button("Khóa / Mở khóa");lockAccount.Width=165;lockAccount.Enabled=a.Role=="Admin";
        lockAccount.Click+=(_,_)=>Toggle();bar.Controls.Add(lockAccount);
        var reload=Theme.Button("Làm mới");reload.Click+=(_,_)=>Reload();bar.Controls.Add(reload);
        layout.Controls.Add(bar,0,1);layout.Controls.Add(grid,0,2);Reload();
    }
    private void Reload()
    {
        try{grid.DataSource=Db.Table("SELECT t.MaTK,t.TenTK,t.VaiTro,t.TT,t.NgayTao,n.HoTen,n.MaNV FROM TaiKhoan t JOIN NhanVien n ON t.MaNV=n.MaNV ORDER BY t.MaTK DESC");}
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void Toggle()
    {
        if(grid.CurrentRow?.DataBoundItem is not DataRowView row)return;
        int id=Convert.ToInt32(row["MaTK"]);
        if(id==account.Id){MessageBox.Show("Không thể tự khóa tài khoản đang đăng nhập.");return;}
        try
        {
            if(account.Role!="Admin")throw new UnauthorizedAccessException();
            Db.Run("UPDATE TaiKhoan SET TT=CASE WHEN TT=N'Hoạt động' THEN N'Đã khóa' ELSE N'Hoạt động' END WHERE MaTK=@id",Db.P("@id",id));Reload();
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void CreateUser()
    {
        using var form=new Form();var ui=FormsUi.EditorLayout(form,"Tạo tài khoản nhân viên",5);
        var fullname=FormsUi.Field("",100);var username=FormsUi.Field("",60);
        var pw=FormsUi.Field();pw.UseSystemPasswordChar=true;
        var role=FormsUi.Combo();role.Items.AddRange(new[]{"Admin","Quản lý","Nhân viên"});role.SelectedIndex=2;
        var email=FormsUi.Field("",120);
        FormsUi.AddField(ui,1,"Họ tên *",fullname);
        FormsUi.AddField(ui,2,"Tên đăng nhập *",username);
        FormsUi.AddField(ui,3,"Mật khẩu *",pw);
        FormsUi.AddField(ui,4,"Vai trò",role);
        FormsUi.AddField(ui,5,"Email",email);
        FormsUi.SaveButton(ui,6,()=>
        {
            try
            {
                if(account.Role!="Admin")throw new UnauthorizedAccessException("Chỉ Admin có quyền tạo tài khoản.");
                if(string.IsNullOrWhiteSpace(fullname.Text) || string.IsNullOrWhiteSpace(username.Text) || pw.Text.Length<8)
                    throw new ArgumentException("Họ tên, tên đăng nhập bắt buộc; mật khẩu tối thiểu 8 ký tự.");
                Db.Transaction<object?>((conn,tx)=>
                {
                    int employeeId=Convert.ToInt32(Db.CommandScalar(conn,tx,
                        "INSERT INTO NhanVien(HoTen,Email) OUTPUT INSERTED.MaNV VALUES(@name,@email)",
                        Db.P("@name",fullname.Text.Trim()),Db.P("@email",email.Text.Trim())));
                    Db.CommandRun(conn,tx,"INSERT INTO TaiKhoan(TenTK,MatKhau,VaiTro,MaNV) VALUES(@user,@hash,@role,@id)",
                        Db.P("@user",username.Text.Trim()),Db.P("@hash",AuthService.Hash(pw.Text)),
                        Db.P("@role",role.Text),Db.P("@id",employeeId));return null;
                });
                form.DialogResult=DialogResult.OK;form.Close();
            }
            catch(Exception ex){FormsUi.Error(ex);}
        });
        if(form.ShowDialog(FindForm())==DialogResult.OK)Reload();
    }
}
