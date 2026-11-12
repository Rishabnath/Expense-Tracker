using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.Maui.ApplicationModel;

namespace ExpenseTracker.Ui;

public sealed class InsightsPage : ContentPage
{
    private readonly IExpenseStore _store = MauiProgram.Services.GetRequiredService<IExpenseStore>();
    private readonly VerticalStackLayout _categoryList = new() { Spacing = 14 };
    private readonly Label _total = SpendwiseTheme.Label("₹0", 30, Colors.White, true);
    private readonly Label _budgetInsight = SpendwiseTheme.Label("Set a budget to see your monthly progress.", 14, SpendwiseTheme.Ink);
    private readonly Label _empty = SpendwiseTheme.Label("Category insights will appear once you have expenses this month.", 13, SpendwiseTheme.Muted);

    public InsightsPage()
    {
        Title = "Insights";
        BackgroundColor = SpendwiseTheme.Paper;
        var header = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { SpendwiseTheme.Label("SPENDING SNAPSHOT", 11, SpendwiseTheme.Muted, true), SpendwiseTheme.Label("Insights", 28, SpendwiseTheme.Ink, true), SpendwiseTheme.Label(DateTime.Now.ToString("MMMM yyyy"), 14, SpendwiseTheme.Muted) }
        };
        var summary = new VerticalStackLayout { Spacing = 10 };
        summary.Add(SpendwiseTheme.Label("Total spent", 13, SpendwiseTheme.Mint));
        summary.Add(_total);
        summary.Add(_budgetInsight);
        var list = new VerticalStackLayout { Spacing = 16 };
        list.Add(SpendwiseTheme.Label("By category", 18, SpendwiseTheme.Ink, true));
        list.Add(_categoryList);
        var root = new VerticalStackLayout { Padding = new Thickness(22, 18, 22, 32), Spacing = 20 };
        root.Add(header);
        root.Add(SpendwiseTheme.Card(summary, SpendwiseTheme.Green, 22, 20));
        root.Add(SpendwiseTheme.Card(list, Colors.White, 20, 18));
        root.Add(_empty);
        Content = new ScrollView { Content = root };
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

    private void Refresh()
    {
        var month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var spent = _store.GetMonthSpend(month);
        var budget = _store.GetBudget();
        _total.Text = SpendwiseTheme.Money(spent);
        var remaining = budget - spent;
        var percent = budget > 0 ? (int)Math.Round((double)(spent / budget * 100m)) : 0;
        _budgetInsight.Text = budget <= 0 ? "Set a budget to see your monthly progress." : remaining >= 0
            ? $"You’ve used {percent}% of your budget, with {SpendwiseTheme.Money(remaining)} left."
            : $"You’re {SpendwiseTheme.Money(-remaining)} over your monthly budget.";

        _categoryList.Children.Clear();
        var categories = _store.GetCategoryTotals(month);
        foreach (var category in categories) _categoryList.Add(BuildCategory(category, spent));
        _empty.IsVisible = categories.Count == 0;
    }

    private static View BuildCategory(CategoryTotal category, decimal total)
    {
        var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        top.Add(SpendwiseTheme.Label(category.Name, 13, SpendwiseTheme.Ink, true), 0);
        top.Add(SpendwiseTheme.Label(SpendwiseTheme.Money(category.Amount), 13, SpendwiseTheme.Ink), 1);
        var progress = new ProgressBar { Progress = total > 0 ? (double)Math.Clamp(category.Amount / total, 0m, 1m) : 0, ProgressColor = SpendwiseTheme.Green, BackgroundColor = SpendwiseTheme.Line, HeightRequest = 6 };
        return new VerticalStackLayout { Spacing = 7, Children = { top, progress } };
    }
}
