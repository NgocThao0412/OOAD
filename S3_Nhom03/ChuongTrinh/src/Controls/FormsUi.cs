namespace OOAD.Bakery.Controls;
public static class FormsUi
{
    public static Label Title(string title)=>Theme.Label(title,20,true,Theme.AccentDark);
    public static TextBox Field(string value="",int maxLength=255)
    {
        var tb=Theme.TextInput();tb.Text=value;tb.MaxLength=maxLength;tb.Dock=DockStyle.Fill;
        return tb;
    }
    public static ComboBox Combo()
      => new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,Font=new Font("Segoe UI",10),BackColor=Color.White};
    public static TableLayoutPanel EditorLayout(Form form,string heading,int rows)
    {
        form.Text=heading;form.StartPosition=FormStartPosition.CenterParent;
        form.Size=new Size(590,Math.Min(860,145+rows*61));
        form.MinimumSize=new Size(520,Math.Min(700,145+rows*55));
        form.BackColor=Theme.Background;
        form.AutoScroll=true;
        var table=new TableLayoutPanel { Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(22),ColumnCount=2,RowCount=rows+2 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        table.Controls.Add(Title(heading),0,0);
        table.SetColumnSpan(table.GetControlFromPosition(0,0)!,2);
        table.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        form.Controls.Add(table);return table;
    }
    public static void AddField(TableLayoutPanel table,int row,string label,Control field)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute,53));
        table.Controls.Add(Theme.Label(label),0,row);
        table.Controls.Add(field,1,row);
        field.Margin=new Padding(0,3,2,11);
    }
    public static Button SaveButton(TableLayoutPanel table,int row,Action action)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute,50));
        var b=Theme.Button("Lưu thay đổi",true);b.Width=180;
        b.Click+=(_,_)=>action();table.Controls.Add(b,1,row);return b;
    }
    public static void Error(Exception ex)
    {
        MessageBox.Show(ex.Message,"Không thể thực hiện thao tác",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    }
}
