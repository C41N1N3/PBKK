using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _currentContentViewModel;
    private string _activeMenu = "Dashboard";

    public MainWindowViewModel(
        INavigationService navigationService,
        ISessionService sessionService,
        IThemeService themeService,
        IDialogService dialogService,
        IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _sessionService = sessionService;
        _themeService = themeService;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;

        Title = "AgroTrack - Sistem Informasi Manajemen Produksi Pertanian";

        _navigationService.CurrentViewModelChanged += (_, vm) =>
        {
            CurrentContentViewModel = vm;
        };

        _sessionService.SessionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsManager));
            OnPropertyChanged(nameof(IsFieldOfficer));
            OnPropertyChanged(nameof(CanViewCosts));
            OnPropertyChanged(nameof(CanViewReports));
            OnPropertyChanged(nameof(CanManageUsers));
        };

        NavigateCommand = new RelayCommand<string>(ExecuteNavigate);
        ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);
        LogoutCommand = new RelayCommand(ExecuteLogout);
    }

    public CurrentUser? CurrentUser => _sessionService.CurrentUser;
    public bool IsAuthenticated => _sessionService.IsAuthenticated;

    public bool IsAdmin => CurrentUser?.IsAdmin ?? false;
    public bool IsManager => CurrentUser?.IsManager ?? false;
    public bool IsFieldOfficer => CurrentUser?.IsFieldOfficer ?? false;

    public bool CanViewCosts => IsAdmin || IsManager;
    public bool CanViewReports => IsAdmin || IsManager;
    public bool CanManageUsers => IsAdmin;

    public ViewModelBase? CurrentContentViewModel
    {
        get => _currentContentViewModel;
        set => SetProperty(ref _currentContentViewModel, value);
    }

    public string ActiveMenu
    {
        get => _activeMenu;
        set => SetProperty(ref _activeMenu, value);
    }

    public string CurrentThemeDisplay => _themeService.CurrentTheme == Enums.AppTheme.Light ? "Mode Gelap 🌙" : "Mode Terang ☀️";

    public ICommand NavigateCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LogoutCommand { get; }

    public void ExecuteNavigate(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return;

        ActiveMenu = destination;
        NavigateToDestination(destination);
    }

    private void NavigateToDestination(string destination)
    {
        switch (destination)
        {
            case "Dashboard":
                if (_serviceProvider.GetService(typeof(DashboardViewModel)) is ViewModelBase dvm)
                    _navigationService.NavigateTo(dvm);
                break;
            case "Lands":
                if (_serviceProvider.GetService(typeof(LandViewModel)) is ViewModelBase lvm)
                    _navigationService.NavigateTo(lvm);
                break;
            case "Commodities":
                if (_serviceProvider.GetService(typeof(CommodityViewModel)) is ViewModelBase cvm)
                    _navigationService.NavigateTo(cvm);
                break;
            case "Plantings":
                if (_serviceProvider.GetService(typeof(PlantingViewModel)) is ViewModelBase pvm)
                    _navigationService.NavigateTo(pvm);
                break;
            case "Activities":
                if (_serviceProvider.GetService(typeof(ActivityViewModel)) is ViewModelBase avm)
                    _navigationService.NavigateTo(avm);
                break;
            case "Materials":
                if (_serviceProvider.GetService(typeof(MaterialViewModel)) is ViewModelBase mvm)
                    _navigationService.NavigateTo(mvm);
                break;
            case "Costs":
                if (CanViewCosts && _serviceProvider.GetService(typeof(CostViewModel)) is ViewModelBase costVm)
                    _navigationService.NavigateTo(costVm);
                break;
            case "Harvests":
                if (_serviceProvider.GetService(typeof(HarvestViewModel)) is ViewModelBase hvm)
                    _navigationService.NavigateTo(hvm);
                break;
            case "Reports":
                if (CanViewReports && _serviceProvider.GetService(typeof(ReportViewModel)) is ViewModelBase rvm)
                    _navigationService.NavigateTo(rvm);
                break;
            case "Users":
                if (CanManageUsers && _serviceProvider.GetService(typeof(UserManagementViewModel)) is ViewModelBase uvm)
                    _navigationService.NavigateTo(uvm);
                break;
        }
    }

    private void ExecuteToggleTheme()
    {
        _themeService.ToggleTheme();
        OnPropertyChanged(nameof(CurrentThemeDisplay));
    }

    private void ExecuteLogout()
    {
        var confirm = _dialogService.ShowConfirmation("Apakah Anda yakin ingin keluar dari sistem AgroTrack?", "Konfirmasi Keluar");
        if (confirm)
        {
            _sessionService.ClearSession();
            _navigationService.RequestLoginView();
        }
    }
}
