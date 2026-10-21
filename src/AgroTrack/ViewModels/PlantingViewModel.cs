using System.Collections.ObjectModel;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class PlantingViewModel : ViewModelBase
{
    private readonly IPlantingService _plantingService;
    private readonly ILandService _landService;
    private readonly ICommodityService _commodityService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    private string _searchTerm = string.Empty;
    private string _selectedStatusFilter = "Semua";
    private Land? _selectedLandFilter;
    private Commodity? _selectedCommodityFilter;
    private DateTime? _startDateFrom;
    private DateTime? _startDateTo;

    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalCount = 0;
    private const int PageSize = 7;

    private Planting? _selectedPlanting;
    private bool _isDetailOpen;
    private bool _isFormOpen;
    private bool _isEditMode;

    // Form inputs
    private int _formPlantingId;
    private string _formPlantingCode = string.Empty;
    private string _formSeasonName = string.Empty;
    private Land? _formSelectedLand;
    private Commodity? _formSelectedCommodity;
    private DateTime _formStartDate = DateTime.Today;
    private DateTime _formExpectedHarvestDate = DateTime.Today.AddDays(90);
    private decimal _formPlantedAreaHectares = 1.0m;
    private decimal _formTargetYield = 5000m;
    private PlantingStatus _formStatus = PlantingStatus.Planned;
    private string? _formNotes;
    private string? _formValidationError;

    public PlantingViewModel(
        IPlantingService plantingService,
        ILandService landService,
        ICommodityService commodityService,
        IDialogService dialogService,
        ISessionService sessionService)
    {
        _plantingService = plantingService;
        _landService = landService;
        _commodityService = commodityService;
        _dialogService = dialogService;
        _sessionService = sessionService;

        Title = "Manajemen Musim Tanam (Penanaman)";

        Plantings = new ObservableCollection<Planting>();
        AvailableLands = new ObservableCollection<Land>();
        AvailableCommodities = new ObservableCollection<Commodity>();
        StatusFilterOptions = new ObservableCollection<string> { "Semua", "Direncanakan", "Aktif", "Siap Panen", "Selesai", "Dibatalkan" };
        FormStatusOptions = new ObservableCollection<PlantingStatus>
        {
            PlantingStatus.Planned,
            PlantingStatus.Active,
            PlantingStatus.ReadyToHarvest,
            PlantingStatus.Completed,
            PlantingStatus.Cancelled
        };

        LoadPlantingsCommand = new AsyncRelayCommand(LoadPlantingsAsync);
        SearchCommand = new AsyncRelayCommand(() => { _currentPage = 1; return LoadPlantingsAsync(); });
        ResetFilterCommand = new AsyncRelayCommand(ExecuteResetFilterAsync);
        NextPageCommand = new AsyncRelayCommand(ExecuteNextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(ExecutePreviousPageAsync, () => HasPreviousPage);

        OpenDetailCommand = new RelayCommand<Planting>(ExecuteOpenDetail);
        CloseDetailCommand = new RelayCommand(() => IsDetailOpen = false);

        OpenAddFormCommand = new RelayCommand(ExecuteOpenAddForm, () => CanManage);
        OpenEditFormCommand = new RelayCommand<Planting>(ExecuteOpenEditForm, _ => CanManage);
        SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
        CancelFormCommand = new RelayCommand(() => IsFormOpen = false);
        DeleteCommand = new AsyncRelayCommand<Planting>(ExecuteDeleteAsync, _ => CanManage);
        ChangeStatusCommand = new AsyncRelayCommand<PlantingStatus>(ExecuteChangeStatusAsync, _ => CanManage && SelectedPlanting != null);

        _ = InitializeAsync();
    }

    public ObservableCollection<Planting> Plantings { get; }
    public ObservableCollection<Land> AvailableLands { get; }
    public ObservableCollection<Commodity> AvailableCommodities { get; }
    public ObservableCollection<string> StatusFilterOptions { get; }
    public ObservableCollection<PlantingStatus> FormStatusOptions { get; }

    public bool CanManage => _sessionService.CurrentUser?.CanManageMasterData ?? false;

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                _currentPage = 1;
                _ = LoadPlantingsAsync();
            }
        }
    }

    public Land? SelectedLandFilter
    {
        get => _selectedLandFilter;
        set
        {
            if (SetProperty(ref _selectedLandFilter, value))
            {
                _currentPage = 1;
                _ = LoadPlantingsAsync();
            }
        }
    }

    public Commodity? SelectedCommodityFilter
    {
        get => _selectedCommodityFilter;
        set
        {
            if (SetProperty(ref _selectedCommodityFilter, value))
            {
                _currentPage = 1;
                _ = LoadPlantingsAsync();
            }
        }
    }

    public DateTime? StartDateFrom
    {
        get => _startDateFrom;
        set
        {
            if (SetProperty(ref _startDateFrom, value))
            {
                _currentPage = 1;
                _ = LoadPlantingsAsync();
            }
        }
    }

    public DateTime? StartDateTo
    {
        get => _startDateTo;
        set
        {
            if (SetProperty(ref _startDateTo, value))
            {
                _currentPage = 1;
                _ = LoadPlantingsAsync();
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

    // Detail Drawer State
    public Planting? SelectedPlanting
    {
        get => _selectedPlanting;
        set => SetProperty(ref _selectedPlanting, value);
    }

    public bool IsDetailOpen
    {
        get => _isDetailOpen;
        set => SetProperty(ref _isDetailOpen, value);
    }

    // Modal Form State
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

    public string FormTitle => IsEditMode ? "Ubah Data Musim Tanam" : "Tambah Musim Tanam Baru";

    public string FormPlantingCode
    {
        get => _formPlantingCode;
        set => SetProperty(ref _formPlantingCode, value);
    }

    public string FormSeasonName
    {
        get => _formSeasonName;
        set => SetProperty(ref _formSeasonName, value);
    }

    public Land? FormSelectedLand
    {
        get => _formSelectedLand;
        set => SetProperty(ref _formSelectedLand, value);
    }

    public Commodity? FormSelectedCommodity
    {
        get => _formSelectedCommodity;
        set
        {
            if (SetProperty(ref _formSelectedCommodity, value) && value != null)
            {
                FormExpectedHarvestDate = FormStartDate.AddDays(value.EstimatedGrowthDays);
            }
        }
    }

    public DateTime FormStartDate
    {
        get => _formStartDate;
        set
        {
            if (SetProperty(ref _formStartDate, value) && FormSelectedCommodity != null)
            {
                FormExpectedHarvestDate = value.AddDays(FormSelectedCommodity.EstimatedGrowthDays);
            }
        }
    }

    public DateTime FormExpectedHarvestDate
    {
        get => _formExpectedHarvestDate;
        set => SetProperty(ref _formExpectedHarvestDate, value);
    }

    public decimal FormPlantedAreaHectares
    {
        get => _formPlantedAreaHectares;
        set => SetProperty(ref _formPlantedAreaHectares, value);
    }

    public decimal FormTargetYield
    {
        get => _formTargetYield;
        set => SetProperty(ref _formTargetYield, value);
    }

    public PlantingStatus FormStatus
    {
        get => _formStatus;
        set => SetProperty(ref _formStatus, value);
    }

    public string? FormNotes
    {
        get => _formNotes;
        set => SetProperty(ref _formNotes, value);
    }

    public string? FormValidationError
    {
        get => _formValidationError;
        set => SetProperty(ref _formValidationError, value);
    }

    // Commands
    public ICommand LoadPlantingsCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand CloseDetailCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ChangeStatusCommand { get; }

    private async Task InitializeAsync()
    {
        try
        {
            var lands = await _landService.GetActiveLandsAsync();
            AvailableLands.Clear();
            foreach (var l in lands) AvailableLands.Add(l);

            var commodities = await _commodityService.GetActiveCommoditiesAsync();
            AvailableCommodities.Clear();
            foreach (var c in commodities) AvailableCommodities.Add(c);
        }
        catch
        {
            // Handled
        }

        await LoadPlantingsAsync();
    }

    public async Task LoadPlantingsAsync()
    {
        IsLoading = true;
        ClearError();

        try
        {
            PlantingStatus? status = SelectedStatusFilter switch
            {
                "Direncanakan" => PlantingStatus.Planned,
                "Aktif" => PlantingStatus.Active,
                "Siap Panen" => PlantingStatus.ReadyToHarvest,
                "Selesai" => PlantingStatus.Completed,
                "Dibatalkan" => PlantingStatus.Cancelled,
                _ => null
            };

            var paged = await _plantingService.GetPlantingsPagedAsync(
                SearchTerm,
                status,
                SelectedLandFilter?.LandId,
                SelectedCommodityFilter?.CommodityId,
                StartDateFrom,
                StartDateTo,
                CurrentPage,
                PageSize);

            Plantings.Clear();
            foreach (var item in paged.Items)
            {
                Plantings.Add(item);
            }

            TotalCount = paged.TotalCount;
            TotalPages = Math.Max(1, paged.TotalPages);
            IsEmpty = Plantings.Count == 0;
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat data penanaman: {ex.Message}");
            Plantings.Clear();
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
        _selectedStatusFilter = "Semua";
        _selectedLandFilter = null;
        _selectedCommodityFilter = null;
        _startDateFrom = null;
        _startDateTo = null;
        _currentPage = 1;

        OnPropertyChanged(nameof(SearchTerm));
        OnPropertyChanged(nameof(SelectedStatusFilter));
        OnPropertyChanged(nameof(SelectedLandFilter));
        OnPropertyChanged(nameof(SelectedCommodityFilter));
        OnPropertyChanged(nameof(StartDateFrom));
        OnPropertyChanged(nameof(StartDateTo));

        await LoadPlantingsAsync();
    }

    private async Task ExecuteNextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadPlantingsAsync();
        }
    }

    private async Task ExecutePreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadPlantingsAsync();
        }
    }

    private void ExecuteOpenDetail(Planting? item)
    {
        if (item == null) return;
        SelectedPlanting = item;
        IsDetailOpen = true;
    }

    private void ExecuteOpenAddForm()
    {
        _formPlantingId = 0;
        FormPlantingCode = $"PLN-{DateTime.Now:yyyyMMdd}-{Plantings.Count + 1:D2}";
        FormSeasonName = string.Empty;
        FormSelectedLand = AvailableLands.FirstOrDefault();
        FormSelectedCommodity = AvailableCommodities.FirstOrDefault();
        FormStartDate = DateTime.Today;
        FormExpectedHarvestDate = FormSelectedCommodity != null
            ? FormStartDate.AddDays(FormSelectedCommodity.EstimatedGrowthDays)
            : FormStartDate.AddDays(90);
        FormPlantedAreaHectares = FormSelectedLand != null ? Math.Min(2.0m, FormSelectedLand.AreaHectares) : 1.0m;
        FormTargetYield = 5000m;
        FormStatus = PlantingStatus.Planned;
        FormNotes = string.Empty;
        FormValidationError = null;

        IsEditMode = false;
        IsFormOpen = true;
    }

    private void ExecuteOpenEditForm(Planting? item)
    {
        if (item == null) return;

        _formPlantingId = item.PlantingId;
        FormPlantingCode = item.PlantingCode;
        FormSeasonName = item.SeasonName;
        FormSelectedLand = AvailableLands.FirstOrDefault(l => l.LandId == item.LandId);
        FormSelectedCommodity = AvailableCommodities.FirstOrDefault(c => c.CommodityId == item.CommodityId);
        FormStartDate = item.StartDate;
        FormExpectedHarvestDate = item.ExpectedHarvestDate;
        FormPlantedAreaHectares = item.PlantedAreaHectares;
        FormTargetYield = item.TargetYield;
        FormStatus = item.Status;
        FormNotes = item.Notes;
        FormValidationError = null;

        IsEditMode = true;
        IsFormOpen = true;
    }

    private async Task ExecuteSaveAsync()
    {
        FormValidationError = null;

        if (FormSelectedLand == null)
        {
            FormValidationError = "Lahan pertanian wajib dipilih.";
            return;
        }

        if (FormSelectedCommodity == null)
        {
            FormValidationError = "Komoditas pertanian wajib dipilih.";
            return;
        }

        var planting = new Planting
        {
            PlantingId = _formPlantingId,
            PlantingCode = FormPlantingCode,
            LandId = FormSelectedLand.LandId,
            CommodityId = FormSelectedCommodity.CommodityId,
            SeasonName = FormSeasonName,
            StartDate = FormStartDate,
            ExpectedHarvestDate = FormExpectedHarvestDate,
            PlantedAreaHectares = FormPlantedAreaHectares,
            TargetYield = FormTargetYield,
            Status = FormStatus,
            Notes = FormNotes
        };

        if (IsEditMode)
        {
            var result = await _plantingService.UpdatePlantingAsync(planting);
            if (result.IsSuccess)
            {
                IsFormOpen = false;
                _dialogService.ShowInformation("Data musim tanam berhasil diperbarui.", "Berhasil");
                await LoadPlantingsAsync();
            }
            else
            {
                FormValidationError = result.ErrorMessage;
            }
        }
        else
        {
            var result = await _plantingService.CreatePlantingAsync(planting);
            if (result.IsSuccess)
            {
                IsFormOpen = false;
                _dialogService.ShowInformation("Musim tanam baru berhasil didaftarkan.", "Berhasil");
                await LoadPlantingsAsync();
            }
            else
            {
                FormValidationError = result.ErrorMessage;
            }
        }
    }

    private async Task ExecuteChangeStatusAsync(PlantingStatus newStatus)
    {
        if (SelectedPlanting == null) return;

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin mengubah status penanaman '{SelectedPlanting.PlantingCode}' menjadi '{newStatus.ToDisplayString()}'?",
            "Konfirmasi Perubahan Status");

        if (confirm)
        {
            var result = await _plantingService.ChangeStatusAsync(SelectedPlanting.PlantingId, newStatus);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation($"Status penanaman berhasil diubah menjadi '{newStatus.ToDisplayString()}'.", "Berhasil");
                await LoadPlantingsAsync();
                SelectedPlanting = await _plantingService.GetPlantingByIdAsync(SelectedPlanting.PlantingId);
            }
            else
            {
                _dialogService.ShowWarning(result.ErrorMessage ?? "Gagal mengubah status.", "Peringatan");
            }
        }
    }

    private async Task ExecuteDeleteAsync(Planting? item)
    {
        if (item == null) return;

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin menghapus penanaman '{item.PlantingCode}' ({item.SeasonName})?",
            "Konfirmasi Hapus Penanaman");

        if (confirm)
        {
            var result = await _plantingService.DeletePlantingAsync(item.PlantingId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Data musim tanam berhasil dihapus.", "Berhasil");
                if (SelectedPlanting?.PlantingId == item.PlantingId)
                    IsDetailOpen = false;
                await LoadPlantingsAsync();
            }
            else
            {
                _dialogService.ShowWarning(result.ErrorMessage ?? "Gagal menghapus penanaman.", "Peringatan");
            }
        }
    }
}
