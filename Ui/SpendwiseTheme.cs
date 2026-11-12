using System.Globalization;
using ExpenseTracker.Models;
using Microsoft.Maui.Controls.Shapes;

namespace ExpenseTracker.Ui;

internal static class SpendwiseTheme
{
    public static readonly Color Paper = Color.FromArgb("#F5F5EE");
    public static readonly Color Ink = Color.FromArgb("#18251F");
    public static readonly Color Muted = Color.FromArgb("#78857D");
    public static readonly Color Green = Color.FromArgb("#173B31");
    public static readonly Color Mint = Color.FromArgb("#DCEFE2");
    public static readonly Color Lime = Color.FromArgb("#C3E181");
    public static readonly Color Line = Color.FromArgb("#E7E9E1");

    public static Label Label(string text, double size, Color color, bool bold = false) => new()
    {
        Text = text,
        FontSize = size,
        TextColor = color,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        LineBreakMode = LineBreakMode.WordWrap
    };

    public static Button Button(string text, Color background, Color foreground) => new()
    {
        Text = text,
        FontSize = 14,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 16,
        Padding = new Thickness(14, 14),
        BackgroundColor = background,
        TextColor = foreground,
        HeightRequest = 52
    };

    public static Border Card(View content, Color? background = null, double radius = 20, double padding = 16) => new()
    {
        StrokeThickness = 0,
        Stroke = Colors.Transparent,
        BackgroundColor = background ?? Colors.White,
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(radius) },
        Padding = padding,
        Content = content
    };

    public static string Money(decimal value) => $"₹{value.ToString("N0", CultureInfo.GetCultureInfo("en-IN"))}";

    public static View TransactionRow(ExpenseRecord item)
    {
        var glyph = item.Category switch
        {
            "Food & dining" => "✳",
            "Travel" => "↗",
            "Shopping" => "□",
            "Bills" => "⌁",
            "Groceries" => "◌",
            "Entertainment" => "▷",
            "Health" => "+",
            _ => "•"
        };
        var icon = new Border
        {
            WidthRequest = 42,
            HeightRequest = 42,
            BackgroundColor = Mint,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = new Label { Text = glyph, TextColor = Green, FontSize = 19, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
        };
        var details = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(item.Merchant, 14, Ink, true),
                Label($"{item.Category} · {item.OccurredAt: d MMM, h:mm tt}", 11, Muted)
            }
        };
        var amount = Label($"−{Money(item.Amount)}", 14, Ink, true);
        amount.HorizontalTextAlignment = TextAlignment.End;
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12, Padding = new Thickness(0, 11) };
        grid.Add(icon, 0);
        grid.Add(details, 1);
        grid.Add(amount, 2);
        return grid;
    }
}
