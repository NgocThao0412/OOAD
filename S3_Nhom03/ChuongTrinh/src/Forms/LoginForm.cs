using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Forms;
public sealed class LoginForm : Form
{
    private readonly TextBox user = Theme.TextInput();
    private readonly TextBox pass = Theme.TextInput();
    public LoginForm()
    {
        Text = "Sweet Bakery | Đăng nhập";
        Size = new Size(870,520);
        MinimumSize = new Size(730,460);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        var main = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2,Padding=new Padding(45) };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,46));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,54));
        Controls.Add(main);
        var left = new Panel { BackColor=Theme.Lavender,Dock=DockStyle.Fill };
        main.Controls.Add(left,0,0);
        var label = new Label { Text="✿\nSWEET BAKERY\n\nQUẢN LÝ BÁN BÁNH NGỌT",Font=new Font("Segoe UI",20,FontStyle.Bold),
            ForeColor=Theme.AccentDark,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter };
        left.Controls.Add(label);
        var right = new TableLayoutPanel { Dock=DockStyle.Fill, BackColor=Color.White,Padding=new Padding(37,45,35,40),ColumnCount=1,RowCount=8 };
        main.Controls.Add(right,1,0);
        right.RowStyles.Add(new RowStyle(SizeType.Absolute,56));
        right.Controls.Add(Theme.Label("Đăng nhập",22,true,Theme.AccentDark),0,0);
        right.Controls.Add(Theme.Label("Tên đăng nhập"),0,1);
        user.Dock=DockStyle.Fill; right.Controls.Add(user,0,2);
        right.Controls.Add(Theme.Label("Mật khẩu"),0,3);
        pass.UseSystemPasswordChar=true;pass.Dock=DockStyle.Fill;right.Controls.Add(pass,0,4);
        var show = new CheckBox { Text="Hiện mật khẩu",AutoSize=true };
        show.CheckedChanged += (_,_)=>pass.UseSystemPasswordChar=!show.Checked;
        right.Controls.Add(show,0,5);
        var login=Theme.Button("Đăng nhập",true);login.Dock=DockStyle.Fill;
        login.Click+=(_,_)=>SignIn();right.Controls.Add(login,0,6);
        right.Controls.Add(Theme.Label("Đăng nhập bằng tài khoản lưu trong SQL Server",9,false,Theme.Muted),0,7);
        AcceptButton=login;
    }
    private void SignIn()
    {
        if(string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(pass.Text))
        { MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu.");return; }
        try
        {
            var account=AuthService.Login(user.Text,pass.Text);
            if(account is null){MessageBox.Show("Tài khoản, mật khẩu hoặc trạng thái không hợp lệ.");return;}
            Hide(); using(var main=new MainForm(account)) main.ShowDialog(this); Show();pass.Clear();
        }
        catch(Exception ex){MessageBox.Show("Lỗi kết nối hoặc xử lý dữ liệu: "+ex.Message);}
    }
}
