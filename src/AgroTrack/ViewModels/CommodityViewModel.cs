using System.Collections.ObjectModel;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Models;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class CommodityViewModel : ViewModelBase
{
    private readonly ICommodityService _commodityService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    private string _searchTerm = string.Empty;
    private string _selectedStatusFilter = "Semua";

    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalCount = 0;
    private const int PageSize = 8;

    private bool _isFormOpen;
    private bool _isEditMode;

    // Form fields
    private int _formCommodityId;
    private string _formCommodityCode = string.Empty;
    private string _formCommodityName = string.Empty;
    private string _formResultUnit = "kg";
    private int _formEstimatedGrowthDays = 90;
    private string? _formDescription;
    private bool _formIsActive = true;
    private string? _formValidationError;

    public CommodityViewModel(
        ICommodityService commodityService,
        IDialogService dialogService,
        ISessionService sessionService)
    {
        _commodityService = commodityService;
        _dialogService = dialogService;
        _sessionService = sessionService;

        Title = "Manajemen Komoditas Pertanian";

        Commodities = new ObservableCollection<Commodity>();
        StatusFilterOptions = new ObservableCollection<string> { "Semua", "Aktif", "Nonaktif" };
        UnitOptions = new ObservableCollection<string> { "kg", "ton", "kuintal", "ikat", "buah" };

        LoadCommoditiesCommand = new AsyncRelayCommand(LoadCommoditiesAsync);
        SearchCommand = new AsyncRelayCommand(() => { _currentPage = 1; return LoadCommoditiesAsync(); });
        ResetFilterCommand = new AsyncRelayCommand(ExecuteResetFilterAsync);
        NextPageCommand = new AsyncRelayCommand(ExecuteNextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(ExecutePreviousPageAsync, () => HasPreviousPage);

        OpenAddFormCommand = new RelayCommand(ExecuteOpenAddForm, () => CanManage);
        OpenEditFormCommand = new RelayCommand<Commodity>(ExecuteOpenEditForm, _ => CanManage);
        SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
        CancelFormCommand = new RelayCommand(() => IsFormOpen = false);
        ToggleActiveCommand = new AsyncRelayCommand<Commodity>(ExecuteToggleActiveAsync, _ => CanManage);
        DeleteCommand = new AsyncRelayCommand<Commodity>(ExecuteDeleteAsync, _ => CanManage);

        _ = LoadCommoditiesAsync();
    }

    public ObservableCollection<Commodity> Commodities { get; }
    public ObservableCollection<string> StatusFilterOptions { get; }
    public ObservableCollection<string> UnitOptions { get; }

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
                _ = LoadCommoditiesAsync();
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

    public string FormTitle => IsEditMode ? "Ubah Data Komoditas" : "Tambah Komoditas Baru";

    public string FormCommodityCode
    {
        get => _formCommodityCode;
        set => SetProperty(ref _formCommodityCode, value);
    }

    public string FormCommodityName
    {
        get => _formCommodityName;
        set => SetProperty(ref _formCommodityName, value);
    }

    public string FormResultUnit
    {
        get => _formResultUnit;
        set => SetProperty(ref _formResultUnit, value);
    }

    public int FormEstimatedGrowthDays
    {
        get => _formEstimatedGrowthDays;
        set => SetProperty(ref _formEstimatedGrowthDays, value);
    }

    public string? FormDescription
    {
        get => _formDescription;
        set => SetProperty(ref _formDescription, value);
    }

    public bool FormIsActive
    {
        get => _formIsActive;
        set => SetProperty(ref _formIsActive, value);
    }

    public string? FormValidationError
    {
        get => _formValidationError;
        set => SetProperty(ref _formValidationError, value);
    }

    // Commands
    public ICommand LoadCommoditiesCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    public ICommand DeleteCommand { get; }

    public async Task LoadCommoditiesAsync()
    {
        IsLoading = true;
        ClearError();

        try
        {
            bool? activeFilter = SelectedStatusFilter switch
            {
                "Aktif" => true,
                "Nonaktif" => false,
                _ => null
            };

            var paged = await _commodityService.GetCommoditiesPagedAsync(
                SearchTerm, 
                activeFilter, 
                CurrentPage, 
                PageSize);

            Commodities.Clear();
            foreach (var item in paged.Items)
            {
                Commodities.Add(item);
            }

            TotalCount = paged.TotalCount;
            TotalPages = Math.Max(1, paged.TotalPages);
            IsEmpty = Commodities.Count == 0;
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat data komoditas: {ex.Message}");
            Commodities.Clear();
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
        _currentPage = 1;

        OnPropertyChanged(nameof(SearchTerm));
        OnPropertyChanged(nameof(SelectedStatusFilter));

        await LoadCommoditiesAsync();
    }

    private async Task ExecuteNextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadCommoditiesAsync();
        }
    }

    private async Task ExecutePreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadCommoditiesAsync();
        }
    }

    private void ExecuteOpenAddForm()
    {
        _formCommodityId = 0;
        FormCommodityCode = $"KMD-{DateTime.Now:yyyyMMdd}-{Commodities.Count + 1:D2}";
        FormCommodityName = string.Empty;
        FormResultUnit = "kg";
        FormEstimatedGrowthDays = 90;
        FormDescription = string.Empty;
        FormIsActive = true;
        FormValidationError = null;

        IsEditMode = false;
        IsFormOpen = true;
    }

    private void ExecuteOpenEditForm(Commodity? item)
    {
        if (item == null) return;

        _formCommodityId = item.CommodityId;
        FormCommodityCode = item.CommodityCode;
        FormCommodityName = item.CommodityName;
        FormResultUnit = item.ResultUnit;
        FormEstimatedGrowthDays = item.EstimatedGrowthDays;
        FormDescription = item.Description;
        FormIsActive = item.IsActive;
        FormValidationError = null;

        IsEditMode = true;
        IsFormOpen = true;
    }

    private async Task ExecuteSaveAsync()
    {
        FormValidationError = null;

        var commodity = new Commodity
        {
            CommodityId = _formCommodityId,
            CommodityCode = FormCommodityCode,
            CommodityName = FormCommodityName,
            ResultUnit = FormResultUnit,
            EstimatedGrowthDays = FormEstimatedGrowthDays,
            Description = FormDescription,
            IsActive = FormIsActive
        };

        if (IsEditMode)
        {
            var result = await _commodityService.UpdateCommodityAsync(commodity);
            if (result.IsSuccess)
            {
                IsFormOpen = false;
                _dialogService.ShowInformation("Data komoditas berhasil diperbarui.", "Berhasil");
                await LoadCommoditiesAsync();
            }
            else
            {
                FormValidationError = result.ErrorMessage;
            }
        }
        else
        {
            var result = await _commodityService.CreateCommodityAsync(commodity);
            if (result.IsSuccess)
            {
                IsFormOpen = false;
                _dialogService.ShowInformation("Komoditas baru berhasil disimpan.", "Berhasil");
                await LoadCommoditiesAsync();
            }
            else
            {
                FormValidationError = result.ErrorMessage;
            }
        }
    }

    private async Task ExecuteToggleActiveAsync(Commodity? item)
    {
        if (item == null) return;

        var newStatus = !item.IsActive;
        var actionText = newStatus ? "mengaktifkan" : "menonaktifkan";

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin {actionText} komoditas '{item.CommodityName}'?", 
            "Ubah Status Komoditas");

        if (confirm)
        {
            var result = await _commodityService.SetCommodityActiveStatusAsync(item.CommodityId, newStatus);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation($"Status komoditas berhasil diubah menjadi {(newStatus ? "Aktif" : "Nonaktif")}.", "Berhasil");
                await LoadCommoditiesAsync();
            }
            else
            {
                _dialogService.ShowWarning(result.ErrorMessage ?? "Gagal mengubah status komoditas.", "Peringatan");
            }
        }
    }

    private async Task ExecuteDeleteAsync(Commodity? item)
    {
        if (item == null) return;

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin menghapus komoditas '{item.CommodityName}' ({item.CommodityCode})?", 
            "Konfirmasi Hapus Komoditas");

        if (confirm)
        {
            var result = await _commodityService.DeleteCommodityAsync(item.CommodityId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Data komoditas berhasil dihapus.", "Berhasil");
                await LoadCommoditiesAsync();
            }
            else
            {
                _dialogService.ShowWarning(result.ErrorMessage ?? "Gagal menghapus komoditas.", "Peringatan");
            }
        }
    }
}
