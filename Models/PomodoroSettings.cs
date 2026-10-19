using CommunityToolkit.Mvvm.ComponentModel;

namespace SprintFocus.Models;

/// <summary>
/// Durações do ciclo Pomodoro, compartilhadas entre a tela de Timer e a de Configurações.
/// </summary>
public partial class PomodoroSettings : ObservableObject
{
    [ObservableProperty]
    public partial int FocusMinutes { get; set; } = 25;

    [ObservableProperty]
    public partial int ShortBreakMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial int LongBreakMinutes { get; set; } = 15;

    [ObservableProperty]
    public partial int CyclesBeforeLongBreak { get; set; } = 4;

    [ObservableProperty]
    public partial bool PlaySoundOnPhaseEnd { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowNotificationOnPhaseEnd { get; set; } = true;

    [ObservableProperty]
    public partial bool FlashOverlayOnPhaseEnd { get; set; } = true;

    /// <summary>Ao terminar uma fase, inicia a próxima automaticamente (foco → pausa → foco).</summary>
    [ObservableProperty]
    public partial bool AutoStartNextPhase { get; set; }
}
