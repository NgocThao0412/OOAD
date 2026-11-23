using System.Data;
using Microsoft.Data.SqlClient;
namespace OOAD.Bakery.Services;
public static class OperationsService
{
    private static void Demand(Account user)
    {
        if(!AuthService.CanManageCatalog(user))throw new UnauthorizedAccessException("Không có quyền thực hiện nghiệp vụ này.");
    }
    public static DataTable Suppliers()=>Db.Table("SELECT MaNCC,TenNCC FROM NhaCungCap ORDER BY TenNCC");
    public static DataTable Customers()=>Db.Table("SELECT MaKH,HoTen FROM KhachHang ORDER BY HoTen");
    public static DataTable Products()=>Db.Table("SELECT MaSP,TenSP,GiaBan FROM SanPham WHERE TT=N'Đang bán' ORDER BY TenSP");
    public static int Receive(Account user,int supplierId,int productId,int quantity,decimal cost,DateTime expiry)
    {
        Demand(user);
        if(quantity<=0 || cost<0 || expiry.Date<DateTime.Today)throw new ArgumentException("Số lượng, giá vốn hoặc hạn sử dụng không hợp lệ.");
        return Db.Transaction((conn,tx)=>
        {
            if(Convert.ToInt32(Db.CommandScalar(conn,tx,"SELECT COUNT(*) FROM NhaCungCap WHERE MaNCC=@id",Db.P("@id",supplierId)))==0)
                throw new ArgumentException("Nhà cung cấp không tồn tại.");
            if(Convert.ToInt32(Db.CommandScalar(conn,tx,"SELECT COUNT(*) FROM SanPham WHERE MaSP=@id AND TT=N'Đang bán'",Db.P("@id",productId)))==0)
                throw new ArgumentException("Sản phẩm không tồn tại hoặc đã ngừng bán.");
            DateTime now=DateTime.Now;
            int receipt=Convert.ToInt32(Db.CommandScalar(conn,tx,
                "INSERT INTO PhieuNhapKho(NgayNhap,MaNCC,MaNV,TongTien,TT) OUTPUT INSERTED.MaPN VALUES(@now,@supplier,@employee,@total,N'Hoàn tất')",
                Db.P("@now",now),Db.P("@supplier",supplierId),Db.P("@employee",user.EmployeeId),Db.P("@total",quantity*cost)));
            int line=Convert.ToInt32(Db.CommandScalar(conn,tx,
                "INSERT INTO ChiTietPhieuNhap(MaPN,MaSP,SLNhap,GiaVon) OUTPUT INSERTED.MaCTPN VALUES(@pn,@sp,@q,@cost)",
                Db.P("@pn",receipt),Db.P("@sp",productId),Db.P("@q",quantity),Db.P("@cost",cost)));
            Db.CommandRun(conn,tx,@"INSERT INTO LoHang(MaCTPN,MaSP,NgayNhap,NgayHetHan,SLNhap,SLCon,GiaVon)
                VALUES(@line,@sp,@now,@expiry,@q,@q,@cost)",Db.P("@line",line),Db.P("@sp",productId),
                Db.P("@now",now),Db.P("@expiry",expiry.Date.AddDays(1).AddTicks(-1)),Db.P("@q",quantity),Db.P("@cost",cost));
            return receipt;
        });
    }
    public static int CreateCounterOrder(Account user,int customerId,int productId,int quantity,string paymentMethod,bool paid)
    {
        if(quantity<=0)throw new ArgumentException("Số lượng đặt phải lớn hơn 0.");
        if(string.IsNullOrWhiteSpace(paymentMethod))throw new ArgumentException("Chọn phương thức thanh toán.");
        return Db.Transaction((conn,tx)=>
        {
            if(Convert.ToInt32(Db.CommandScalar(conn,tx,"SELECT COUNT(*) FROM KhachHang WHERE MaKH=@id",Db.P("@id",customerId)))==0)
                throw new ArgumentException("Khách hàng không tồn tại.");
            object? priceObj=Db.CommandScalar(conn,tx,"SELECT GiaBan FROM SanPham WHERE MaSP=@sp AND TT=N'Đang bán'",Db.P("@sp",productId));
            if(priceObj is null)throw new ArgumentException("Sản phẩm không tồn tại hoặc không còn bán.");
            decimal price=Convert.ToDecimal(priceObj);
            var lots=new List<(int id,int quantity)>();
            using(var command=new SqlCommand(@"SELECT MaLo,SLCon FROM LoHang WITH (UPDLOCK,HOLDLOCK)
                WHERE MaSP=@sp AND SLCon>0 AND (NgayHetHan IS NULL OR NgayHetHan>=@today)
                ORDER BY NgayNhap,MaLo",conn,tx))
            {
                command.Parameters.Add(Db.P("@sp",productId));command.Parameters.Add(Db.P("@today",DateTime.Today));
                using var rd=command.ExecuteReader();
                while(rd.Read())lots.Add((rd.GetInt32(0),rd.GetInt32(1)));
            }
            if(lots.Sum(x=>x.quantity)<quantity)throw new InvalidOperationException("Số lượng tồn khả dụng không đủ, hoặc hàng đã hết hạn.");
            int orderId=Convert.ToInt32(Db.CommandScalar(conn,tx,@"INSERT INTO DonHang
                (MaKH,MaNV,NgayDat,TongTien,PTTT,TTThanhToan,TTDon,GhiChu)
                OUTPUT INSERTED.MaDH VALUES(@kh,@nv,@now,@total,@method,@payment,N'Đã xử lý',N'Bán tại quầy, đã xuất kho FIFO')",
                Db.P("@kh",customerId),Db.P("@nv",user.EmployeeId),Db.P("@now",DateTime.Now),
                Db.P("@total",price*quantity),Db.P("@method",paymentMethod),Db.P("@payment",paid?"Đã thanh toán":"Chưa thanh toán")));
            int detailId=Convert.ToInt32(Db.CommandScalar(conn,tx,
                "INSERT INTO ChiTietDonHang(MaDH,MaSP,SL,DonGia) OUTPUT INSERTED.MaCTDH VALUES(@dh,@sp,@q,@price)",
                Db.P("@dh",orderId),Db.P("@sp",productId),Db.P("@q",quantity),Db.P("@price",price)));
            int rest=quantity;
            foreach(var lot in lots)
            {
                int picked=Math.Min(rest,lot.quantity);
                if(picked<=0)continue;
                if(Db.CommandRun(conn,tx,"UPDATE LoHang SET SLCon=SLCon-@q WHERE MaLo=@lo AND SLCon>=@q",
                    Db.P("@q",picked),Db.P("@lo",lot.id))!=1)throw new InvalidOperationException("Lô hàng đã thay đổi tồn kho.");
                Db.CommandRun(conn,tx,"INSERT INTO PhanBoXuatLo(MaCTDH,MaLo,SLXuat) VALUES(@detail,@lo,@q)",
                    Db.P("@detail",detailId),Db.P("@lo",lot.id),Db.P("@q",picked));
                rest-=picked;if(rest==0)break;
            }
            return orderId;
        });
    }
    public static DataTable StockByPeriod(DateTime start,DateTime end)
    {
        if(start.Date>end.Date)throw new ArgumentException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        // Dung lich su nhap da hoan tat va xuat theo phan bo FIFO; khong tinh phieu nhap nhap/huy.
        return Db.Table(@"SELECT s.MaSP,s.TenSP,
            (SELECT COALESCE(SUM(ct.SLNhap),0) FROM ChiTietPhieuNhap ct JOIN PhieuNhapKho pn ON ct.MaPN=pn.MaPN
                WHERE ct.MaSP=s.MaSP AND pn.TT=N'Hoàn tất' AND pn.NgayNhap<@start)
            - (SELECT COALESCE(SUM(pb.SLXuat),0) FROM PhanBoXuatLo pb JOIN LoHang lo ON lo.MaLo=pb.MaLo
                WHERE lo.MaSP=s.MaSP AND pb.NgayXuat<@start) AS [Tồn đầu kỳ],
            (SELECT COALESCE(SUM(ct.SLNhap),0) FROM ChiTietPhieuNhap ct JOIN PhieuNhapKho pn ON ct.MaPN=pn.MaPN
                WHERE ct.MaSP=s.MaSP AND pn.TT=N'Hoàn tất' AND pn.NgayNhap>=@start AND pn.NgayNhap<@end) AS [Nhập trong kỳ],
            (SELECT COALESCE(SUM(pb.SLXuat),0) FROM PhanBoXuatLo pb JOIN LoHang lo ON lo.MaLo=pb.MaLo
                WHERE lo.MaSP=s.MaSP AND pb.NgayXuat>=@start AND pb.NgayXuat<@end) AS [Xuất trong kỳ],
            (SELECT COALESCE(SUM(ct.SLNhap),0) FROM ChiTietPhieuNhap ct JOIN PhieuNhapKho pn ON ct.MaPN=pn.MaPN
                WHERE ct.MaSP=s.MaSP AND pn.TT=N'Hoàn tất' AND pn.NgayNhap<@end)
            - (SELECT COALESCE(SUM(pb.SLXuat),0) FROM PhanBoXuatLo pb JOIN LoHang lo ON lo.MaLo=pb.MaLo
                WHERE lo.MaSP=s.MaSP AND pb.NgayXuat<@end) AS [Tồn cuối kỳ]
            FROM SanPham s ORDER BY s.TenSP",
            Db.P("@start",start.Date),Db.P("@end",end.Date.AddDays(1)));
    }
}
