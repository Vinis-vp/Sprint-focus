using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SprintFocus.ViewModels;
using SprintFocus.Views;

namespace SprintFocus;

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
            var viewModel = new MainViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            // Exit dispara em qualquer encerramento "limpo" (fechar janela, logout...).
            desktop.Exit += (_, _) => viewModel.SaveOnExit();
        }

        base.OnFrameworkInitializationCompleted();
    }
}