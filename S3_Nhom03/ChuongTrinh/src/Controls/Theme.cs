using System.Drawing;
using System.Windows.Forms;

namespace OOAD.Bakery.Controls;

public static class Theme
{
    public static readonly Color Background = ColorTranslator.FromHtml("#FBF8FC");
    public static readonly Color Sidebar = ColorTranslator.FromHtml("#F5E8F4");
    public static readonly Color Paper = Color.White;
    public static readonly Color Accent = ColorTranslator.FromHtml("#9564A4");
    public static readonly Color AccentDark = ColorTranslator.FromHtml("#704C7F");
    public static readonly Color Pink = ColorTranslator.FromHtml("#E8B7D0");
    public static readonly Color PinkSoft = ColorTranslator.FromHtml("#FFF0F5");
    public static readonly Color Lavender = ColorTranslator.FromHtml("#EFE5F6");
    public static readonly Color Border = ColorTranslator.FromHtml("#E7DAE8");
    public static readonly Color Text = ColorTranslator.FromHtml("#392C3D");
    public static readonly Color Muted = ColorTranslator.FromHtml("#786B7B");

    public static Label Label(string value, int size = 10, bool bold = false, Color? color = null)
        => new()
        {
            Text = value, AutoSize = true,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color ?? Text, BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 0)
        };

    public static Button Button(string value, bool primary = false)
    {
        var button = new Button
        {
            Text = value, Height = 38, Width = 116,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = primary ? Accent : Lavender,
            ForeColor = primary ? Color.White : AccentDark,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 0, 9, 0),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    public static TextBox TextInput(int width = 240)
        => new()
        {
            Width = width, Height = 32, BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10), BackColor = Color.White,
            ForeColor = Text
        };

    public static Panel Card()
        => new() { BackColor = Paper, Padding = new Padding(18), Margin = new Padding(0) };

    public static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            RowHeadersVisible = false, ColumnHeadersHeight = 39, RowTemplate = { Height = 35 },
            EnableHeadersVisualStyles = false, GridColor = Border,
            Font = new Font("Segoe UI", 10),
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Lavender, ForeColor = Text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = PinkSoft, SelectionForeColor = Text,
                BackColor = Color.White, ForeColor = Text
            }
        };
        return grid;
    }
}
