using AgroTrack.Services.Interfaces;
using AgroTrack.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AgroTrack.Services.Implementations;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ViewModelBase? CurrentViewModel => _currentViewModel;

    public event EventHandler<ViewModelBase>? CurrentViewModelChanged;
    public event Action? NavigateToLoginRequested;
    public event Action? NavigateToMainRequested;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        NavigateTo(viewModel);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        if (viewModel is null)
            throw new ArgumentNullException(nameof(viewModel));

        _currentViewModel = viewModel;
        CurrentViewModelChanged?.Invoke(this, viewModel);
    }

    public void RequestLoginView()
    {
        NavigateToLoginRequested?.Invoke();
    }

    public void RequestMainView()
    {
        NavigateToMainRequested?.Invoke();
    }
}
