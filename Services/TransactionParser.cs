using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public static partial class TransactionParser
{
    [GeneratedRegex("(?:₹|Rs\\.?|INR)\\s*([0-9][0-9,]*(?:\\.[0-9]{1,2})?)|([0-9][0-9,]*(?:\\.[0-9]{1,2})?)\\s*(?:INR|rupees?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountRegex();

    [GeneratedRegex("\\b(?:to|at|for|on)\\s+([A-Za-z0-9&._' -]{2,48})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MerchantRegex();

    public static bool TryParse(string packageName, string title, string text, DateTime occurredAt, out ExpenseRecord? record)
    {
        record = null;
        var combined = $"{title} {text}".Trim();
        if (combined.Length == 0) return false;

        var lower = combined.ToLowerInvariant();
        if (!Regex.IsMatch(lower, @"\b(debited|debit|dr|paid|payment|purchase|spent|sent|txn|transaction)\b", RegexOptions.CultureInvariant)) return false;
        if (Regex.IsMatch(lower, @"\b(credited|received|refund|cashback|reversed|failed|declined)\b", RegexOptions.CultureInvariant)) return false;

        var match = AmountRegex().Match(combined);
        if (!match.Success) return false;
        var rawAmount = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Replace(",", string.Empty, StringComparison.Ordinal);
        if (!decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0 || amount > 10_000_000m) return false;

        var merchantMatch = MerchantRegex().Match(combined);
        var merchant = merchantMatch.Success ? CleanMerchant(merchantMatch.Groups[1].Value) : FriendlySource(packageName);
        if (merchant.Length == 0) merchant = FriendlySource(packageName);

        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{packageName}|{title}|{text}")));
        record = new ExpenseRecord
        {
            Amount = amount,
            Merchant = merchant,
            Category = ExpenseCategories.ForMerchant(merchant),
            OccurredAt = occurredAt,
            Source = "Notification",
            NotificationKey = digest
        };
        return true;
    }

    private static string CleanMerchant(string value)
    {
        var end = Regex.Match(value, @"\s+(?:via|using|through|upi|from|ref|txn|transaction)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (end.Success) value = value[..end.Index];
        value = Regex.Replace(value, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim(' ', '.', ',', ':', ';', '-', '_');
        return value.Length > 34 ? value[..34].TrimEnd() : value;
    }

    private static string FriendlySource(string packageName)
    {
        if (packageName.Contains("phonepe", StringComparison.OrdinalIgnoreCase)) return "PhonePe";
        if (packageName.Contains("paytm", StringComparison.OrdinalIgnoreCase)) return "Paytm";
        if (packageName.Contains("paisa", StringComparison.OrdinalIgnoreCase) || packageName.Contains("google.android.apps.nbu", StringComparison.OrdinalIgnoreCase)) return "Google Pay";
        return "Bank payment";
    }
}
