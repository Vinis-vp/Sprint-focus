using System;
using System.Collections.Generic;
using SprintFocus.Models;

namespace SprintFocus.Services;

/// <summary>
/// Registro em memória do tempo gasto por categoria no dia.
/// Futuramente será trocado por persistência (SQLite) sem mudar quem o consome.
/// </summary>
public class TimeLog
{
    private readonly Dictionary<ActivityCategory, TimeSpan> _totals = new();

    public event EventHandler? Changed;

    public TimeSpan GetTotal(ActivityCategory category) =>
        _totals.TryGetValue(category, out var total) ? total : TimeSpan.Zero;

    public void Add(ActivityCategory category, TimeSpan duration)
    {
        _totals[category] = GetTotal(category) + duration;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
