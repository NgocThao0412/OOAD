using System.Data;
using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;

public sealed class ContactsPage:UserControl
{
    private readonly Account current;
    private readonly string table,pk,nameField;
    private readonly DataGridView grid=Theme.Grid();
    private readonly TextBox find=Theme.TextInput(260);
    public ContactsPage(Account current,string kind)
    {
        this.current=current;
        (table,pk,nameField)=kind switch
        {
            "Khách hàng" => ("KhachHang","MaKH","HoTen"),
            "Nhà cung cấp" => ("NhaCungCap","MaNCC","TenNCC"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        BackColor=Theme.Background;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),RowCount=3};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title("Quản lý "+kind.ToLowerInvariant()),0,0);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};bar.Controls.Add(find);
        var search=Theme.Button("Tìm kiếm");search.Click+=(_,_)=>Reload();bar.Controls.Add(search);
        var create=Theme.Button("+ Thêm",true);bar.Controls.Add(create);
        var update=Theme.Button("Sửa");bar.Controls.Add(update);
        var delete=Theme.Button("Xóa");bar.Controls.Add(delete);
        layout.Controls.Add(bar,0,1);layout.Controls.Add(grid,0,2);
        bool allowed=AuthService.CanManageCatalog(current);create.Enabled=update.Enabled=delete.Enabled=allowed;
        create.Click+=(_,_)=>Edit(null);
        update.Click+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is DataRowView row)Edit(row);};
        delete.Click+=(_,_)=>Delete();
        Reload();
    }
    private void Reload()
    {
        try { grid.DataSource=Db.Table($"SELECT {pk},{nameField},SDT,Email,DiaChi FROM {table} WHERE {nameField} LIKE @q ORDER BY {pk} DESC",Db.P("@q","%"+find.Text.Trim()+"%")); }
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void Edit(DataRowView? row)
    {
        int? id=row is null?null:Convert.ToInt32(row[pk]);
        using var form=new Form();
        var ui=FormsUi.EditorLayout(form,id is null?"Thêm đối tác":"Sửa đối tác",4);
        var name=FormsUi.Field(row is null?"":Convert.ToString(row[nameField])??"",table=="NhaCungCap"?150:100);
        var phone=FormsUi.Field(row is null?"":Convert.ToString(row["SDT"])??"",15);
        var email=FormsUi.Field(row is null?"":Convert.ToString(row["Email"])??"",120);
        var address=FormsUi.Field(row is null?"":Convert.ToString(row["DiaChi"])??"",255);
        FormsUi.AddField(ui,1,"Họ tên / tên NCC *",name);
        FormsUi.AddField(ui,2,"Số điện thoại",phone);
        FormsUi.AddField(ui,3,"Email",email);
        FormsUi.AddField(ui,4,"Địa chỉ",address);
        FormsUi.SaveButton(ui,5,()=>
        {
            try
            {
                if(!AuthService.CanManageCatalog(current))throw new UnauthorizedAccessException("Không có quyền chỉnh sửa.");
                if(string.IsNullOrWhiteSpace(name.Text))throw new ArgumentException("Tên không được để trống.");
                if(phone.Text.Length>15 || email.Text.Length>120 || address.Text.Length>255)throw new ArgumentException("Dữ liệu vượt độ dài cho phép.");
                var args=new[]{Db.P("@name",name.Text.Trim()),Db.P("@phone",Null(phone.Text)),Db.P("@email",Null(email.Text)),Db.P("@address",Null(address.Text)),Db.P("@id",id)};
                string sql=id is null?$"INSERT INTO {table}({nameField},SDT,Email,DiaChi) VALUES(@name,@phone,@email,@address)":
                    $"UPDATE {table} SET {nameField}=@name,SDT=@phone,Email=@email,DiaChi=@address WHERE {pk}=@id";
                Db.Run(sql,args);form.DialogResult=DialogResult.OK;form.Close();
            }
            catch(Exception ex){FormsUi.Error(ex);}
        });
        if(form.ShowDialog(FindForm())==DialogResult.OK)Reload();
    }
    private static object? Null(string text)=>string.IsNullOrWhiteSpace(text)?null:text.Trim();
    private void Delete()
    {
        if(grid.CurrentRow?.DataBoundItem is not DataRowView row)return;
        if(MessageBox.Show("Xóa dữ liệu đối tác? Dữ liệu đã phát sinh giao dịch sẽ không thể xóa.","Xác nhận",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        try
        {
            if(!AuthService.CanManageCatalog(current))throw new UnauthorizedAccessException("Không có quyền xóa.");
            Db.Run($"DELETE FROM {table} WHERE {pk}=@id",Db.P("@id",row[pk]));Reload();
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
}
