using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public interface IExpenseStore
{
    decimal GetBudget();
    void SetBudget(decimal amount);
    decimal GetMonthSpend(DateTime month);
    IReadOnlyList<ExpenseRecord> GetRecent(int limit = 100);
    IReadOnlyList<ExpenseRecord> GetMonthTransactions(DateTime month);
    IReadOnlyList<CategoryTotal> GetCategoryTotals(DateTime month);
    bool Add(ExpenseRecord record);
    void Update(ExpenseRecord record);
    void Delete(long id);
}

public static class ExpenseStoreEvents
{
    public static event EventHandler? Changed;

    public static void NotifyChanged() => Changed?.Invoke(null, EventArgs.Empty);
}
