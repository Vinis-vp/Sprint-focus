using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SprintFocus.Services;

/// <summary>
/// Mostra notificações nativas do sistema operacional usando a ferramenta de linha de comando de cada plataforma.
/// </summary>
public class SystemNotifier
{
    private readonly bool _hasNotifySend = OperatingSystem.IsLinux() && IsOnPath("notify-send");

    public void Show(string title, string message)
    {
        try
        {
            var start = CreateStartInfo(title, message);
            if (start is null)
                return;

            // Dispose só libera o handle; a notificação segue normalmente.
            Process.Start(start)?.Dispose();
        }
        catch (Exception ex)
        {
            // Notificação é um extra: se falhar, o timer continua funcionando normalmente.
            Trace.WriteLine($"[SystemNotifier] Falha ao notificar: {ex.Message}");
        }
    }

    private ProcessStartInfo? CreateStartInfo(string title, string message)
    {
        if (_hasNotifySend)
        {
            // ArgumentList passa cada argumento separado, sem precisar escapar aspas no texto.
            var info = new ProcessStartInfo("notify-send") { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("--app-name=SprintFocus");
            info.ArgumentList.Add("--urgency=normal");
            info.ArgumentList.Add(title);
            info.ArgumentList.Add(message);
            return info;
        }

        if (OperatingSystem.IsMacOS())
        {
            var info = new ProcessStartInfo("osascript") { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("-e");
            info.ArgumentList.Add($"display notification \"{Escape(message)}\" with title \"{Escape(title)}\"");
            return info;
        }

        // Windows: notificações "toast" exigem registro do app; fica para quando houver instalador.
        return null;
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static bool IsOnPath(string command) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => File.Exists(Path.Combine(dir, command)));
}
