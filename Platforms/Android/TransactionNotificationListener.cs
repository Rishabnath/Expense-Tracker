using Android.App;
using Android.Content;
using Android.OS;
using Android.Service.Notification;
using ExpenseTracker.Services;

namespace ExpenseTracker.Platforms.Android;

[Service(Name = "com.srl.spendwise.TransactionNotificationListener", Label = "Spendwise transaction capture", Exported = false, Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE")]
[IntentFilter(["android.service.notification.NotificationListenerService"])]
public sealed class TransactionNotificationListener : NotificationListenerService
{
    public override void OnNotificationPosted(StatusBarNotification? statusBarNotification)
    {
        if (statusBarNotification?.Notification?.Extras is not Bundle extras) return;

        var title = extras.GetCharSequence(Notification.ExtraTitle)?.ToString() ?? string.Empty;
        var text = extras.GetCharSequence(Notification.ExtraBigText)?.ToString();
        if (string.IsNullOrWhiteSpace(text)) text = extras.GetCharSequence(Notification.ExtraText)?.ToString();
        if (string.IsNullOrWhiteSpace(text)) text = extras.GetCharSequence(Notification.ExtraSubText)?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (TransactionParser.TryParse(statusBarNotification.PackageName ?? string.Empty, title, text,
                DateTimeOffset.FromUnixTimeMilliseconds(statusBarNotification.PostTime).LocalDateTime, out var record) && record is not null)
        {
            ExpenseDatabase.For(this).Add(record);
            BudgetWidgetProvider.Refresh(this);
        }
    }
}
