using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ExpenseTracker.Services;
using ExpenseTracker.Platforms.Android;
using ExpenseTracker.Ui;

namespace ExpenseTracker;

public static class MauiProgram
{
	public static IServiceProvider Services { get; private set; } = default!;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.Services.AddSingleton<IExpenseStore, AndroidExpenseStore>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		Services = app.Services;
		return app;
	}
}
