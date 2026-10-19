using CommunityToolkit.Mvvm.ComponentModel;

namespace SprintFocus.Models;

public enum OverlayCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>
/// Configurações do mini-timer flutuante exibido quando a janela principal é minimizada.
/// </summary>
public partial class OverlaySettings : ObservableObject
{
    [ObservableProperty]
    public partial bool Enabled { get; set; } = true;

    [ObservableProperty]
    public partial OverlayCorner Corner { get; set; } = OverlayCorner.BottomRight;
}
