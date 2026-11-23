using System.Data;
using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;
public sealed class CatalogPage : UserControl
{
    private readonly Account account;
    private readonly bool products;
    private readonly TextBox filter=Theme.TextInput(320);
    private readonly DataGridView grid=Theme.Grid();
    private readonly Button create=Theme.Button("+ Thêm",true),edit=Theme.Button("Sửa"),delete=Theme.Button("Xóa / Ẩn");
    public CatalogPage(Account a,bool isProducts)
    {
        account=a;products=isProducts;Dock=DockStyle.Fill;BackColor=Theme.Background;
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=3,Padding=new Padding(14),ColumnCount=1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,65));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title(products?"Quản lý sản phẩm bánh":"Quản lý danh mục sản phẩm"),0,0);
        var toolbar=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
        filter.PlaceholderText=products?"Tìm sản phẩm hoặc danh mục...":"Tìm tên danh mục...";
        toolbar.Controls.Add(filter);toolbar.Controls.Add(Theme.Button("Tìm kiếm"));
        var search=(Button)toolbar.Controls[1];search.Click+=(_,_)=>Reload();
        var refresh=Theme.Button("Làm mới");refresh.Click+=(_,_)=>{filter.Clear();Reload();};toolbar.Controls.Add(refresh);
        toolbar.Controls.Add(create);toolbar.Controls.Add(edit);toolbar.Controls.Add(delete);
        layout.Controls.Add(toolbar,0,1);
        layout.Controls.Add(grid,0,2);
        bool canWrite=AuthService.CanManageCatalog(account);
        create.Enabled=edit.Enabled=delete.Enabled=canWrite;
        create.Click+=(_,_)=>OpenEditor(null);
        edit.Click+=(_,_)=>{if(Selected() is DataRowView row)OpenEditor(row);};
        delete.Click+=(_,_)=>Delete();
        filter.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Reload();}};
        grid.CellDoubleClick+=(_,_)=>{if(canWrite && Selected() is DataRowView row) OpenEditor(row);};
        Reload();
    }
    private DataRowView? Selected()=>grid.CurrentRow?.DataBoundItem as DataRowView;
    private void Reload()
    {
        try
        {
            grid.DataSource=products?CatalogService.Products(filter.Text):CatalogService.Categories(filter.Text);
            if(products)
            {
                foreach(var hidden in new[]{"Mã DM","Thành phần","Mô tả","Hình ảnh"})
                    if(grid.Columns.Contains(hidden)) grid.Columns[hidden]!.Visible=false;
                if(grid.Columns.Contains("Giá bán")) grid.Columns["Giá bán"]!.DefaultCellStyle.Format="N0";
            }
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void OpenEditor(DataRowView? row)
    {
        try
        {
            using Form modal=products?new ProductEditor(account,row):new CategoryEditor(account,row);
            if(modal.ShowDialog(FindForm())==DialogResult.OK)Reload();
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
    private void Delete()
    {
        var row=Selected();if(row is null){MessageBox.Show("Chọn một dòng trước.");return;}
        if(MessageBox.Show("Xác nhận xóa/ẩn mục đang chọn?", "Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        try
        {
            int id=Convert.ToInt32(row[products?"Mã SP":"Mã DM"]);
            string text=products?CatalogService.DeleteProduct(account,id):CatalogService.DeleteCategory(account,id);
            MessageBox.Show(text,"Kết quả");Reload();
        }
        catch(Exception ex){FormsUi.Error(ex);}
    }
}
public sealed class CategoryEditor:Form
{
    public CategoryEditor(Account account,DataRowView? row)
    {
        int? id=row is null?null:Convert.ToInt32(row["Mã DM"]);
        var ui=FormsUi.EditorLayout(this,id is null?"Thêm danh mục":"Sửa danh mục",3);
        var name=FormsUi.Field(row is null?"":Convert.ToString(row["Tên danh mục"])??"",100);
        var desc=FormsUi.Field(row is null?"":Convert.ToString(row["Mô tả"])??"",255);
        var status=FormsUi.Combo();status.Items.AddRange(new[]{"Đang dùng","Ngừng dùng"});
        status.SelectedItem=row is null?"Đang dùng":Convert.ToString(row["Trạng thái"]);
        FormsUi.AddField(ui,1,"Tên danh mục *",name);
        FormsUi.AddField(ui,2,"Mô tả",desc);
        FormsUi.AddField(ui,3,"Trạng thái",status);
        var b=FormsUi.SaveButton(ui,4,()=>
        {
            try{CatalogService.SaveCategory(account,id,name.Text,desc.Text,status.Text);DialogResult=DialogResult.OK;Close();}
            catch(Exception ex){FormsUi.Error(ex);}
        });
        AcceptButton=b;
    }
}
public sealed class ProductEditor:Form
{
    public ProductEditor(Account account,DataRowView? row)
    {
        int? id=row is null?null:Convert.ToInt32(row["Mã SP"]);
        var categories=CatalogService.CategoryOptions();
        if(categories.Rows.Count==0)throw new InvalidOperationException("Hãy tạo danh mục đang dùng trước khi thêm sản phẩm.");
        var ui=FormsUi.EditorLayout(this,id is null?"Thêm sản phẩm":"Sửa sản phẩm",10);
        var name=FormsUi.Field(Val("Tên sản phẩm"),150);
        var cat=FormsUi.Combo();cat.DataSource=categories;cat.DisplayMember="TenDM";cat.ValueMember="MaDM";
        if(id is not null)cat.SelectedValue=Convert.ToInt32(row!["Mã DM"]);
        var image=FormsUi.Field(Val("Hình ảnh"),255);
        var ing=FormsUi.Field(Val("Thành phần"),3000);
        var desc=FormsUi.Field(Val("Mô tả"),3000);
        var weight=FormsUi.Field(Val("Trọng lượng (g)"));
        var price=FormsUi.Field(Val("Giá bán"));
        var hsd=FormsUi.Field(Val("HSD (ngày)"));
        var min=FormsUi.Field(id is null?"0":Val("Ngưỡng tồn"));
        var status=FormsUi.Combo();status.Items.AddRange(new[]{"Đang bán","Ẩn","Ngừng bán"});
        status.SelectedItem=id is null?"Đang bán":Val("Trạng thái");
        var fields=new (string,Control)[]{("Tên sản phẩm *",name),("Danh mục *",cat),("Hình ảnh (đường dẫn)",image),
            ("Thành phần",ing),("Mô tả",desc),("Trọng lượng (g)",weight),("Giá bán *",price),("HSD (ngày)",hsd),
            ("Ngưỡng tồn *",min),("Trạng thái",status)};
        for(int i=0;i<fields.Length;i++)FormsUi.AddField(ui,i+1,fields[i].Item1,fields[i].Item2);
        var b=FormsUi.SaveButton(ui,11,()=>
        {
            try
            {
                if(cat.SelectedValue is null || !decimal.TryParse(price.Text,out decimal cost) ||
                    !int.TryParse(min.Text,out int low)) throw new ArgumentException("Danh mục, giá bán hoặc ngưỡng tồn không hợp lệ.");
                decimal? g=string.IsNullOrWhiteSpace(weight.Text)?null:
                    decimal.TryParse(weight.Text,out decimal w)?w:throw new ArgumentException("Trọng lượng phải là số.");
                int? days=string.IsNullOrWhiteSpace(hsd.Text)?null:
                    int.TryParse(hsd.Text,out int d)?d:throw new ArgumentException("HSD phải là số nguyên.");
                CatalogService.SaveProduct(account,new CatalogService.ProductInput(id,name.Text,Convert.ToInt32(cat.SelectedValue),
                    image.Text,ing.Text,desc.Text,g,cost,days,low,status.Text));
                DialogResult=DialogResult.OK;Close();
            }
            catch(Exception ex){FormsUi.Error(ex);}
        });
        AcceptButton=b;
        string Val(string key)=>row is null || row[key]==DBNull.Value?"":Convert.ToString(row[key])??"";
    }
}
