using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker.Ui;

public sealed class TransactionsPage : ContentPage
{
    private readonly IExpenseStore _store = MauiProgram.Services.GetRequiredService<IExpenseStore>();
    private readonly SearchBar _search = new() { Placeholder = "Search merchant or category", BackgroundColor = Colors.White, TextColor = SpendwiseTheme.Ink, PlaceholderColor = SpendwiseTheme.Muted };
    private readonly VerticalStackLayout _rows = new() { Spacing = 0 };
    private readonly Label _empty = SpendwiseTheme.Label("Your expenses will appear here as they’re captured or added.", 13, SpendwiseTheme.Muted);
    private IReadOnlyList<ExpenseRecord> _records = [];

    public TransactionsPage()
    {
        Title = "Activity";
        BackgroundColor = SpendwiseTheme.Paper;
        _search.TextChanged += (_, _) => RenderRows();
        var root = new VerticalStackLayout { Padding = new Thickness(22, 18, 22, 24), Spacing = 18 };
        root.Add(SpendwiseTheme.Label("ALL YOUR SPENDING", 11, SpendwiseTheme.Muted, true));
        root.Add(SpendwiseTheme.Label("Activity", 28, SpendwiseTheme.Ink, true));
        root.Add(_search);
        root.Add(SpendwiseTheme.Card(_rows, Colors.White, 20, 16));
        root.Add(_empty);
        var add = SpendwiseTheme.Button("＋  Add expense", SpendwiseTheme.Green, Colors.White);
        add.Clicked += async (_, _) => { await ExpenseDialogs.AddAsync(this, _store); Refresh(); };
        root.Add(add);
        Content = new ScrollView { Content = root };
    }

    protected override void OnAppearing() { base.OnAppearing(); Refresh(); }

    private void Refresh()
    {
        _records = _store.GetRecent(300);
        RenderRows();
    }

    private void RenderRows()
    {
        _rows.Children.Clear();
        var query = _search.Text?.Trim();
        var visible = string.IsNullOrEmpty(query)
            ? _records
            : _records.Where(x => x.Merchant.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Category.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var record in visible)
        {
            var row = SpendwiseTheme.TransactionRow(record);
            row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await ShowActionsAsync(record)) });
            _rows.Add(row);
            _rows.Add(new BoxView { HeightRequest = 1, Color = SpendwiseTheme.Line });
        }
        _empty.IsVisible = visible.Count == 0;
    }

    private async Task ShowActionsAsync(ExpenseRecord record)
    {
        var action = await DisplayActionSheet(record.Merchant, "Cancel", "Delete", "Edit");
        if (action == "Edit")
        {
            await ExpenseDialogs.EditAsync(this, _store, record);
            Refresh();
        }
        else if (action == "Delete")
        {
            if (await DisplayAlert("Delete expense?", $"Remove {SpendwiseTheme.Money(record.Amount)} at {record.Merchant}?", "Delete", "Keep"))
            {
                _store.Delete(record.Id);
                Refresh();
            }
        }
    }
}
