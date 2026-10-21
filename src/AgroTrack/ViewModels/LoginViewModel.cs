using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private string _username = string.Empty;
    private string _password = string.Empty;

    public LoginViewModel(IAuthenticationService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Masuk - AgroTrack";

        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
        SetDemoUserCommand = new RelayCommand<string>(ExecuteSetDemoUser);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ClearError();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ClearError();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ICommand LoginCommand { get; }
    public ICommand SetDemoUserCommand { get; }

    private bool CanExecuteLogin()
    {
        return !IsLoading &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    private async Task ExecuteLoginAsync()
    {
        if (!CanExecuteLogin())
            return;

        IsLoading = true;
        ClearError();

        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (result.IsSuccess)
            {
                Password = string.Empty;
                _navigationService.RequestMainView();
            }
            else
            {
                SetError(result.ErrorMessage ?? "Nama pengguna atau kata sandi tidak valid.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Terjadi kesalahan: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ExecuteSetDemoUser(string? role)
    {
        switch (role?.ToLowerInvariant())
        {
            case "admin":
                Username = "admin";
                Password = "AdminPassword@123";
                break;
            case "manager":
                Username = "manager";
                Password = "ManagerPassword@123";
                break;
            case "petugas":
                Username = "petugas";
                Password = "PetugasPassword@123";
                break;
        }
    }
}
