# Spendwise

Spendwise is an Android-first expense tracker built with .NET MAUI. The first version is designed to work offline and keep transaction data on the device.

## First-version features

- Monthly budget and remaining-balance dashboard
- Local SQLite transaction history with search, edit, and delete
- Android notification listener that recognizes debit alerts and saves matching transactions locally
- Merchant-based category suggestions and monthly category insights
- Android home-screen widget for the remaining monthly budget
- Manual expense entry when a notification is missed or parsed incorrectly

## Privacy and scope

The Android manifest does not request internet access. Notification text is processed on device; matching debit alerts are stored as transactions, and the original notification text is not retained. Notification access is optional and must be enabled by the user in Android settings. The parser uses common message patterns, so review automatic entries and correct them when a bank or payment app uses an unfamiliar format.

The first version has no account, server, cloud sync, PostgreSQL dependency, Firebase service, or multi-device support. The transaction model and store interface are separated so a future API-backed store can be added later.

## Build an Android debug APK

Install the .NET MAUI Android workload, Android SDK, and a supported JDK, then run:

```powershell
dotnet build -f net11.0-android -c Debug -p:AndroidPackageFormats=apk
```

The build output is under `bin/Debug/net11.0-android/`. A debug APK is intended for local installation and development, not store distribution.

## Use notification capture and the widget

1. Install and open Spendwise.
2. Set a monthly budget in **Settings**.
3. Open **Notification access** and enable Spendwise in Android settings.
4. Long-press the Android home screen, choose **Widgets**, then add the Spendwise budget widget.

You can add, edit, categorize, and delete transactions in the app at any time.
