using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Forms;
public sealed class FirstRunForm : Form
{
    public FirstRunForm()
    {
        Text = "Thiết lập quản trị viên đầu tiên";
        Size = new Size(490, 345);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30), RowCount = 8, ColumnCount = 1 };
        Controls.Add(layout);
        layout.Controls.Add(Theme.Label("Tạo tài khoản quản trị ban đầu",17,true,Theme.AccentDark));
        layout.Controls.Add(Theme.Label("Họ và tên"));
        var name = Theme.TextInput(); name.Dock = DockStyle.Fill; layout.Controls.Add(name);
        layout.Controls.Add(Theme.Label("Tên đăng nhập"));
        var username = Theme.TextInput(); username.Dock = DockStyle.Fill; layout.Controls.Add(username);
        layout.Controls.Add(Theme.Label("Mật khẩu (ít nhất 8 ký tự)"));
        var password = Theme.TextInput(); password.UseSystemPasswordChar = true; password.Dock = DockStyle.Fill; layout.Controls.Add(password);
        var save = Theme.Button("Tạo quản trị viên",true); save.Dock = DockStyle.Fill;
        save.Click += (_, _) =>
        {
            try { AuthService.CreateFirstAdmin(name.Text,username.Text,password.Text); DialogResult = DialogResult.OK; Close(); }
            catch(Exception ex) { MessageBox.Show(ex.Message,"Không thể tạo tài khoản",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        };
        layout.Controls.Add(save);
        AcceptButton = save;
    }
}
