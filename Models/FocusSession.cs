using System;

namespace SprintFocus.Models;

/// <summary>
/// Um bloco contínuo de foco numa categoria. Id = 0 enquanto ainda não foi gravado no banco.
/// Datas sempre em UTC.
/// </summary>
public record FocusSession(long Id, ActivityCategory Category, DateTime StartedAt, DateTime EndedAt)
{
    public TimeSpan Duration => EndedAt - StartedAt;
}
