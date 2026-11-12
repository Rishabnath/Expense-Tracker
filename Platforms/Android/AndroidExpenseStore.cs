using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.Maui.ApplicationModel;

namespace ExpenseTracker.Platforms.Android;

public sealed class AndroidExpenseStore : IExpenseStore
{
    private readonly ExpenseDatabase _database;

    public AndroidExpenseStore()
    {
        _database = ExpenseDatabase.For(Platform.AppContext);
    }

    public decimal GetBudget() => _database.GetBudget();
    public void SetBudget(decimal amount)
    {
        _database.SetBudget(amount);
        BudgetWidgetProvider.Refresh(Platform.AppContext);
    }

    public decimal GetMonthSpend(DateTime month) => _database.GetMonthSpend(month);
    public IReadOnlyList<ExpenseRecord> GetRecent(int limit = 100) => _database.ReadRecords(limit);
    public IReadOnlyList<ExpenseRecord> GetMonthTransactions(DateTime month) => _database.ReadMonthRecords(month);
    public IReadOnlyList<CategoryTotal> GetCategoryTotals(DateTime month) => _database.ReadCategoryTotals(month);

    public bool Add(ExpenseRecord record)
    {
        var added = _database.Add(record);
        if (added) BudgetWidgetProvider.Refresh(Platform.AppContext);
        return added;
    }

    public void Update(ExpenseRecord record)
    {
        _database.Update(record);
        BudgetWidgetProvider.Refresh(Platform.AppContext);
    }

    public void Delete(long id)
    {
        _database.Delete(id);
        BudgetWidgetProvider.Refresh(Platform.AppContext);
    }
}

public sealed class ExpenseDatabase : SQLiteOpenHelper
{
    private const string DatabaseName = "spendwise.db";
    private const int Version = 1;
    private static readonly object Gate = new();
    private static ExpenseDatabase? _instance;

    private ExpenseDatabase(Context context) : base(context.ApplicationContext, DatabaseName, null, Version) { }

    public static ExpenseDatabase For(Context context)
    {
        lock (Gate) return _instance ??= new ExpenseDatabase(context);
    }

    public override void OnCreate(SQLiteDatabase db)
    {
        db.ExecSQL("CREATE TABLE Transactions (Id INTEGER PRIMARY KEY AUTOINCREMENT, Amount REAL NOT NULL, Merchant TEXT NOT NULL, Category TEXT NOT NULL, OccurredAt INTEGER NOT NULL, Source TEXT NOT NULL, NotificationKey TEXT UNIQUE)");
        db.ExecSQL("CREATE TABLE Settings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
        var initial = new ContentValues();
        initial.Put("Key", "monthly_budget");
        initial.Put("Value", "0");
        db.Insert("Settings", null, initial);
    }

    public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) { }

    public decimal GetBudget()
    {
        var db = ReadableDatabase;
        using var cursor = db.RawQuery("SELECT Value FROM Settings WHERE Key='monthly_budget' LIMIT 1", null);
        return cursor.MoveToFirst() && decimal.TryParse(cursor.GetString(0), out var value) ? value : 0m;
    }

    public void SetBudget(decimal amount)
    {
        var values = new ContentValues();
        values.Put("Key", "monthly_budget");
        values.Put("Value", amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        WritableDatabase.InsertWithOnConflict("Settings", null, values, Conflict.Replace);
    }

    public bool Add(ExpenseRecord record)
    {
        try
        {
            WritableDatabase.InsertOrThrow("Transactions", null, ToValues(record));
            return true;
        }
        catch (SQLiteException)
        {
            // A repeated notification is rejected by the unique NotificationKey.
            return false;
        }
    }

    public void Update(ExpenseRecord record) => WritableDatabase.Update("Transactions", ToValues(record), "Id=?", [record.Id.ToString()]);
    public void Delete(long id) => WritableDatabase.Delete("Transactions", "Id=?", [id.ToString()]);

    public decimal GetMonthSpend(DateTime month)
    {
        var (start, end) = MonthBounds(month);
        using var cursor = ReadableDatabase.RawQuery("SELECT COALESCE(SUM(Amount),0) FROM Transactions WHERE OccurredAt>=? AND OccurredAt<?", [start.ToString(), end.ToString()]);
        return cursor.MoveToFirst() ? (decimal)cursor.GetDouble(0) : 0m;
    }

    public IReadOnlyList<ExpenseRecord> ReadRecords(int limit)
    {
        using var cursor = ReadableDatabase.RawQuery("SELECT Id,Amount,Merchant,Category,OccurredAt,Source,NotificationKey FROM Transactions ORDER BY OccurredAt DESC LIMIT ?", [Math.Clamp(limit, 1, 500).ToString()]);
        return ReadCursor(cursor);
    }

    public IReadOnlyList<ExpenseRecord> ReadMonthRecords(DateTime month)
    {
        var (start, end) = MonthBounds(month);
        using var cursor = ReadableDatabase.RawQuery("SELECT Id,Amount,Merchant,Category,OccurredAt,Source,NotificationKey FROM Transactions WHERE OccurredAt>=? AND OccurredAt<? ORDER BY OccurredAt DESC", [start.ToString(), end.ToString()]);
        return ReadCursor(cursor);
    }

    public IReadOnlyList<CategoryTotal> ReadCategoryTotals(DateTime month)
    {
        var (start, end) = MonthBounds(month);
        using var cursor = ReadableDatabase.RawQuery("SELECT Category,COALESCE(SUM(Amount),0) FROM Transactions WHERE OccurredAt>=? AND OccurredAt<? GROUP BY Category ORDER BY SUM(Amount) DESC", [start.ToString(), end.ToString()]);
        var results = new List<CategoryTotal>();
        while (cursor.MoveToNext()) results.Add(new CategoryTotal { Name = cursor.GetString(0) ?? "Other", Amount = (decimal)cursor.GetDouble(1) });
        return results;
    }

    public (decimal Budget, decimal Spent) GetWidgetSummary(DateTime month) => (GetBudget(), GetMonthSpend(month));

    private static ContentValues ToValues(ExpenseRecord record)
    {
        var values = new ContentValues();
        values.Put("Amount", (double)record.Amount);
        values.Put("Merchant", record.Merchant);
        values.Put("Category", record.Category);
        values.Put("OccurredAt", new DateTimeOffset(record.OccurredAt).ToUnixTimeMilliseconds());
        values.Put("Source", record.Source);
        if (record.NotificationKey is null) values.PutNull("NotificationKey");
        else values.Put("NotificationKey", record.NotificationKey);
        return values;
    }

    private static IReadOnlyList<ExpenseRecord> ReadCursor(ICursor cursor)
    {
        var results = new List<ExpenseRecord>();
        while (cursor.MoveToNext())
        {
            var notificationKey = cursor.IsNull(6) ? null : cursor.GetString(6);
            results.Add(new ExpenseRecord
            {
                Id = cursor.GetLong(0),
                Amount = (decimal)cursor.GetDouble(1),
                Merchant = cursor.GetString(2) ?? "Unknown",
                Category = cursor.GetString(3) ?? "Other",
                OccurredAt = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(4)).LocalDateTime,
                Source = cursor.GetString(5) ?? "Manual",
                NotificationKey = notificationKey
            });
        }
        return results;
    }

    private static (long Start, long End) MonthBounds(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var end = start.AddMonths(1);
        return (new DateTimeOffset(start).ToUnixTimeMilliseconds(), new DateTimeOffset(end).ToUnixTimeMilliseconds());
    }
}
