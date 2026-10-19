using System;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SprintFocus.Models;
using SprintFocus.Services;

namespace SprintFocus.ViewModels;

public partial class CategorySummary : ObservableObject
{
    public CategorySummary(ActivityCategory category)
    {
        Category = category;
        Brush = new SolidColorBrush(category.GetColor());
    }

    public ActivityCategory Category { get; }

    public IBrush Brush { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalText))]
    public partial TimeSpan Total { get; set; }

    /// <summary>Participação (0–100) desta categoria no total do dia.</summary>
    [ObservableProperty]
    public partial double Share { get; set; }

    public string TotalText => SummaryViewModel.Format(Total);
}

public partial class SummaryViewModel : ViewModelBase
{
    private readonly TimeLog _timeLog;

    public SummaryViewModel(TimeLog timeLog)
    {
        _timeLog = timeLog;
        _timeLog.Changed += (_, _) => Refresh();

        Rows = Enum.GetValues<ActivityCategory>().Select(c => new CategorySummary(c)).ToArray();
        Refresh();
    }

    public CategorySummary[] Rows { get; }

    [ObservableProperty]
    public partial string TotalText { get; set; } = "0min";

    private void Refresh()
    {
        foreach (var row in Rows)
            row.Total = _timeLog.GetTotal(row.Category);

        var total = TimeSpan.FromTicks(Rows.Sum(r => r.Total.Ticks));
        foreach (var row in Rows)
            row.Share = total.Ticks == 0 ? 0 : 100.0 * row.Total.Ticks / total.Ticks;

        TotalText = Format(total);
    }

    public static string Format(TimeSpan time) =>
        time.TotalHours >= 1
            ? $"{(int)time.TotalHours}h{time.Minutes:00}"
            : $"{time.Minutes}min {time.Seconds:00}s";
}
