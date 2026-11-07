
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RandomVideoDistributor.Services;
using RandomVideoDistributor.ViewModels;
using RandomVideoDistributor.Views;

namespace RandomVideoDistributor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashViewModel = new SplashViewModel();

            var splashWindow = new SplashWindow
            {
                DataContext = splashViewModel,
                Opacity = 1
            };

            desktop.MainWindow = splashWindow;

            splashWindow.Opened += async (_, _) =>
            {
                try
                {
                    // المرحلة الأولى: تهيئة خدمة الإعدادات.
                    splashViewModel.StatusMessage =
                        "جارٍ تهيئة خدمة الإعدادات...";

                    splashViewModel.Progress = 10;
#if DEBUG
                    await Task.Delay(1000);
#endif

                    var settingsService = await Task.Run(
                        () => new VideoExtensionSettingsService());

                    splashViewModel.Progress = 35;

                    // المرحلة الثانية: تحميل إعدادات الفيديو.
                    splashViewModel.StatusMessage =
                        "جارٍ تحميل إعدادات الفيديو...";

                    var extensionSettings = await Task.Run(
                        () => settingsService.Load());

                    splashViewModel.Progress = 75;
#if DEBUG
                    await Task.Delay(1000);
#endif

                    // المرحلة الثالثة: تجهيز النافذة الرئيسية.
                    splashViewModel.StatusMessage =
                        "جارٍ تجهيز واجهة البرنامج...";

                    var mainViewModel =
                        new MainViewModel(extensionSettings);

                    var mainWindow = new MainWindow
                    {
                        DataContext = mainViewModel,
                        Opacity = 0
                    };

                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();

                    // اكتملت جميع مراحل التهيئة.
                    splashViewModel.Progress = 100;

#if DEBUG
                    await Task.Delay(1000);
#endif

                    splashViewModel.StatusMessage =
                        "اكتمل تجهيز البرنامج";

                    // الانتقال التدريجي إلى النافذة الرئيسية.
                    await FadeTransitionAsync(
                        splashWindow,
                        mainWindow);

                    splashWindow.Close();
                }
                catch (Exception ex)
                {
                    splashViewModel.StatusMessage =
                        $"تعذر تجهيز البرنامج: {ex.Message}";
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task FadeTransitionAsync(
        Window splashWindow,
        Window mainWindow)
    {
        const double transitionDuration = 0.7;

        var stopwatch = Stopwatch.StartNew();

        var completion = new TaskCompletionSource<bool>();

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        timer.Tick += (_, _) =>
        {
            double progress = Math.Clamp(
                stopwatch.Elapsed.TotalSeconds / transitionDuration,
                0.0,
                1.0);

            splashWindow.Opacity = 1.0 - progress;
            mainWindow.Opacity = progress;

            if (progress >= 1.0)
            {
                timer.Stop();
                stopwatch.Stop();

                completion.TrySetResult(true);
            }
        };

        timer.Start();

        await completion.Task;
    }
}
