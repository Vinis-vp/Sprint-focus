using SprintFocus.Models;

namespace SprintFocus.Services;

/// <summary>
/// Decide quais avisos disparar quando uma fase do Pomodoro termina (som e notificação do sistema),
/// conforme as preferências do usuário. Avisos visuais da interface ficam a cargo das Views.
/// </summary>
public class PhaseAlerts
{
    private readonly PomodoroSettings _settings;
    private readonly SoundPlayer _sound = new();
    private readonly SystemNotifier _notifier = new();

    public PhaseAlerts(PomodoroSettings settings)
    {
        _settings = settings;
    }

    /// <param name="finished">Fase que acabou de terminar.</param>
    /// <param name="next">Fase que vem a seguir.</param>
    /// <param name="category">Categoria selecionada no timer.</param>
    public void PhaseCompleted(PomodoroPhase finished, PomodoroPhase next, ActivityCategory category)
    {
        if (_settings.PlaySoundOnPhaseEnd)
            _sound.Play(finished == PomodoroPhase.Focus ? SoundCue.FocusEnded : SoundCue.BreakEnded);

        if (_settings.ShowNotificationOnPhaseEnd)
        {
            var (title, message) = BuildMessage(finished, next, category);
            _notifier.Show(title, message);
        }
    }

    /// <summary>Dispara os avisos habilitados como se um foco tivesse acabado de terminar.</summary>
    public void Test() => PhaseCompleted(PomodoroPhase.Focus, PomodoroPhase.ShortBreak, ActivityCategory.Trabalho);

    private (string Title, string Message) BuildMessage(
        PomodoroPhase finished, PomodoroPhase next, ActivityCategory category)
    {
        var autoStart = _settings.AutoStartNextPhase ? " Já começou." : "";

        if (finished == PomodoroPhase.Focus)
        {
            var (kind, minutes) = next == PomodoroPhase.LongBreak
                ? ("longa", _settings.LongBreakMinutes)
                : ("curta", _settings.ShortBreakMinutes);
            return ("Foco concluído", $"Hora da pausa {kind} ({minutes} min).{autoStart}");
        }

        return ("Pausa concluída", $"Hora de voltar ao foco: {_settings.FocusMinutes} min de {category}.{autoStart}");
    }
}
