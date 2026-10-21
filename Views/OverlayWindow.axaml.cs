using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using SprintFocus.Models;
using SprintFocus.Platform;

namespace SprintFocus.Views;

public partial class OverlayWindow : Window
{
    private const int ScreenMargin = 16;

    // Depois que o usuário arrasta a janela, passamos a lembrar onde ela ficou
    // em vez de usar o canto configurado.
    private PixelPoint? _customPosition;

    // Estado do arraste manual: posição da janela e do ponteiro (em pixels de tela) no início.
    private PixelPoint? _dragStartWindow;
    private PixelPoint _dragStartPointer;

    public OverlayWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => EnsureTopmost();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // O mini-timer nunca deve maximizar/ocupar a tela (ex.: atalho Super+↑ ou duplo clique).
        if (change.Property == WindowStateProperty && WindowState is WindowState.Maximized or WindowState.FullScreen)
            WindowState = WindowState.Normal;
    }

    /// <summary>Disparado pelo botão de abrir a janela principal.</summary>
    public event EventHandler? OpenMainRequested;

    /// <summary>Exibe o overlay no canto configurado (ou onde o usuário o deixou).</summary>
    public void ShowAt(Screen screen, OverlayCorner corner)
    {
        Position = _customPosition ?? GetCornerPosition(screen, corner);

        // Linux/X11 (GNOME): com ShowActivated=False o Avalonia grava _NET_WM_USER_TIME=0 antes de
        // mostrar a janela, e o Mutter transforma isso em "precisa de atenção" → aviso
        // "SprintFocus is ready". Saída: declarar a janela como "não aceito foco" (WM_HINTS) e
        // mostrar com ShowActivated=True — o zero não é gravado e, como ela não aceita foco,
        // também não rouba o foco do app em uso. Fora do X11 mantemos ShowActivated=False.
        ShowActivated = X11Interop.SetAcceptsFocus(this, false);

        // Ao esconder, o gerenciador de janelas apaga os estados da janela (_NET_WM_STATE) e o
        // Avalonia não os regrava: sem reaplicar, o mini-timer apareceria na barra de tarefas
        // a partir da segunda vez que é mostrado.
        ShowInTaskbar = true;
        ShowInTaskbar = false;

        Show();
        EnsureTopmost();
    }

    /// <summary>Pisca o overlay na cor da categoria para chamar atenção (ex.: fim de uma fase).</summary>
    public void Flash()
    {
        if (!IsVisible)
            return;

        // Remover e adicionar de novo reinicia a animação caso ainda esteja rodando.
        FlashLayer.Classes.Remove("flash");
        FlashLayer.Classes.Add("flash");
        DispatcherTimer.RunOnce(() => FlashLayer.Classes.Remove("flash"), TimeSpan.FromSeconds(2));
    }

    /// <summary>Esquece a posição arrastada, voltando a usar o canto configurado.</summary>
    public void ResetPosition() => _customPosition = null;

    // Arraste manual em vez de BeginMoveDrag: entregar o arraste ao gerenciador de janelas
    // ativa o "edge tiling" do GNOME (soltar no topo maximiza, nas laterais ocupa meia tela).
    // Movendo nós mesmos, o sistema só recebe novas posições e não aplica esses encaixes.
    private void OnDragAreaPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _dragStartWindow = Position;
        _dragStartPointer = this.PointToScreen(e.GetPosition(this));
        // Captura: continua recebendo PointerMoved mesmo se o mouse sair da janela durante o arraste.
        e.Pointer.Capture((IInputElement?)sender);
    }

    private void OnDragAreaMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStartWindow is not { } start)
            return;

        var pointer = this.PointToScreen(e.GetPosition(this));
        Position = new PixelPoint(
            start.X + pointer.X - _dragStartPointer.X,
            start.Y + pointer.Y - _dragStartPointer.Y);
    }

    private void OnDragAreaReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragStartWindow is null)
            return;

        EndDrag();
        e.Pointer.Capture(null);
    }

    private void OnDragCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndDrag();

    private void EndDrag()
    {
        // Só lembra a posição se a janela realmente saiu do lugar (um clique simples não conta).
        if (_dragStartWindow is { } start && start != Position)
            _customPosition = Position;
        _dragStartWindow = null;
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
