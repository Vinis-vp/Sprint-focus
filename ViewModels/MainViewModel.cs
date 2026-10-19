using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using SprintFocus.Models;
using SprintFocus.Services;

namespace SprintFocus.ViewModels;

/// <summary>Item do menu lateral: um rótulo, um ícone e a página que ele abre.</summary>
public record NavItem(string Title, Symbol Icon, ViewModelBase Page);

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        var timeLog = new TimeLog();
        var settings = Pomodoro;
        var alerts = new PhaseAlerts(settings);

        Timer = new TimerViewModel(timeLog, settings);
        // Quando o evento dispara, o timer já avançou: Timer.Phase é a próxima fase.
        Timer.PhaseCompleted += (_, finished) =>
            alerts.PhaseCompleted(finished, Timer.Phase, Timer.SelectedCategory);

        Pages =
        [
            new NavItem("Timer", Symbol.Timer, Timer),
            new NavItem("Resumo", Symbol.DataPie, new SummaryViewModel(timeLog)),
            new NavItem("Configurações", Symbol.Settings, new SettingsViewModel(settings, Overlay, alerts)),
        ];

        SelectedPage = Pages[0];
    }

    public NavItem[] Pages { get; }

    /// <summary>Timer compartilhado entre a página principal e o overlay.</summary>
    public TimerViewModel Timer { get; }

    public OverlaySettings Overlay { get; } = new();

    public PomodoroSettings Pomodoro { get; } = new();

    [ObservableProperty]
    public partial NavItem SelectedPage { get; set; }
}
