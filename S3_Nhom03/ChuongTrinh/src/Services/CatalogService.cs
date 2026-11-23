using System.Data;
namespace OOAD.Bakery.Services;
public static class CatalogService
{
    private static void Demand(Account a)
    {
        if(!AuthService.CanManageCatalog(a)) throw new UnauthorizedAccessException("Tài khoản không có quyền quản lý danh mục và sản phẩm.");
    }
    public static DataTable Categories(string search="") => Db.Table(
        "SELECT MaDM AS [Mã DM],TenDM AS [Tên danh mục],MoTa AS [Mô tả],TT AS [Trạng thái] FROM DanhMuc WHERE TenDM LIKE @s ORDER BY MaDM DESC",
        Db.P("@s","%"+search.Trim()+"%"));
    public static DataTable CategoryOptions() => Db.Table("SELECT MaDM,TenDM FROM DanhMuc WHERE TT=N'Đang dùng' ORDER BY TenDM");
    public static DataTable Products(string search="") => Db.Table(@"
        SELECT sp.MaSP AS [Mã SP],sp.TenSP AS [Tên sản phẩm],sp.MaDM AS [Mã DM],dm.TenDM AS [Danh mục],
          sp.GiaBan AS [Giá bán],sp.TrongLuong AS [Trọng lượng (g)],sp.HSD AS [HSD (ngày)],
          sp.NguongTon AS [Ngưỡng tồn],COALESCE(stock.Total,0) AS [Tồn hiện tại],sp.TT AS [Trạng thái],
          sp.ThanhPhan AS [Thành phần],sp.MoTa AS [Mô tả],sp.HinhAnh AS [Hình ảnh]
        FROM SanPham sp JOIN DanhMuc dm ON dm.MaDM=sp.MaDM
        OUTER APPLY (SELECT SUM(SLCon) AS Total FROM LoHang WHERE MaSP=sp.MaSP) stock
        WHERE sp.TenSP LIKE @s OR dm.TenDM LIKE @s
        ORDER BY sp.MaSP DESC",Db.P("@s","%"+search.Trim()+"%"));
    public static void SaveCategory(Account a,int? id,string name,string description,string status)
    {
        Demand(a); name=name.Trim();description=description.Trim();
        if(name.Length==0 || name.Length>100) throw new ArgumentException("Tên danh mục phải từ 1 đến 100 ký tự.");
        if(description.Length>255) throw new ArgumentException("Mô tả tối đa 255 ký tự.");
        if(status is not ("Đang dùng" or "Ngừng dùng")) throw new ArgumentException("Trạng thái danh mục không hợp lệ.");
        if(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM DanhMuc WHERE TenDM=@n AND (@id IS NULL OR MaDM<>@id)",
            Db.P("@n",name),Db.P("@id",id)))>0) throw new ArgumentException("Tên danh mục đã tồn tại.");
        if(id is null) Db.Run("INSERT INTO DanhMuc(TenDM,MoTa,TT) VALUES(@n,@m,@t)",Db.P("@n",name),Db.P("@m",description),Db.P("@t",status));
        else if(Db.Run("UPDATE DanhMuc SET TenDM=@n,MoTa=@m,TT=@t WHERE MaDM=@id",Db.P("@n",name),Db.P("@m",description),Db.P("@t",status),Db.P("@id",id))==0)
            throw new InvalidOperationException("Danh mục không còn tồn tại.");
    }
    public static string DeleteCategory(Account a,int id)
    {
        Demand(a);
        return Db.Transaction((conn,tx)=>
        {
            int count=Convert.ToInt32(Db.CommandScalar(conn,tx,"SELECT COUNT(*) FROM SanPham WHERE MaDM=@id",Db.P("@id",id)));
            if(count>0)
            {
                Db.CommandRun(conn,tx,"UPDATE DanhMuc SET TT=N'Ngừng dùng' WHERE MaDM=@id",Db.P("@id",id));
                return "Danh mục có sản phẩm nên đã ngừng dùng, không xóa dữ liệu liên quan.";
            }
            Db.CommandRun(conn,tx,"DELETE FROM DanhMuc WHERE MaDM=@id",Db.P("@id",id));
            return "Đã xóa danh mục chưa có sản phẩm.";
        });
    }
    public sealed record ProductInput(int? Id,string Name,int CategoryId,string Image,string Ingredients,string Description,
        decimal? Weight,decimal Price,int? ShelfLife,int LowStock,string Status);
    public static void SaveProduct(Account a,ProductInput p)
    {
        Demand(a);
        string name=p.Name.Trim();
        if(name.Length is <1 or >150) throw new ArgumentException("Tên sản phẩm phải từ 1 đến 150 ký tự.");
        if(p.Price<0 || p.Weight is <=0 || p.ShelfLife is <=0 || p.LowStock<0) throw new ArgumentException("Giá bán, trọng lượng, HSD hoặc ngưỡng tồn không hợp lệ.");
        if(p.Image.Length>255) throw new ArgumentException("Đường dẫn hình ảnh tối đa 255 ký tự.");
        if(p.Status is not ("Đang bán" or "Ẩn" or "Ngừng bán")) throw new ArgumentException("Trạng thái không hợp lệ.");
        if(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM DanhMuc WHERE MaDM=@id AND TT=N'Đang dùng'",Db.P("@id",p.CategoryId)))==0)
            throw new ArgumentException("Danh mục không tồn tại hoặc đã ngừng dùng.");
        const string values="TenSP=@n,MaDM=@dm,HinhAnh=@h,ThanhPhan=@tp,MoTa=@m,TrongLuong=@w,GiaBan=@g,HSD=@hs,NguongTon=@nt,TT=@tt";
        var args=new[]{Db.P("@n",name),Db.P("@dm",p.CategoryId),Db.P("@h",NullIfEmpty(p.Image)),Db.P("@tp",NullIfEmpty(p.Ingredients)),
            Db.P("@m",NullIfEmpty(p.Description)),Db.P("@w",p.Weight),Db.P("@g",p.Price),Db.P("@hs",p.ShelfLife),Db.P("@nt",p.LowStock),Db.P("@tt",p.Status),Db.P("@id",p.Id)};
        if(p.Id is null) Db.Run("INSERT INTO SanPham(TenSP,MaDM,HinhAnh,ThanhPhan,MoTa,TrongLuong,GiaBan,HSD,NguongTon,TT) VALUES(@n,@dm,@h,@tp,@m,@w,@g,@hs,@nt,@tt)",args);
        else if(Db.Run("UPDATE SanPham SET "+values+" WHERE MaSP=@id",args)==0)
            throw new InvalidOperationException("Sản phẩm không còn tồn tại.");
    }
    private static object? NullIfEmpty(string s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
    public static string DeleteProduct(Account a,int id)
    {
        Demand(a);
        return Db.Transaction((conn,tx)=>
        {
            int references=Convert.ToInt32(Db.CommandScalar(conn,tx,@"SELECT
                (SELECT COUNT(*) FROM ChiTietPhieuNhap WHERE MaSP=@id)+
                (SELECT COUNT(*) FROM ChiTietDonHang WHERE MaSP=@id)+
                (SELECT COUNT(*) FROM CauHinhGiaBan WHERE MaSP=@id)",Db.P("@id",id)));
            if(references>0)
            {
                Db.CommandRun(conn,tx,"UPDATE SanPham SET TT=N'Ẩn' WHERE MaSP=@id",Db.P("@id",id));
                return "Sản phẩm đã phát sinh dữ liệu nghiệp vụ: chuyển sang trạng thái Ẩn để bảo toàn lịch sử.";
            }
            Db.CommandRun(conn,tx,"DELETE FROM SanPham WHERE MaSP=@id",Db.P("@id",id));
            return "Đã xóa sản phẩm chưa phát sinh giao dịch.";
        });
    }
}
