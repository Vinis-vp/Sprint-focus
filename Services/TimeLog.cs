using System;
using System.Collections.Generic;
using SprintFocus.Models;

namespace SprintFocus.Services;

/// <summary>
/// Registro do tempo de foco por categoria, persistido em SQLite como sessões.
///
/// O timer chama <see cref="Add"/> a cada tick (~250 ms). Para não gravar no disco a cada tick,
/// mantemos uma sessão "aberta" em memória que vai crescendo; ela é gravada quando fecha
/// (outra categoria, pausa longa entre ticks) ou periodicamente, a cada <see cref="SaveInterval"/>.
/// </summary>
public class TimeLog
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan MaxGap = TimeSpan.FromSeconds(5);

    private readonly SessionStore _store;

    // Totais de hoje das sessões já gravadas + o que a sessão aberta acumulou até agora.
    private Dictionary<ActivityCategory, TimeSpan> _todayTotals = new();
    private DateTime _totalsDay;

    private FocusSession? _current;
    private DateTime _lastSaveUtc = DateTime.UtcNow;

    public TimeLog(SessionStore store)
    {
        _store = store;
        ReloadToday();
    }

    public event EventHandler? Changed;

    public TimeSpan GetTotal(ActivityCategory category)
    {
        EnsureToday();
        return _todayTotals.TryGetValue(category, out var total) ? total : TimeSpan.Zero;
    }

    public void Add(ActivityCategory category, TimeSpan duration)
    {
        EnsureToday();

        var now = DateTime.UtcNow;
        var chunkStart = now - duration;

        if (_current is not null && _current.Category == category && chunkStart - _current.EndedAt <= MaxGap)
        {
            _current = _current! with { EndedAt = now };
        }
        else
        {
            CloseCurrent();
            _current = new FocusSession(0, category, chunkStart, now);
        }

        _todayTotals[category] = GetTotal(category) + duration;

        if (now - _lastSaveUtc >= SaveInterval)
            SaveCurrent();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Grava a sessão aberta e a encerra. Chamar ao fechar o app.</summary>
    public void Flush() => CloseCurrent();

    private void CloseCurrent()
    {
        SaveCurrent();
        _current = null;
    }

    /// <summary>Insere a sessão aberta (primeira vez) ou atualiza o fim dela no banco.</summary>
    private void SaveCurrent()
    {
        _lastSaveUtc = DateTime.UtcNow;
        if (_current is null || _current.Duration <= TimeSpan.Zero)
            return;

        if (_current.Id == 0)
            _current = _current with { Id = _store.Insert(_current) };
        else
            _store.UpdateEnd(_current.Id, _current.EndedAt);
    }

    /// <summary>Virou o dia desde a última leitura? Fecha a sessão e recarrega os totais do novo dia.</summary>
    private void EnsureToday()
    {
        if (DateTime.Today == _totalsDay)
            return;

        CloseCurrent();
        ReloadToday();
    }

    private void ReloadToday()
    {
        _totalsDay = DateTime.Today;
        _todayTotals = _store.GetTotalsSince(_totalsDay.ToUniversalTime());
    }
}
