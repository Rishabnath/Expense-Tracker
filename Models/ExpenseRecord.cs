namespace ExpenseTracker.Models;

public sealed class ExpenseRecord
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Source { get; set; } = "Manual";
    public string? NotificationKey { get; set; }
}

public sealed class CategoryTotal
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public static class ExpenseCategories
{
    public static readonly string[] All =
    [
        "Food & dining", "Travel", "Shopping", "Bills", "Groceries",
        "Entertainment", "Health", "Transfer", "Other"
    ];

    public static string ForMerchant(string merchant)
    {
        var value = merchant.ToLowerInvariant();
        if (Contains(value, "swiggy", "zomato", "restaurant", "cafe", "food", "domino", "mcdonald", "starbucks")) return "Food & dining";
        if (Contains(value, "uber", "ola", "rapido", "metro", "irctc", "railway", "bus", "petrol", "fuel")) return "Travel";
        if (Contains(value, "amazon", "flipkart", "myntra", "ajio", "mall", "store")) return "Shopping";
        if (Contains(value, "electricity", "water", "broadband", "airtel", "jio", "recharge", "bill", "gas")) return "Bills";
        if (Contains(value, "blinkit", "zepto", "bigbasket", "dmart", "grocery")) return "Groceries";
        if (Contains(value, "netflix", "spotify", "cinema", "bookmyshow", "hotstar", "prime video")) return "Entertainment";
        if (Contains(value, "pharmacy", "hospital", "clinic", "apollo", "medplus", "medical")) return "Health";
        return "Other";
    }

    private static bool Contains(string value, params string[] terms) => terms.Any(value.Contains);
}
