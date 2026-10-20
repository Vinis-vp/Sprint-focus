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
        var timeLog = _timeLog = new TimeLog(new SessionStore());
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

    private readonly TimeLog _timeLog;

    /// <summary>Grava o que ainda está só em memória (a sessão de foco aberta). Chamado ao encerrar o app.</summary>
    public void SaveOnExit() => _timeLog.Flush();

    public NavItem[] Pages { get; }

    /// <summary>Timer compartilhado entre a página principal e o overlay.</summary>
    public TimerViewModel Timer { get; }

    public OverlaySettings Overlay { get; } = new();

    public PomodoroSettings Pomodoro { get; } = new();

    [ObservableProperty]
    public partial NavItem SelectedPage { get; set; }
}
