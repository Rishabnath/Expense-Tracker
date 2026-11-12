using Android.App;
using Android.Content;
using Android.Provider;
using ExpenseTracker.Services;
using Microsoft.Maui.ApplicationModel;

namespace ExpenseTracker.Ui;

public sealed class SettingsPage : ContentPage
{
    private readonly IExpenseStore _store = MauiProgram.Services.GetRequiredService<IExpenseStore>();
    private readonly Label _budgetValue = SpendwiseTheme.Label("Not set", 24, SpendwiseTheme.Ink, true);

    public SettingsPage()
    {
        Title = "Settings";
        BackgroundColor = SpendwiseTheme.Paper;
        var root = new VerticalStackLayout { Padding = new Thickness(22, 18, 22, 30), Spacing = 18 };
        root.Add(SpendwiseTheme.Label("MAKE IT YOURS", 11, SpendwiseTheme.Muted, true));
        root.Add(SpendwiseTheme.Label("Settings", 28, SpendwiseTheme.Ink, true));

        var budgetStack = new VerticalStackLayout { Spacing = 7 };
        budgetStack.Add(SpendwiseTheme.Label("Monthly budget", 15, SpendwiseTheme.Ink, true));
        budgetStack.Add(_budgetValue);
        budgetStack.Add(SpendwiseTheme.Label("Your limit resets at the start of each month.", 12, SpendwiseTheme.Muted));
        var setBudget = SpendwiseTheme.Button("Change monthly budget", SpendwiseTheme.Mint, SpendwiseTheme.Green);
        setBudget.Clicked += async (_, _) => { await ExpenseDialogs.SetBudgetAsync(this, _store); Refresh(); };
        budgetStack.Add(setBudget);
        root.Add(SpendwiseTheme.Card(budgetStack));

        var captureStack = new VerticalStackLayout { Spacing = 10 };
        captureStack.Add(SpendwiseTheme.Label("Automatic capture", 15, SpendwiseTheme.Ink, true));
        captureStack.Add(SpendwiseTheme.Label("Allow Spendwise to read payment alerts in Android settings. Matching debit alerts are processed on this phone and saved to your local expense list.", 13, SpendwiseTheme.Muted));
        var access = SpendwiseTheme.Button("Open notification access", SpendwiseTheme.Green, Colors.White);
        access.Clicked += async (_, _) => await OpenListenerSettingsAsync(this);
        captureStack.Add(access);
        root.Add(SpendwiseTheme.Card(captureStack));

        var privacy = new VerticalStackLayout { Spacing = 8 };
        privacy.Add(SpendwiseTheme.Label("Private by design", 15, SpendwiseTheme.Ink, true));
        privacy.Add(SpendwiseTheme.Label("Your data stays in the app’s local SQLite database. This build has no network permission and does not upload notification text or transactions.", 13, SpendwiseTheme.Muted));
        root.Add(SpendwiseTheme.Card(privacy));

        var widget = new VerticalStackLayout { Spacing = 8 };
        widget.Add(SpendwiseTheme.Label("Home-screen widget", 15, SpendwiseTheme.Ink, true));
        widget.Add(SpendwiseTheme.Label("Long-press your Android home screen, open Widgets, choose Spendwise, then add the budget widget.", 13, SpendwiseTheme.Muted));
        root.Add(SpendwiseTheme.Card(widget));
        Content = new ScrollView { Content = root };
    }

    protected override void OnAppearing() { base.OnAppearing(); Refresh(); }

    private void Refresh() => _budgetValue.Text = _store.GetBudget() > 0 ? SpendwiseTheme.Money(_store.GetBudget()) : "Not set";

    public static async Task OpenListenerSettingsAsync(Page page)
    {
        try
        {
            var intent = new Intent(Settings.ActionNotificationListenerSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(intent);
        }
        catch (Exception)
        {
            await page.DisplayAlert("Settings unavailable", "Open Android Settings and find Notification access to enable Spendwise.", "OK");
        }
    }
}
