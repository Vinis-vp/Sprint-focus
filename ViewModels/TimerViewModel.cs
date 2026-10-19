using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SprintFocus.Models;
using SprintFocus.Services;

namespace SprintFocus.ViewModels;

public enum PomodoroPhase
{
    Focus,
    ShortBreak,
    LongBreak,
}

public partial class TimerViewModel : ViewModelBase
{
    private readonly TimeLog _timeLog;
    private readonly PomodoroSettings _settings;
    private readonly DispatcherTimer _ticker;
    private readonly Stopwatch _sinceLastTick = new();

    private int _completedFocusSessions;

    public TimerViewModel(TimeLog timeLog, PomodoroSettings settings)
    {
        _timeLog = timeLog;
        _settings = settings;
        _settings.PropertyChanged += OnSettingsChanged;

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _ticker.Tick += OnTick;

        Remaining = PhaseDuration;
    }

    public ActivityCategory[] Categories { get; } = Enum.GetValues<ActivityCategory>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CategoryBrush))]
    public partial ActivityCategory SelectedCategory { get; set; } = ActivityCategory.Trabalho;

    public IBrush CategoryBrush => new SolidColorBrush(SelectedCategory.GetColor());

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseName), nameof(CycleText), nameof(Progress))]
    public partial PomodoroPhase Phase { get; set; } = PomodoroPhase.Focus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingText), nameof(Progress))]
    public partial TimeSpan Remaining { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(PauseCommand))]
    [NotifyPropertyChangedFor(nameof(ToggleGlyph))]
    public partial bool IsRunning { get; set; }

    public string ToggleGlyph => IsRunning ? "❚❚" : "▶";

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
    public double Progress =>
        PhaseDuration.TotalSeconds <= 0 ? 0 : 100 * (1 - Remaining.TotalSeconds / PhaseDuration.TotalSeconds);

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
        Remaining = PhaseDuration;
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
            Pause();
            AdvancePhase();
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

        Remaining = PhaseDuration;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Só aplica a nova duração se o timer estiver parado, para não "pular" no meio de uma sessão.
        if (!IsRunning)
            Remaining = PhaseDuration;
        OnPropertyChanged(nameof(CycleText));
    }
}
