using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace OOAD.Bakery.Services;
public static class Db
{
    private static string _connectionString = "";
    public static void Initialize()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var json = JsonDocument.Parse(File.ReadAllText(file));
        _connectionString = json.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
            ?? throw new InvalidOperationException("Thiếu chuỗi kết nối CSDL.");
    }
    public static SqlConnection Connect()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
    public static void Ping()
    {
        using var c = Connect();
        using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.SanPham',N'U') IS NOT NULL THEN 1 ELSE 0 END", c);
        if (Convert.ToInt32(cmd.ExecuteScalar()) != 1) throw new InvalidOperationException("Chưa tạo đủ bảng. Hãy chạy file SQL theo báo cáo.");
    }
    public static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);
    public static DataTable Table(string sql, params SqlParameter[] parameters)
    {
        using var connection = Connect();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        using var reader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }
    public static object? Scalar(string sql, params SqlParameter[] parameters)
    {
        using var connection = Connect();
        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteScalar();
    }
    public static int Run(string sql, params SqlParameter[] parameters)
    {
        using var connection = Connect();
        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteNonQuery();
    }
    public static T Transaction<T>(Func<SqlConnection,SqlTransaction,T> action)
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try { var result = action(connection, transaction); transaction.Commit(); return result; }
        catch { transaction.Rollback(); throw; }
    }
    public static object? CommandScalar(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
    {
        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteScalar();
    }
    public static int CommandRun(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
    {
        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.Parameters.AddRange(parameters);
        return cmd.ExecuteNonQuery();
    }
}
