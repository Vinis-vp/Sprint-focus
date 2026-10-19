using Avalonia.Media;

namespace SprintFocus.Models;

public enum ActivityCategory
{
    Trabalho,
    Estudo,
    Lazer,
}

public static class ActivityCategoryExtensions
{
    public static Color GetColor(this ActivityCategory category) => category switch
    {
        ActivityCategory.Trabalho => Color.Parse("#4F7CFF"),
        ActivityCategory.Estudo => Color.Parse("#2EB872"),
        ActivityCategory.Lazer => Color.Parse("#F5A524"),
        _ => Colors.Gray,
    };
}
