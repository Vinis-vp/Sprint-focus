using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using SprintFocus.Models;
using SprintFocus.Services;

namespace SprintFocus.ViewModels;

public partial class TimerViewModel : ViewModelBase
{
    private readonly TimeLog _timeLog;
    private readonly PomodoroSettings _settings;
    private readonly DispatcherTimer _ticker;
    private readonly Stopwatch _sinceLastTick = new();

    private static readonly TimeSpan ExtraTime = TimeSpan.FromSeconds(30);

    private int _completedFocusSessions;

    // Duração total da fase atual, incluindo tempo extra adicionado com "+30s".
    private TimeSpan _phaseTotal;

    public TimerViewModel(TimeLog timeLog, PomodoroSettings settings)
    {
        _timeLog = timeLog;
        _settings = settings;
        _settings.PropertyChanged += OnSettingsChanged;

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _ticker.Tick += OnTick;

        RestartPhaseTime();
    }

    /// <summary>
    /// Disparado quando uma fase termina naturalmente (o tempo zerou), com a fase que acabou.
    /// Não dispara ao pular manualmente.
    /// </summary>
    public event EventHandler<PomodoroPhase>? PhaseCompleted;

    public ActivityCategory[] Categories { get; } = Enum.GetValues<ActivityCategory>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CategoryBrush))]
    public partial ActivityCategory SelectedCategory { get; set; } = ActivityCategory.Trabalho;

    public IBrush CategoryBrush => new SolidColorBrush(SelectedCategory.GetColor());

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseName), nameof(CycleText), nameof(ProgressAngle))]
    [NotifyPropertyChangedFor(nameof(Mood), nameof(MascotEmoji), nameof(IsMascotFocused), nameof(IsMascotResting))]
    public partial PomodoroPhase Phase { get; set; } = PomodoroPhase.Focus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingText), nameof(ProgressAngle), nameof(IsFinalMinute))]
    public partial TimeSpan Remaining { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(PauseCommand))]
    [NotifyPropertyChangedFor(nameof(ToggleIcon), nameof(IsFinalMinute))]
    [NotifyPropertyChangedFor(nameof(Mood), nameof(MascotEmoji), nameof(IsMascotFocused), nameof(IsMascotResting))]
    public partial bool IsRunning { get; set; }

    public Symbol ToggleIcon => IsRunning ? Symbol.Pause : Symbol.Play;

    /// <summary>Humor do mascote: parado se o timer não roda; senão, focado ou descansando conforme a fase.</summary>
    public MascotMood Mood =>
        !IsRunning ? MascotMood.Idle
        : Phase == PomodoroPhase.Focus ? MascotMood.Focused
        : MascotMood.Resting;

    public string MascotEmoji => Mood.GetEmoji();

    // Atalhos booleanos para a View ligar classes de animação (Classes.bob/sway) ao humor.
    public bool IsMascotFocused => Mood == MascotMood.Focused;

    public bool IsMascotResting => Mood == MascotMood.Resting;

    /// <summary>Timer rodando no último minuto da fase (o relógio pulsa).</summary>
    public bool IsFinalMinute => IsRunning && Remaining > TimeSpan.Zero && Remaining <= TimeSpan.FromMinutes(1);

    public string RemainingText =>
        $"{(int)Remaining.TotalMinutes:00}:{Remaining.Seconds:00}";

    public string PhaseName => Phase switch
    {
        PomodoroPhase.Focus => "Foco",
        PomodoroPhase.ShortBreak => "Pausa curta",
        _ => "Pausa longa",
    };

    public string CycleText =>
        $"Ciclo {_completedFocusSessions % _settings.CyclesBeforeLongBreak + 1} de {_settings.CyclesBeforeLongBreak}";

    /// <summary>Percentual (0–100) já decorrido da fase atual.</summary>
    public double ProgressAngle =>
        _phaseTotal.TotalSeconds <= 0
            ? 0
            // 359.9 e não 360: com 360° início e fim do arco coincidem e nada é desenhado.
            : Math.Min(359.9, 360 * (1 - Remaining.TotalSeconds / _phaseTotal.TotalSeconds));

    private TimeSpan PhaseDuration => TimeSpan.FromMinutes(Phase switch
    {
        PomodoroPhase.Focus => _settings.FocusMinutes,
        PomodoroPhase.ShortBreak => _settings.ShortBreakMinutes,
        _ => _settings.LongBreakMinutes,
    });

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        IsRunning = true;
        _sinceLastTick.Restart();
        _ticker.Start();
    }

    private bool CanStart() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        RecordElapsed();
        _ticker.Stop();
        IsRunning = false;
    }

    private bool CanPause() => IsRunning;

    /// <summary>Alterna entre iniciar e pausar (usado pelo overlay).</summary>
    [RelayCommand]
    private void Toggle()
    {
        if (IsRunning)
            Pause();
        else
            Start();
    }

    [RelayCommand]
    private void Reset()
    {
        if (IsRunning)
            Pause();
        RestartPhaseTime();
    }

    /// <summary>Estende a fase atual em 30 segundos (funciona rodando ou pausado).</summary>
    [RelayCommand]
    private void AddTime()
    {
        _phaseTotal += ExtraTime;
        Remaining += ExtraTime;
    }

    [RelayCommand]
    private void Skip()
    {
        if (IsRunning)
            Pause();
        AdvancePhase();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var elapsed = RecordElapsed();
        Remaining = Remaining > elapsed ? Remaining - elapsed : TimeSpan.Zero;

        if (Remaining == TimeSpan.Zero)
        {
            var finished = Phase;
            Pause();
            AdvancePhase();
            PhaseCompleted?.Invoke(this, finished);

            if (_settings.AutoStartNextPhase)
                Start();
        }
    }

    /// <summary>
    /// Mede o tempo real desde o último tick e, se estiver em foco, soma na categoria atual.
    /// </summary>
    private TimeSpan RecordElapsed()
    {
        var elapsed = _sinceLastTick.Elapsed;
        _sinceLastTick.Restart();

        // Não contabiliza além do que restava na fase.
        if (elapsed > Remaining)
            elapsed = Remaining;

        if (Phase == PomodoroPhase.Focus && elapsed > TimeSpan.Zero)
            _timeLog.Add(SelectedCategory, elapsed);

        return elapsed;
    }

    /// <summary>Reinicia a contagem da fase atual com a duração configurada (descartando tempo extra).</summary>
    private void RestartPhaseTime()
    {
        _phaseTotal = PhaseDuration;
        Remaining = PhaseDuration;
        // Se Remaining já tinha esse valor, o setter não notifica; garante a barra atualizada.
        OnPropertyChanged(nameof(ProgressAngle));
    }

    private void AdvancePhase()
    {
        if (Phase == PomodoroPhase.Focus)
        {
            _completedFocusSessions++;
            Phase = _completedFocusSessions % _settings.CyclesBeforeLongBreak == 0
                ? PomodoroPhase.LongBreak
                : PomodoroPhase.ShortBreak;
        }
        else
        {
            Phase = PomodoroPhase.Focus;
        }

        RestartPhaseTime();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Só mudanças de duração/ciclo afetam o timer; avisos e afins não devem reiniciá-lo.
        if (e.PropertyName is not (nameof(PomodoroSettings.FocusMinutes)
                                or nameof(PomodoroSettings.ShortBreakMinutes)
                                or nameof(PomodoroSettings.LongBreakMinutes)
                                or nameof(PomodoroSettings.CyclesBeforeLongBreak)))
            return;

        // Só aplica a nova duração se o timer estiver parado, para não "pular" no meio de uma sessão.
        if (!IsRunning)
            RestartPhaseTime();
        OnPropertyChanged(nameof(CycleText));
    }
}
