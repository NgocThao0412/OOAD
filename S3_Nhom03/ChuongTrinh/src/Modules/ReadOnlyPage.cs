using OOAD.Bakery.Controls;
using OOAD.Bakery.Services;
namespace OOAD.Bakery.Modules;

public sealed class ReadOnlyPage:UserControl
{
    private readonly string query;
    private readonly DataGridView grid=Theme.Grid();
    private readonly Label details=Theme.Label("",9,false,Theme.Muted);
    public ReadOnlyPage(string name,string sql,string explanation)
    {
        query=sql;BackColor=Theme.Background;
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(14)};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,57));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(layout);
        layout.Controls.Add(FormsUi.Title(name),0,0);
        var refresh=Theme.Button("Tải dữ liệu từ SQL Server"); refresh.Width=240;
        refresh.Click+=(_,_)=>Reload();layout.Controls.Add(refresh,0,1);
        details.Text=explanation;details.AutoSize=false;details.Dock=DockStyle.Fill;
        layout.Controls.Add(details,0,2);
        layout.Controls.Add(grid,0,3);
        Reload();
    }
    private void Reload()
    {
        try{grid.DataSource=Db.Table(query);}
        catch(Exception ex){FormsUi.Error(ex);}
    }
}
