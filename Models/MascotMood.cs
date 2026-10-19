namespace SprintFocus.Models;

/// <summary>Estado de humor do mascote, derivado do timer.</summary>
public enum MascotMood
{
    /// <summary>Timer parado/pausado.</summary>
    Idle,

    /// <summary>Sessão de foco em andamento.</summary>
    Focused,

    /// <summary>Pausa (curta ou longa) em andamento.</summary>
    Resting,
}

public static class MascotMoodExtensions
{
    // Provisório: emojis até o mascote ganhar arte e animações próprias.
    public static string GetEmoji(this MascotMood mood) => mood switch
    {
        MascotMood.Focused => "🔥",
        MascotMood.Resting => "☕",
        _ => "😴",
    };
}
