using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace SprintFocus.Platform;

/// <summary>
/// Chamadas diretas ao X11 (Linux, incluindo XWayland) para recursos que o Avalonia não expõe.
/// Existe para contornar a proteção contra roubo de foco do GNOME/Mutter, que mostra o aviso
/// "SprintFocus is ready" em vez de obedecer pedidos de foco feitos "pelo app".
/// Em outras plataformas todos os métodos retornam false e não fazem nada.
/// </summary>
internal static class X11Interop
{
    private const string LibX11 = "libX11.so.6";

    private const int ClientMessage = 33;
    private const long SubstructureNotifyMask = 1L << 19;
    private const long SubstructureRedirectMask = 1L << 20;
    private const long InputHint = 1;

    // Origem de um _NET_ACTIVE_WINDOW (EWMH): 1 = aplicativo, 2 = pager (ação do usuário).
    private const long SourcePager = 2;

    /// <summary>
    /// Diz ao gerenciador de janelas se a janela aceita foco do teclado (WM_HINTS.input).
    /// Uma janela que não aceita foco nunca é tratada como "roubando foco" ao aparecer.
    /// </summary>
    public static bool SetAcceptsFocus(TopLevel window, bool accepts) => WithDisplay(window, (display, xid) =>
    {
        var hints = new XWMHints { flags = (IntPtr)InputHint, input = accepts ? 1 : 0 };
        XSetWMHints(display, xid, ref hints);
    });

    /// <summary>
    /// Pede ao gerenciador de janelas para ativar (e restaurar, se minimizada) a janela,
    /// identificando o pedido como ação do usuário (origem "pager"), que o Mutter não bloqueia.
    /// </summary>
    public static bool RequestActivation(TopLevel window) => WithDisplay(window, (display, xid) =>
    {
        var ev = new XClientMessageEvent
        {
            type = ClientMessage,
            send_event = 1,
            display = display,
            window = xid,
            message_type = XInternAtom(display, "_NET_ACTIVE_WINDOW", false),
            format = 32,
            l0 = (IntPtr)SourcePager,
            l1 = IntPtr.Zero, // timestamp 0 = "agora"
        };
        XSendEvent(display, XDefaultRootWindow(display), false,
            (IntPtr)(SubstructureRedirectMask | SubstructureNotifyMask), ref ev);
    });

    /// <summary>Abre uma conexão própria com o X, executa a ação na janela e fecha.</summary>
    private static bool WithDisplay(TopLevel window, Action<IntPtr, IntPtr> action)
    {
        if (!OperatingSystem.IsLinux() || window.TryGetPlatformHandle() is not { HandleDescriptor: "XID" } handle)
            return false;

        try
        {
            var display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
                return false;
            try
            {
                action(display, handle.Handle);
                XFlush(display);
                return true;
            }
            finally
            {
                XCloseDisplay(display);
            }
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    // Layout de XWMHints em 64 bits (long/XID = 8 bytes, int = 4).
    [StructLayout(LayoutKind.Sequential)]
    private struct XWMHints
    {
        public IntPtr flags;
        public int input;
        public int initial_state;
        public IntPtr icon_pixmap;
        public IntPtr icon_window;
        public int icon_x;
        public int icon_y;
        public IntPtr icon_mask;
        public IntPtr window_group;
    }

    // XClientMessageEvent dentro da union XEvent (192 bytes em 64 bits).
    [StructLayout(LayoutKind.Explicit, Size = 192)]
    private struct XClientMessageEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(8)] public IntPtr serial;
        [FieldOffset(16)] public int send_event;
        [FieldOffset(24)] public IntPtr display;
        [FieldOffset(32)] public IntPtr window;
        [FieldOffset(40)] public IntPtr message_type;
        [FieldOffset(48)] public int format;
        [FieldOffset(56)] public IntPtr l0;
        [FieldOffset(64)] public IntPtr l1;
        [FieldOffset(72)] public IntPtr l2;
        [FieldOffset(80)] public IntPtr l3;
        [FieldOffset(88)] public IntPtr l4;
    }

    [DllImport(LibX11)] private static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport(LibX11)] private static extern int XCloseDisplay(IntPtr display);
    [DllImport(LibX11)] private static extern int XFlush(IntPtr display);
    [DllImport(LibX11)] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport(LibX11)] private static extern IntPtr XInternAtom(IntPtr display, string name, bool onlyIfExists);
    [DllImport(LibX11)] private static extern int XSetWMHints(IntPtr display, IntPtr window, ref XWMHints hints);
    [DllImport(LibX11)] private static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr eventMask, ref XClientMessageEvent ev);
}
