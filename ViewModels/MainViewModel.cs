using CommunityToolkit.Mvvm.ComponentModel;

namespace SprintFocus.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to SprintFocus!";
}
