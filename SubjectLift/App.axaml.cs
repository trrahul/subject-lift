using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SubjectLift.ViewModels;
using SubjectLift.Views;

namespace SubjectLift;

public class App : Application
{
    public static Window MainWindow { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            MainWindow = desktop.MainWindow;
        }

        
        base.OnFrameworkInitializationCompleted();
    }
}