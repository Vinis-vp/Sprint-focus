using CommunityToolkit.Mvvm.ComponentModel;
using SprintFocus.Models;
using SprintFocus.Services;

namespace SprintFocus.ViewModels;

/// <summary>Item do menu lateral: um rótulo, um ícone e a página que ele abre.</summary>
public record NavItem(string Title, string Glyph, ViewModelBase Page);

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        var timeLog = new TimeLog();
        var settings = new PomodoroSettings();

        Timer = new TimerViewModel(timeLog, settings);

        Pages =
        [
            new NavItem("Timer", "◷", Timer),
            new NavItem("Resumo", "▥", new SummaryViewModel(timeLog)),
            new NavItem("Configurações", "⚙", new SettingsViewModel(settings, Overlay)),
        ];

        SelectedPage = Pages[0];
    }

    public NavItem[] Pages { get; }

    /// <summary>Timer compartilhado entre a página principal e o overlay.</summary>
    public TimerViewModel Timer { get; }

    public OverlaySettings Overlay { get; } = new();

    [ObservableProperty]
    public partial NavItem SelectedPage { get; set; }
}
