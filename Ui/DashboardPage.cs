using ExpenseTracker.Services;
using Microsoft.Maui.ApplicationModel;

namespace ExpenseTracker.Ui;

public sealed class DashboardPage : ContentPage
{
    private readonly IExpenseStore _store = MauiProgram.Services.GetRequiredService<IExpenseStore>();
    private readonly Label _spent = SpendwiseTheme.Label("₹0", 36, Colors.White, true);
    private readonly Label _left = SpendwiseTheme.Label("Set a budget to see your balance", 14, SpendwiseTheme.Mint);
    private readonly Label _budgetLabel = SpendwiseTheme.Label("Budget not set", 12, Colors.White);
    private readonly ProgressBar _progress = new() { ProgressColor = SpendwiseTheme.Lime, BackgroundColor = Color.FromArgb("#466358"), HeightRequest = 7 };
    private readonly VerticalStackLayout _recent = new() { Spacing = 0 };
    private readonly Label _empty = SpendwiseTheme.Label("No expenses yet. Add one, or switch on automatic capture.", 13, SpendwiseTheme.Muted);

    public DashboardPage()
    {
        Title = "Home";
        BackgroundColor = SpendwiseTheme.Paper;
        Content = new ScrollView { Content = BuildContent() };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ExpenseStoreEvents.Changed -= OnExpenseStoreChanged;
        ExpenseStoreEvents.Changed += OnExpenseStoreChanged;
        Refresh();
    }

    protected override void OnDisappearing()
    {
        ExpenseStoreEvents.Changed -= OnExpenseStoreChanged;
        base.OnDisappearing();
    }

    private void OnExpenseStoreChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    private View BuildContent()
    {
        var root = new VerticalStackLayout { Padding = new Thickness(22, 18, 22, 30), Spacing = 20 };
        root.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                SpendwiseTheme.Label("YOUR MONEY, IN FOCUS", 11, SpendwiseTheme.Muted, true),
                SpendwiseTheme.Label("Good morning", 28, SpendwiseTheme.Ink, true),
                SpendwiseTheme.Label(DateTime.Now.ToString("dddd, d MMMM"), 14, SpendwiseTheme.Muted)
            }
        });
        root.Add(BuildBudgetCard());

        var actions = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 };
        var add = SpendwiseTheme.Button("＋  Add expense", SpendwiseTheme.Green, Colors.White);
        add.Clicked += async (_, _) => { await ExpenseDialogs.AddAsync(this, _store); Refresh(); };
        var budget = SpendwiseTheme.Button("Set budget", SpendwiseTheme.Mint, SpendwiseTheme.Green);
        budget.Clicked += async (_, _) => { await ExpenseDialogs.SetBudgetAsync(this, _store); Refresh(); };
        actions.Add(add, 0); actions.Add(budget, 1); root.Add(actions);

        var capture = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12, Padding = 15 };
        capture.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { SpendwiseTheme.Label("Automatic capture", 15, SpendwiseTheme.Ink, true), SpendwiseTheme.Label("Pick up eligible payment alerts", 12, SpendwiseTheme.Muted) }
        }, 0);
        var setup = new Button { Text = "Set up", FontSize = 12, CornerRadius = 14, Padding = new Thickness(12, 6), BackgroundColor = SpendwiseTheme.Paper, TextColor = SpendwiseTheme.Green, FontAttributes = FontAttributes.Bold };
        setup.Clicked += async (_, _) => await SettingsPage.OpenListenerSettingsAsync(this);
        capture.Add(setup, 1);
        root.Add(SpendwiseTheme.Card(capture, Colors.White, 20));

        var heading = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        heading.Add(SpendwiseTheme.Label("Recent activity", 19, SpendwiseTheme.Ink, true), 0);
        var all = new Label { Text = "See all  ›", FontSize = 13, TextColor = SpendwiseTheme.Green, VerticalTextAlignment = TextAlignment.Center };
        all.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Shell.Current.GoToAsync("//transactions")) });
        heading.Add(all, 1); root.Add(heading);
        root.Add(_recent); root.Add(_empty);
        return root;
    }

    private View BuildBudgetCard()
    {
        var stack = new VerticalStackLayout { Spacing = 14 };
        var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        top.Add(SpendwiseTheme.Label(DateTime.Now.ToString("MMMM yyyy").ToUpperInvariant(), 11, SpendwiseTheme.Mint, true), 0);
        top.Add(SpendwiseTheme.Label("MONTHLY BUDGET", 10, SpendwiseTheme.Mint, true), 1);
        stack.Add(top); stack.Add(SpendwiseTheme.Label("Spent this month", 13, Color.FromArgb("#D0DED6"))); stack.Add(_spent); stack.Add(_progress);
        var bottom = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        bottom.Add(_left, 0); bottom.Add(_budgetLabel, 1); stack.Add(bottom);
        return SpendwiseTheme.Card(stack, SpendwiseTheme.Green, 24, 20);
    }

    private void Refresh()
    {
        var month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var budget = _store.GetBudget();
        var spent = _store.GetMonthSpend(month);
        var remaining = budget - spent;
        _spent.Text = SpendwiseTheme.Money(spent);
        _left.Text = budget <= 0 ? "Set a budget to see your balance" : remaining >= 0 ? $"{SpendwiseTheme.Money(remaining)} left to spend" : $"{SpendwiseTheme.Money(-remaining)} over budget";
        _budgetLabel.Text = budget > 0 ? $"Budget {SpendwiseTheme.Money(budget)}" : "Budget not set";
        _progress.Progress = budget > 0 ? (double)Math.Clamp(spent / budget, 0m, 1m) : 0;
        _recent.Children.Clear();
        var records = _store.GetRecent(5);
        foreach (var record in records) _recent.Add(SpendwiseTheme.TransactionRow(record));
        _empty.IsVisible = records.Count == 0;
    }
}
