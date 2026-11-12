using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using ExpenseTracker.Services;

namespace ExpenseTracker.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, Label = "Spendwise budget")]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData("android.appwidget.provider", Resource = "@xml/budget_widget_info")]
public sealed class BudgetWidgetProvider : AppWidgetProvider
{
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;
        foreach (var id in appWidgetIds) UpdateOne(context, appWidgetManager, id);
    }

    public static void Refresh(Context context)
    {
        var manager = AppWidgetManager.GetInstance(context);
        var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(BudgetWidgetProvider))!);
        var ids = manager.GetAppWidgetIds(component);
        if (ids?.Length > 0) new BudgetWidgetProvider().OnUpdate(context, manager, ids);
    }

    private static void UpdateOne(Context context, AppWidgetManager manager, int id)
    {
        var views = new RemoteViews(context.PackageName, Resource.Layout.budget_widget);
        var (budget, spent) = ExpenseDatabase.For(context).GetWidgetSummary(DateTime.Now);
        var remaining = budget - spent;
        views.SetTextViewText(Resource.Id.widget_remaining, budget > 0 ? $"₹{remaining:N0}" : "Set a budget");
        views.SetTextViewText(Resource.Id.widget_caption, budget <= 0 ? "open Spendwise to get started" : remaining >= 0 ? "left in your monthly budget" : "over your monthly budget");
        views.SetTextViewText(Resource.Id.widget_spent, budget > 0 ? $"₹{spent:N0} spent of ₹{budget:N0}" : "Your balance will show here");
        var open = new Intent(context, typeof(MainActivity));
        open.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        var pending = PendingIntent.GetActivity(context, 412, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        views.SetOnClickPendingIntent(Resource.Id.widget_root, pending);
        manager.UpdateAppWidget(id, views);
    }
}
