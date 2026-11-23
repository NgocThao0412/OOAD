using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace OOAD.Bakery.Services;
public sealed record Account(int Id, int EmployeeId, string UserName, string Role);
public static class AuthService
{
    public static bool HasAccounts() => Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM TaiKhoan")) > 0;
    private const int Iterations = 210000;
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    private static bool Check(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out int iterations) || iterations < 100000) return false;
        try
        {
            byte[] target = Convert.FromBase64String(parts[3]);
            byte[] computed = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[2]), iterations, HashAlgorithmName.SHA256, target.Length);
            return CryptographicOperations.FixedTimeEquals(computed, target);
        }
        catch (FormatException) { return false; }
    }
    public static void CreateFirstAdmin(string fullName, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || password.Length < 8)
            throw new ArgumentException("Nhập họ tên, tên đăng nhập và mật khẩu tối thiểu 8 ký tự.");
        Db.Transaction<object?>((conn, tx) =>
        {
            if (Convert.ToInt32(Db.CommandScalar(conn,tx,"SELECT COUNT(*) FROM TaiKhoan")) != 0)
                throw new InvalidOperationException("Hệ thống đã có tài khoản; không thể khởi tạo thêm Admin.");
            int employeeId = Convert.ToInt32(Db.CommandScalar(conn,tx,
                "INSERT INTO NhanVien(HoTen) OUTPUT INSERTED.MaNV VALUES (@name)",Db.P("@name",fullName.Trim())));
            Db.CommandRun(conn,tx,
                "INSERT INTO TaiKhoan(TenTK,MatKhau,VaiTro,TT,MaNV) VALUES (@u,@p,N'Admin',N'Hoạt động',@id)",
                Db.P("@u",username.Trim()),Db.P("@p",Hash(password)),Db.P("@id",employeeId));
            return null;
        });
    }
    public static Account? Login(string username, string password)
    {
        var rows = Db.Table("SELECT MaTK,MaNV,TenTK,MatKhau,VaiTro,TT FROM TaiKhoan WHERE TenTK=@u",
            Db.P("@u",username.Trim()));
        if (rows.Rows.Count != 1) return null;
        var row = rows.Rows[0];
        if (!string.Equals(Convert.ToString(row["TT"]),"Hoạt động",StringComparison.OrdinalIgnoreCase)) return null;
        if (!Check(password,Convert.ToString(row["MatKhau"]) ?? "")) return null;
        return new Account(Convert.ToInt32(row["MaTK"]),Convert.ToInt32(row["MaNV"]),
            Convert.ToString(row["TenTK"])!,Convert.ToString(row["VaiTro"])!);
    }
    public static bool CanManageCatalog(Account user) => user.Role is "Admin" or "Quản lý";
}
