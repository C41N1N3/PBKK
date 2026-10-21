using System.Collections.ObjectModel;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class LandViewModel : ViewModelBase
{
    private readonly ILandService _landService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    private string _searchTerm = string.Empty;
    private string _selectedSoilFilter = "Semua";
    private string _selectedStatusFilter = "Semua";

    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalCount = 0;
    private const int PageSize = 8;

    private bool _isFormOpen;
    private bool _isEditMode;

    // Form fields
    private int _formLandId;
    private string _formLandCode = string.Empty;
    private string _formLandName = string.Empty;
    private string _formLocation = string.Empty;
    private decimal _formAreaHectares;
    private string _formSoilType = "Aluvial";
    private LandStatus _formStatus = LandStatus.Active;
    private string? _formDescription;
    private string? _formValidationError;

    public LandViewModel(
        ILandService landService,
        IDialogService dialogService,
        ISessionService sessionService)
    {
        _landService = landService;
        _dialogService = dialogService;
        _sessionService = sessionService;

        Title = "Manajemen Lahan Pertanian";

        Lands = new ObservableCollection<Land>();
        SoilTypes = new ObservableCollection<string> { "Semua" };
        StatusFilterOptions = new ObservableCollection<string> { "Semua", "Aktif", "Bera / Istirahat", "Perawatan", "Nonaktif" };

        LoadLandsCommand = new AsyncRelayCommand(LoadLandsAsync);
        SearchCommand = new AsyncRelayCommand(() => { _currentPage = 1; return LoadLandsAsync(); });
        ResetFilterCommand = new AsyncRelayCommand(ExecuteResetFilterAsync);
        NextPageCommand = new AsyncRelayCommand(ExecuteNextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(ExecutePreviousPageAsync, () => HasPreviousPage);

        OpenAddFormCommand = new RelayCommand(ExecuteOpenAddForm, () => CanManage);
        OpenEditFormCommand = new RelayCommand<Land>(ExecuteOpenEditForm, _ => CanManage);
        SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
        CancelFormCommand = new RelayCommand(() => IsFormOpen = false);
        DeleteCommand = new AsyncRelayCommand<Land>(ExecuteDeleteAsync, _ => CanManage);

        _ = InitializeAsync();
    }

    public ObservableCollection<Land> Lands { get; }
    public ObservableCollection<string> SoilTypes { get; }
    public ObservableCollection<string> StatusFilterOptions { get; }

    public bool CanManage => _sessionService.CurrentUser?.CanManageMasterData ?? false;

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public string SelectedSoilFilter
    {
        get => _selectedSoilFilter;
        set
        {
            if (SetProperty(ref _selectedSoilFilter, value))
            {
                _currentPage = 1;
                _ = LoadLandsAsync();
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
                _currentPage = 1;
                _ = LoadLandsAsync();
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(PaginationDisplay));
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(PaginationDisplay));
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            if (SetProperty(ref _totalCount, value))
            {
                OnPropertyChanged(nameof(PaginationDisplay));
            }
        }
    }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PaginationDisplay => $"Halaman {CurrentPage} dari {Math.Max(1, TotalPages)} (Total {TotalCount} data)";

    // Form modal state
    public bool IsFormOpen
    {
        get => _isFormOpen;
        set => SetProperty(ref _isFormOpen, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(FormTitle));
            }
        }
    }

    public string FormTitle => IsEditMode ? "Ubah Data Lahan" : "Tambah Lahan Baru";

    public string FormLandCode
    {
        get => _formLandCode;
        set => SetProperty(ref _formLandCode, value);
    }

    public string FormLandName
    {
        get => _formLandName;
        set => SetProperty(ref _formLandName, value);
    }

    public string FormLocation
    {
        get => _formLocation;
        set => SetProperty(ref _formLocation, value);
    }

    public decimal FormAreaHectares
    {
        get => _formAreaHectares;
        set => SetProperty(ref _formAreaHectares, value);
    }

    public string FormSoilType
    {
        get => _formSoilType;
        set => SetProperty(ref _formSoilType, value);
    }

    public LandStatus FormStatus
    {
        get => _formStatus;
        set => SetProperty(ref _formStatus, value);
    }

    public string? FormDescription
    {
        get => _formDescription;
        set => SetProperty(ref _formDescription, value);
    }

    public string? FormValidationError
    {
        get => _formValidationError;
        set => SetProperty(ref _formValidationError, value);
    }

    // Commands
    public ICommand LoadLandsCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task InitializeAsync()
    {
        try
        {
            var soils = await _landService.GetSoilTypesAsync();
            foreach (var soil in soils)
            {
                if (!SoilTypes.Contains(soil))
                    SoilTypes.Add(soil);
            }
        }
        catch
        {
            // Ignore if DB not yet migrated
        }

        await LoadLandsAsync();
    }

    public async Task LoadLandsAsync()
    {
        IsLoading = true;
        ClearError();

        try
        {
            LandStatus? status = SelectedStatusFilter switch
            {
                "Aktif" => LandStatus.Active,
                "Bera / Istirahat" => LandStatus.Resting,
                "Perawatan" => LandStatus.Maintenance,
                "Nonaktif" => LandStatus.Inactive,
                _ => null
            };

            var paged = await _landService.GetLandsPagedAsync(
                SearchTerm, 
                status, 
                SelectedSoilFilter, 
                CurrentPage, 
                PageSize);

            Lands.Clear();
            foreach (var land in paged.Items)
            {
                Lands.Add(land);
            }

            TotalCount = paged.TotalCount;
            TotalPages = Math.Max(1, paged.TotalPages);
            IsEmpty = Lands.Count == 0;
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat data lahan: {ex.Message}");
            Lands.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteResetFilterAsync()
    {
        _searchTerm = string.Empty;
        _selectedSoilFilter = "Semua";
        _selectedStatusFilter = "Semua";
        _currentPage = 1;

        OnPropertyChanged(nameof(SearchTerm));
        OnPropertyChanged(nameof(SelectedSoilFilter));
        OnPropertyChanged(nameof(SelectedStatusFilter));

        await LoadLandsAsync();
    }

    private async Task ExecuteNextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadLandsAsync();
        }
    }

    private async Task ExecutePreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadLandsAsync();
        }
    }

    private void ExecuteOpenAddForm()
    {
        _formLandId = 0;
        FormLandCode = $"LHN-{DateTime.Now:yyyyMMdd}-{Lands.Count + 1:D2}";
        FormLandName = string.Empty;
        FormLocation = string.Empty;
        FormAreaHectares = 1.0m;
        FormSoilType = SoilTypes.Count > 1 ? SoilTypes[1] : "Aluvial";
        FormStatus = LandStatus.Active;
        FormDescription = string.Empty;
        FormValidationError = null;

        IsEditMode = false;
        IsFormOpen = true;
    }

    private void ExecuteOpenEditForm(Land? land)
    {
        if (land == null) return;

        _formLandId = land.LandId;
        FormLandCode = land.LandCode;
        FormLandName = land.LandName;
        FormLocation = land.Location;
        FormAreaHectares = land.AreaHectares;
        FormSoilType = land.SoilType;
        FormStatus = land.Status;
        FormDescription = land.Description;
        FormValidationError = null;

        IsEditMode = true;
        IsFormOpen = true;
    }

    private async Task ExecuteSaveAsync()
    {
        FormValidationError = null;

        var land = new Land
        {
            LandId = _formLandId,
            LandCode = FormLandCode,
            LandName = FormLandName,
            Location = FormLocation,
            AreaHectares = FormAreaHectares,
            SoilType = FormSoilType,
            Status = FormStatus,
            Description = FormDescription
        };

        if (IsEditMode)
        {
            var result = await _landService.UpdateLandAsync(land);
            if (result.IsSuccess)
            {
                IsFormOpen = false;
                _dialogService.ShowInformation("Data lahan berhasil diperbarui.", "Berhasil");
                await LoadLandsAsync();
            }
            else
            {
                FormValidationError = result.ErrorMessage;
            }
        }
        else
        {
            var result = await _landService.CreateLandAsync(land);
            if (result.IsSuccess)
            {
                IsFormOpen = false;
                _dialogService.ShowInformation("Lahan baru berhasil disimpan.", "Berhasil");
                await LoadLandsAsync();
            }
            else
            {
                FormValidationError = result.ErrorMessage;
            }
        }
    }

    private async Task ExecuteDeleteAsync(Land? land)
    {
        if (land == null) return;

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin menghapus lahan '{land.LandName}' ({land.LandCode})?", 
            "Konfirmasi Hapus Lahan");

        if (confirm)
        {
            var result = await _landService.DeleteLandAsync(land.LandId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Data lahan berhasil dihapus.", "Berhasil");
                await LoadLandsAsync();
            }
            else
            {
                _dialogService.ShowWarning(result.ErrorMessage ?? "Gagal menghapus lahan.", "Peringatan");
            }
        }
    }
}
