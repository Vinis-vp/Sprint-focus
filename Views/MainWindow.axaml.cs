using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using SprintFocus.Models;
using SprintFocus.ViewModels;

namespace SprintFocus.Views;

public partial class MainWindow : Window
{
    private OverlayWindow? _overlay;
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Voltou para o app principal: o mini-timer não é mais necessário.
        Activated += (_, _) => _overlay?.Hide();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.Overlay.PropertyChanged -= OnOverlaySettingsChanged;
            _viewModel.Timer.PhaseCompleted -= OnPhaseCompleted;
        }

        _viewModel = DataContext as MainViewModel;

        if (_viewModel is not null)
        {
            _viewModel.Overlay.PropertyChanged += OnOverlaySettingsChanged;
            _viewModel.Timer.PhaseCompleted += OnPhaseCompleted;
        }
    }

    private void OnPhaseCompleted(object? sender, PomodoroPhase finished)
    {
        if (_viewModel?.Pomodoro.FlashOverlayOnPhaseEnd == true)
            _overlay?.Flash();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Só reage à minimização. Esconder fica a cargo de ações explícitas
        // (foco na janela principal, botões do overlay), pois no X11 o WindowState
        // pode oscilar quando o foco muda para outros apps.
        if (change.Property == WindowStateProperty && WindowState == WindowState.Minimized)
            ShowOverlay();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Sem isso o overlay manteria o processo vivo depois de fechar a janela principal.
        _overlay?.Close();
        base.OnClosed(e);
    }

    private void OnOverlaySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null || _overlay is null)
            return;

        if (e.PropertyName == nameof(OverlaySettings.Enabled) && !_viewModel.Overlay.Enabled)
        {
            _overlay.Hide();
        }
        else if (e.PropertyName == nameof(OverlaySettings.Corner))
        {
            _overlay.ResetPosition();
            if (_overlay.IsVisible)
                ShowOverlay();
        }
    }

    private void ShowOverlay()
    {
        if (_viewModel is null || !_viewModel.Overlay.Enabled)
            return;

        if (_overlay is null)
        {
            _overlay = new OverlayWindow { DataContext = _viewModel.Timer };
            _overlay.OpenMainRequested += (_, _) => RestoreFromOverlay();
            _overlay.Closed += (_, _) => _overlay = null;
        }

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is not null)
            _overlay.ShowAt(screen, _viewModel.Overlay.Corner);
    }

    private void RestoreFromOverlay()
    {
        _overlay?.Hide();
        // Sem Activate(): no GNOME, pedir foco explicitamente faz o sistema exibir
        // a notificação "SprintFocus está pronto" em vez de focar a janela.
        WindowState = WindowState.Normal;
    }
}
