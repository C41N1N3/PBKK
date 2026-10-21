using System.Collections.ObjectModel;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class ActivityViewModel : ViewModelBase
{
    private readonly IActivityService _activityService;
    private readonly IPlantingService _plantingService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;

    // Filter properties
    private string _searchTerm = string.Empty;
    private int? _selectedPlantingId;
    private ActivityType? _selectedTypeFilter;
    private ActivityStatus? _selectedStatusFilter;
    private DateTime? _startDateFilter;
    private DateTime? _endDateFilter;

    // Pagination
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalPages = 1;
    private int _totalItems = 0;

    // Form Modal
    private bool _isFormOpen;
    private bool _isEditing;
    private int _editingActivityId;
    private int _formPlantingId;
    private ActivityType _formActivityType = ActivityType.Inspection;
    private DateTime _formActivityDate = DateTime.Today;
    private decimal? _formDurationHours;
    private ActivityStatus _formStatus = ActivityStatus.Scheduled;
    private string _formDescription = string.Empty;

    // Lists for dropdowns
    private ObservableCollection<FarmingActivity> _activities = new();
    private ObservableCollection<Planting> _plantings = new();

    public ActivityViewModel(
        IActivityService activityService,
        IPlantingService plantingService,
        ISessionService sessionService,
        IDialogService dialogService)
    {
        _activityService = activityService;
        _plantingService = plantingService;
        _sessionService = sessionService;
        _dialogService = dialogService;

        Title = "Aktivitas Pertanian";

        // Commands
        LoadDataCommand = new AsyncRelayCommand(async () => await LoadDataAsync());
        SearchCommand = new AsyncRelayCommand(async () =>
        {
            _currentPage = 1;
            await LoadDataAsync();
        });
        ResetFilterCommand = new AsyncRelayCommand(async () =>
        {
            SearchTerm = string.Empty;
            SelectedPlantingId = null;
            SelectedTypeFilter = null;
            SelectedStatusFilter = null;
            StartDateFilter = null;
            EndDateFilter = null;
            _currentPage = 1;
            await LoadDataAsync();
        });

        NextPageCommand = new AsyncRelayCommand(async () =>
        {
            if (HasNextPage)
            {
                _currentPage++;
                await LoadDataAsync();
            }
        }, () => HasNextPage);

        PreviousPageCommand = new AsyncRelayCommand(async () =>
        {
            if (HasPreviousPage)
            {
                _currentPage--;
                await LoadDataAsync();
            }
        }, () => HasPreviousPage);

        OpenCreateFormCommand = new RelayCommand(_ => OpenCreateForm());
        OpenEditFormCommand = new RelayCommand(param =>
        {
            if (param is FarmingActivity activity)
                OpenEditForm(activity);
        });
        SaveFormCommand = new AsyncRelayCommand(async () => await SaveFormAsync(), () => !IsLoading);
        CancelFormCommand = new RelayCommand(_ => IsFormOpen = false);

        DeleteCommand = new AsyncRelayCommand<FarmingActivity>(async activity =>
        {
            if (activity != null)
                await DeleteActivityAsync(activity);
        }, _ => CanDelete);

        // Load Initial Data
        _ = LoadLookupsAndDataAsync();
    }

    public ObservableCollection<FarmingActivity> Activities
    {
        get => _activities;
        set => SetProperty(ref _activities, value);
    }

    public ObservableCollection<Planting> Plantings
    {
        get => _plantings;
        set => SetProperty(ref _plantings, value);
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public int? SelectedPlantingId
    {
        get => _selectedPlantingId;
        set => SetProperty(ref _selectedPlantingId, value);
    }

    public ActivityType? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set => SetProperty(ref _selectedTypeFilter, value);
    }

    public ActivityStatus? SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set => SetProperty(ref _selectedStatusFilter, value);
    }

    public DateTime? StartDateFilter
    {
        get => _startDateFilter;
        set => SetProperty(ref _startDateFilter, value);
    }

    public DateTime? EndDateFilter
    {
        get => _endDateFilter;
        set => SetProperty(ref _endDateFilter, value);
    }

    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set => SetProperty(ref _totalItems, value);
    }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public bool CanDelete => _sessionService.CurrentUser?.Role == UserRole.Administrator ||
                             _sessionService.CurrentUser?.Role == UserRole.Manager;

    // Form Modal Bindings
    public bool IsFormOpen
    {
        get => _isFormOpen;
        set => SetProperty(ref _isFormOpen, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string FormTitle => IsEditing ? "Ubah Aktivitas Pertanian" : "Tambah Aktivitas Baru";

    public int FormPlantingId
    {
        get => _formPlantingId;
        set => SetProperty(ref _formPlantingId, value);
    }

    public ActivityType FormActivityType
    {
        get => _formActivityType;
        set => SetProperty(ref _formActivityType, value);
    }

    public DateTime FormActivityDate
    {
        get => _formActivityDate;
        set => SetProperty(ref _formActivityDate, value);
    }

    public decimal? FormDurationHours
    {
        get => _formDurationHours;
        set => SetProperty(ref _formDurationHours, value);
    }

    public ActivityStatus FormStatus
    {
        get => _formStatus;
        set => SetProperty(ref _formStatus, value);
    }

    public string FormDescription
    {
        get => _formDescription;
        set => SetProperty(ref _formDescription, value);
    }

    // Commands
    public ICommand LoadDataCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand OpenCreateFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteCommand { get; }

    public IEnumerable<ActivityType> ActivityTypes => Enum.GetValues<ActivityType>();
    public IEnumerable<ActivityStatus> ActivityStatuses => Enum.GetValues<ActivityStatus>();

    private async Task LoadLookupsAndDataAsync()
    {
        try
        {
            var pList = await _plantingService.GetActivePlantingsAsync();
            Plantings = new ObservableCollection<Planting>(pList);
        }
        catch
        {
            // Fallback lookup
        }

        await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ClearError();

        try
        {
            var paged = await _activityService.GetActivitiesPagedAsync(
                SelectedPlantingId,
                SelectedTypeFilter,
                SelectedStatusFilter,
                null,
                StartDateFilter,
                EndDateFilter,
                SearchTerm,
                CurrentPage,
                _pageSize);

            Activities = new ObservableCollection<FarmingActivity>(paged.Items);
            TotalItems = paged.TotalCount;
            TotalPages = paged.TotalPages;
            IsEmpty = Activities.Count == 0;

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat daftar aktivitas: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenCreateForm()
    {
        IsEditing = false;
        _editingActivityId = 0;
        FormPlantingId = Plantings.FirstOrDefault()?.PlantingId ?? 0;
        FormActivityType = ActivityType.Inspection;
        FormActivityDate = DateTime.Today;
        FormDurationHours = null;
        FormStatus = ActivityStatus.Scheduled;
        FormDescription = string.Empty;

        IsFormOpen = true;
    }

    private void OpenEditForm(FarmingActivity activity)
    {
        // Field Officer can only edit own activities
        if (_sessionService.CurrentUser?.Role == UserRole.FieldOfficer &&
            activity.OfficerId != _sessionService.CurrentUser.UserId)
        {
            _dialogService.ShowWarning("Anda hanya diizinkan untuk mengubah aktivitas yang Anda buat sendiri.");
            return;
        }

        IsEditing = true;
        _editingActivityId = activity.ActivityId;
        FormPlantingId = activity.PlantingId;
        FormActivityType = activity.ActivityType;
        FormActivityDate = activity.ActivityDate;
        FormDurationHours = activity.DurationHours;
        FormStatus = activity.Status;
        FormDescription = activity.Description;

        IsFormOpen = true;
    }

    private async Task SaveFormAsync()
    {
        if (FormPlantingId <= 0)
        {
            _dialogService.ShowWarning("Silakan pilih penanaman.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FormDescription))
        {
            _dialogService.ShowWarning("Deskripsi aktivitas wajib diisi.");
            return;
        }

        IsLoading = true;
        try
        {
            var activity = new FarmingActivity
            {
                ActivityId = _editingActivityId,
                PlantingId = FormPlantingId,
                ActivityType = FormActivityType,
                ActivityDate = FormActivityDate,
                DurationHours = FormDurationHours,
                Status = FormStatus,
                Description = FormDescription
            };

            if (IsEditing)
            {
                var result = await _activityService.UpdateActivityAsync(activity);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Aktivitas pertanian berhasil diperbarui.");
                    IsFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal memperbarui aktivitas.");
                }
            }
            else
            {
                var result = await _activityService.CreateActivityAsync(activity);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Aktivitas pertanian berhasil ditambahkan.");
                    IsFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal menambahkan aktivitas.");
                }
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Terjadi kesalahan: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteActivityAsync(FarmingActivity activity)
    {
        if (!CanDelete)
        {
            _dialogService.ShowWarning("Akun Anda tidak memiliki hak akses untuk menghapus data aktivitas.");
            return;
        }

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin menghapus aktivitas '{activity.ActivityTypeDisplay}' pada penanaman {activity.PlantingCode}?\n\nTindakan ini tidak dapat dibatalkan.",
            "Konfirmasi Hapus Aktivitas");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _activityService.DeleteActivityAsync(activity.ActivityId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Aktivitas berhasil dihapus.");
                await LoadDataAsync();
            }
            else
            {
                _dialogService.ShowError(result.ErrorMessage ?? "Gagal menghapus aktivitas.");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Terjadi kesalahan saat menghapus: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
