global using System.Drawing;
global using System.Windows.Forms;
using OOAD.Bakery.Forms;
using OOAD.Bakery.Services;

namespace OOAD.Bakery;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Db.Initialize();
            Db.Ping();
            if (!AuthService.HasAccounts())
            {
                using var setup = new FirstRunForm();
                if (setup.ShowDialog() != DialogResult.OK) return;
            }
            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không kết nối được cơ sở dữ liệu.\n" + ex.Message +
              "\n\nKiểm tra SQL Server, chạy script S3_Nhom03_DB.sql và cấu hình appsettings.json.",
              "Lỗi khởi động", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
