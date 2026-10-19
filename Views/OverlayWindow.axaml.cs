using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using SprintFocus.Models;

namespace SprintFocus.Views;

public partial class OverlayWindow : Window
{
    private const int ScreenMargin = 16;

    // Depois que o usuário arrasta a janela, passamos a lembrar onde ela ficou
    // em vez de usar o canto configurado.
    private bool _userMoved;
    private PixelPoint? _customPosition;

    public OverlayWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => EnsureTopmost();

        // No X11 o arraste é assíncrono: a posição final chega por este evento,
        // não logo após BeginMoveDrag retornar.
        PositionChanged += (_, e) =>
        {
            if (_userMoved && IsVisible)
                _customPosition = e.Point;
        };
    }

    /// <summary>Disparado pelo botão de abrir a janela principal.</summary>
    public event EventHandler? OpenMainRequested;

    /// <summary>Exibe o overlay no canto configurado (ou onde o usuário o deixou).</summary>
    public void ShowAt(Screen screen, OverlayCorner corner)
    {
        Position = _customPosition ?? GetCornerPosition(screen, corner);
        Show();
        EnsureTopmost();
    }

    /// <summary>Esquece a posição arrastada, voltando a usar o canto configurado.</summary>
    public void ResetPosition()
    {
        _userMoved = false;
        _customPosition = null;
    }

    private void OnDragAreaPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _userMoved = true;
        BeginMoveDrag(e);
    }

    private void OnResizeGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // Impede que o Border pai interprete o clique como início de arraste.
        e.Handled = true;
        BeginResizeDrag(WindowEdge.SouthEast, e);
    }

    private void OnOpenMainClick(object? sender, RoutedEventArgs e) =>
        OpenMainRequested?.Invoke(this, EventArgs.Empty);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Hide();

    /// <summary>
    /// Alguns gerenciadores de janela (ex.: GNOME/Mutter via XWayland) ignoram o "sempre por cima"
    /// definido antes da janela ser mapeada. Reaplicar depois que ela já está visível resolve.
    /// </summary>
    private void EnsureTopmost()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsVisible)
                return;
            Topmost = false;
            Topmost = true;
        }, DispatcherPriority.Background);
    }

    private PixelPoint GetCornerPosition(Screen screen, OverlayCorner corner)
    {
        // WorkingArea e Position são em pixels físicos; ClientSize em unidades lógicas.
        // ClientSize (e não Width/Height) reflete o tamanho após o usuário redimensionar.
        var area = screen.WorkingArea;
        var width = (int)Math.Ceiling(ClientSize.Width * screen.Scaling);
        var height = (int)Math.Ceiling(ClientSize.Height * screen.Scaling);
        var margin = (int)(ScreenMargin * screen.Scaling);

        var left = corner is OverlayCorner.TopLeft or OverlayCorner.BottomLeft;
        var top = corner is OverlayCorner.TopLeft or OverlayCorner.TopRight;

        var x = left ? area.X + margin : area.Right - width - margin;
        var y = top ? area.Y + margin : area.Bottom - height - margin;

        return new PixelPoint(x, y);
    }
}
