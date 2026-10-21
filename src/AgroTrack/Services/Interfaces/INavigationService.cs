using AgroTrack.ViewModels;

namespace AgroTrack.Services.Interfaces;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    event EventHandler<ViewModelBase>? CurrentViewModelChanged;
    event Action? NavigateToLoginRequested;
    event Action? NavigateToMainRequested;

    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo(ViewModelBase viewModel);
    void RequestLoginView();
    void RequestMainView();
}
