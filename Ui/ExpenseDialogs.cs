using System.Globalization;
using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker.Ui;

internal static class ExpenseDialogs
{
    public static async Task AddAsync(Page page, IExpenseStore store)
    {
        var merchant = await page.DisplayPromptAsync("New expense", "Where did you spend?", "Save", "Cancel", "Merchant or note", maxLength: 60);
        if (string.IsNullOrWhiteSpace(merchant)) return;
        var rawAmount = await page.DisplayPromptAsync("Amount", "Enter the amount in ₹", "Continue", "Cancel", "0.00", keyboard: Keyboard.Numeric);
        if (!TryAmount(rawAmount, out var amount) || amount <= 0)
        {
            await page.DisplayAlert("Check the amount", "Enter an amount greater than zero.", "OK");
            return;
        }

        var suggested = ExpenseCategories.ForMerchant(merchant);
        var category = await page.DisplayActionSheet("Choose a category", "Cancel", null, ExpenseCategories.All);
        if (string.IsNullOrWhiteSpace(category) || category == "Cancel") category = suggested;
        store.Add(new ExpenseRecord { Amount = amount, Merchant = merchant.Trim(), Category = category, OccurredAt = DateTime.Now, Source = "Manual" });
    }

    public static async Task SetBudgetAsync(Page page, IExpenseStore store)
    {
        var current = store.GetBudget().ToString("0.##", CultureInfo.InvariantCulture);
        var raw = await page.DisplayPromptAsync("Monthly budget", "Set your monthly spending limit in ₹", "Save budget", "Cancel", current, keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(raw)) return;
        if (!TryAmount(raw, out var amount) || amount <= 0)
        {
            await page.DisplayAlert("Check the budget", "Enter a monthly budget greater than zero.", "OK");
            return;
        }
        store.SetBudget(amount);
    }

    public static async Task EditAsync(Page page, IExpenseStore store, ExpenseRecord record)
    {
        var merchant = await page.DisplayPromptAsync("Edit expense", "Merchant or note", "Continue", "Cancel", record.Merchant, maxLength: 60);
        if (string.IsNullOrWhiteSpace(merchant)) return;
        var raw = await page.DisplayPromptAsync("Edit amount", "Amount in ₹", "Continue", "Cancel", record.Amount.ToString("0.##", CultureInfo.InvariantCulture), keyboard: Keyboard.Numeric);
        if (!TryAmount(raw, out var amount) || amount <= 0) return;
        var category = await page.DisplayActionSheet("Choose a category", "Cancel", null, ExpenseCategories.All);
        if (string.IsNullOrWhiteSpace(category) || category == "Cancel") category = record.Category;
        record.Merchant = merchant.Trim();
        record.Amount = amount;
        record.Category = category;
        store.Update(record);
    }

    private static bool TryAmount(string? value, out decimal amount) =>
        decimal.TryParse(value?.Replace(",", string.Empty, StringComparison.Ordinal), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
}
