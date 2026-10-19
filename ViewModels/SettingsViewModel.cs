using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SprintFocus.Models;
using SprintFocus.Services;

namespace SprintFocus.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly PhaseAlerts _alerts;

    public SettingsViewModel(PomodoroSettings pomodoro, OverlaySettings overlay, PhaseAlerts alerts)
    {
        Pomodoro = pomodoro;
        Overlay = overlay;
        _alerts = alerts;
    }

    [RelayCommand]
    private void TestAlerts() => _alerts.Test();

    public PomodoroSettings Pomodoro { get; }

    public OverlaySettings Overlay { get; }

    // Mesma ordem do enum OverlayCorner.
    public string[] CornerOptions { get; } =
        ["Superior esquerdo", "Superior direito", "Inferior esquerdo", "Inferior direito"];

    public int SelectedCornerIndex
    {
        get => (int)Overlay.Corner;
        set => Overlay.Corner = (OverlayCorner)value;
    }

    public string[] ThemeOptions { get; } = ["Sistema", "Claro", "Escuro"];

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = value switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
