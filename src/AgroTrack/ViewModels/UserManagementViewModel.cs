using System.Collections.ObjectModel;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class UserManagementViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    private List<User> _allUsers = new();
    private ObservableCollection<User> _filteredUsers = new();
    private User? _selectedUser;

    private string _searchKeyword = string.Empty;
    private string _selectedRoleFilter = "Semua Role";
    private string _selectedStatusFilter = "Semua Status";

    private bool _isEditorOpen;
    private bool _isResetPasswordOpen;
    private bool _isEditing;

    // Form inputs
    private int _editingUserId;
    private string _formUsername = string.Empty;
    private string _formFullName = string.Empty;
    private UserRole _formRole = UserRole.FieldOfficer;
    private string _formPassword = string.Empty;
    private string _newPasswordForReset = string.Empty;
    private string _formErrorMessage = string.Empty;

    public UserManagementViewModel(
        IUserService userService,
        IDialogService dialogService,
        ISessionService sessionService)
    {
        _userService = userService;
        _dialogService = dialogService;
        _sessionService = sessionService;

        Title = "Manajemen Pengguna";

        LoadUsersCommand = new AsyncRelayCommand(async () => await LoadUsersAsync());
        OpenAddUserCommand = new RelayCommand(() => OpenAddDialog());
        OpenEditUserCommand = new RelayCommand(() => OpenEditDialog(), () => SelectedUser != null);
        OpenResetPasswordCommand = new RelayCommand(() => OpenResetPasswordDialog(), () => SelectedUser != null);
        SaveUserCommand = new AsyncRelayCommand(async () => await SaveUserAsync());
        SaveResetPasswordCommand = new AsyncRelayCommand(async () => await SaveResetPasswordAsync());
        ToggleStatusCommand = new AsyncRelayCommand(async () => await ToggleStatusAsync(), () => SelectedUser != null);
        CloseDialogCommand = new RelayCommand(() => CloseDialogs());
    }

    public ObservableCollection<User> FilteredUsers
    {
        get => _filteredUsers;
        set => SetProperty(ref _filteredUsers, value);
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                (OpenEditUserCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (OpenResetPasswordCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ToggleStatusCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                ApplyFilter();
            }
        }
    }

    public string SelectedRoleFilter
    {
        get => _selectedRoleFilter;
        set
        {
            if (SetProperty(ref _selectedRoleFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        set => SetProperty(ref _isEditorOpen, value);
    }

    public bool IsResetPasswordOpen
    {
        get => _isResetPasswordOpen;
        set => SetProperty(ref _isResetPasswordOpen, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string FormUsername
    {
        get => _formUsername;
        set => SetProperty(ref _formUsername, value);
    }

    public string FormFullName
    {
        get => _formFullName;
        set => SetProperty(ref _formFullName, value);
    }

    public UserRole FormRole
    {
        get => _formRole;
        set => SetProperty(ref _formRole, value);
    }

    public string FormPassword
    {
        get => _formPassword;
        set => SetProperty(ref _formPassword, value);
    }

    public string NewPasswordForReset
    {
        get => _newPasswordForReset;
        set => SetProperty(ref _newPasswordForReset, value);
    }

    public string FormErrorMessage
    {
        get => _formErrorMessage;
        set => SetProperty(ref _formErrorMessage, value);
    }

    public IReadOnlyList<UserRole> AvailableRoles { get; } = new[]
    {
        UserRole.Administrator,
        UserRole.Manager,
        UserRole.FieldOfficer
    };

    public IReadOnlyList<string> RoleFilterOptions { get; } = new[]
    {
        "Semua Role",
        "Administrator",
        "Manager",
        "FieldOfficer"
    };

    public IReadOnlyList<string> StatusFilterOptions { get; } = new[]
    {
        "Semua Status",
        "Aktif",
        "Nonaktif"
    };

    public ICommand LoadUsersCommand { get; }
    public ICommand OpenAddUserCommand { get; }
    public ICommand OpenEditUserCommand { get; }
    public ICommand OpenResetPasswordCommand { get; }
    public ICommand SaveUserCommand { get; }
    public ICommand SaveResetPasswordCommand { get; }
    public ICommand ToggleStatusCommand { get; }
    public ICommand CloseDialogCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var list = await _userService.GetAllUsersAsync();
            _allUsers = list.ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Gagal memuat pengguna: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var query = _allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLowerInvariant();
            query = query.Where(u => u.Username.ToLowerInvariant().Contains(kw) ||
                                     u.FullName.ToLowerInvariant().Contains(kw));
        }

        if (SelectedRoleFilter != "Semua Role")
        {
            query = query.Where(u => u.Role.ToString().Equals(SelectedRoleFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatusFilter == "Aktif")
        {
            query = query.Where(u => u.IsActive);
        }
        else if (SelectedStatusFilter == "Nonaktif")
        {
            query = query.Where(u => !u.IsActive);
        }

        FilteredUsers = new ObservableCollection<User>(query);
    }

    private void OpenAddDialog()
    {
        FormErrorMessage = string.Empty;
        IsEditing = false;
        _editingUserId = 0;
        FormUsername = string.Empty;
        FormFullName = string.Empty;
        FormRole = UserRole.FieldOfficer;
        FormPassword = string.Empty;

        IsEditorOpen = true;
    }

    private void OpenEditDialog()
    {
        if (SelectedUser == null) return;

        FormErrorMessage = string.Empty;
        IsEditing = true;
        _editingUserId = SelectedUser.UserId;
        FormUsername = SelectedUser.Username;
        FormFullName = SelectedUser.FullName;
        FormRole = SelectedUser.Role;
        FormPassword = string.Empty;

        IsEditorOpen = true;
    }

    private void OpenResetPasswordDialog()
    {
        if (SelectedUser == null) return;

        FormErrorMessage = string.Empty;
        NewPasswordForReset = string.Empty;
        IsResetPasswordOpen = true;
    }

    private void CloseDialogs()
    {
        IsEditorOpen = false;
        IsResetPasswordOpen = false;
        FormErrorMessage = string.Empty;
    }

    private async Task SaveUserAsync()
    {
        FormErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            if (IsEditing)
            {
                var result = await _userService.UpdateUserAsync(_editingUserId, FormFullName, FormRole);
                if (result.IsFailure)
                {
                    FormErrorMessage = result.ErrorMessage ?? "Gagal memperbarui data pengguna.";
                    return;
                }
                _dialogService.ShowInformation("Data pengguna berhasil diperbarui.", "Sukses");
            }
            else
            {
                var result = await _userService.CreateUserAsync(FormUsername, FormFullName, FormRole, FormPassword);
                if (result.IsFailure)
                {
                    FormErrorMessage = result.ErrorMessage ?? "Gagal menambahkan pengguna baru.";
                    return;
                }
                _dialogService.ShowInformation($"Pengguna baru '{FormUsername}' berhasil ditambahkan.", "Sukses");
            }

            CloseDialogs();
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Terjadi kesalahan: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveResetPasswordAsync()
    {
        if (SelectedUser == null) return;

        FormErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var result = await _userService.ResetPasswordAsync(SelectedUser.UserId, NewPasswordForReset);
            if (result.IsFailure)
            {
                FormErrorMessage = result.ErrorMessage ?? "Gagal mereset kata sandi.";
                return;
            }

            _dialogService.ShowInformation($"Password untuk pengguna '{SelectedUser.Username}' berhasil direset.", "Sukses");
            CloseDialogs();
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Gagal mereset password: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleStatusAsync()
    {
        if (SelectedUser == null) return;

        var actionText = SelectedUser.IsActive ? "menonaktifkan" : "mengaktifkan";
        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin {actionText} pengguna '{SelectedUser.Username}' ({SelectedUser.FullName})?",
            "Konfirmasi Status Pengguna");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _userService.ToggleUserStatusAsync(SelectedUser.UserId);
            if (result.IsFailure)
            {
                _dialogService.ShowError(result.ErrorMessage ?? "Gagal mengubah status.", "Peringatan");
                return;
            }

            _dialogService.ShowInformation($"Status akun pengguna '{SelectedUser.Username}' berhasil diubah.", "Sukses");
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Gagal mengubah status: {ex.Message}", "Kesalahan");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
