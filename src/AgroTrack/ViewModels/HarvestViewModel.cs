using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class HarvestViewModel : ViewModelBase
{
    private readonly IHarvestService _harvestService;
    private readonly IPlantingService _plantingService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;

    // Filters
    private string _searchTerm = string.Empty;
    private int? _selectedPlantingId;
    private HarvestQuality? _selectedQualityFilter;
    private DateTime? _startDateFilter;
    private DateTime? _endDateFilter;

    // Pagination & Totals
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalPages = 1;
    private int _totalItems = 0;
    private decimal _grandTotalYield = 0m;
    private decimal _grandTotalRevenue = 0m;

    // Form Modal
    private bool _isFormOpen;
    private bool _isEditing;
    private int _editingHarvestId;
    private int _formPlantingId;
    private DateTime _formHarvestDate = DateTime.Today;
    private decimal _formYieldAmount;
    private HarvestQuality _formQuality = HarvestQuality.GradeA;
    private decimal _formSellingPricePerUnit;
    private string? _formNotes;

    // Collections
    private ObservableCollection<Harvest> _harvests = new();
    private ObservableCollection<Planting> _plantings = new();

    public HarvestViewModel(
        IHarvestService harvestService,
        IPlantingService plantingService,
        ISessionService sessionService,
        IDialogService dialogService)
    {
        _harvestService = harvestService;
        _plantingService = plantingService;
        _sessionService = sessionService;
        _dialogService = dialogService;

        Title = "Pencatatan Hasil Panen";

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
            SelectedQualityFilter = null;
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
            if (param is Harvest harvest)
                OpenEditForm(harvest);
        });
        SaveFormCommand = new AsyncRelayCommand(async () => await SaveFormAsync(), () => !IsLoading);
        CancelFormCommand = new RelayCommand(_ => IsFormOpen = false);

        DeleteCommand = new AsyncRelayCommand<Harvest>(async harvest =>
        {
            if (harvest != null)
                await DeleteHarvestAsync(harvest);
        }, _ => CanDelete);

        _ = LoadLookupsAndDataAsync();
    }

    public ObservableCollection<Harvest> Harvests
    {
        get => _harvests;
        set => SetProperty(ref _harvests, value);
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

    public HarvestQuality? SelectedQualityFilter
    {
        get => _selectedQualityFilter;
        set => SetProperty(ref _selectedQualityFilter, value);
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

    public decimal GrandTotalYield
    {
        get => _grandTotalYield;
        set => SetProperty(ref _grandTotalYield, value);
    }

    public decimal GrandTotalRevenue
    {
        get => _grandTotalRevenue;
        set => SetProperty(ref _grandTotalRevenue, value);
    }

    public string GrandTotalRevenueDisplay =>
        string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", GrandTotalRevenue);

    public string GrandTotalYieldDisplay => $"{GrandTotalYield:N0} kg";

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

    public string FormTitle => IsEditing ? "Ubah Hasil Panen" : "Catat Hasil Panen Baru";

    public int FormPlantingId
    {
        get => _formPlantingId;
        set => SetProperty(ref _formPlantingId, value);
    }

    public DateTime FormHarvestDate
    {
        get => _formHarvestDate;
        set => SetProperty(ref _formHarvestDate, value);
    }

    public decimal FormYieldAmount
    {
        get => _formYieldAmount;
        set
        {
            if (SetProperty(ref _formYieldAmount, value))
            {
                OnPropertyChanged(nameof(FormTotalRevenueDisplay));
            }
        }
    }

    public HarvestQuality FormQuality
    {
        get => _formQuality;
        set => SetProperty(ref _formQuality, value);
    }

    public decimal FormSellingPricePerUnit
    {
        get => _formSellingPricePerUnit;
        set
        {
            if (SetProperty(ref _formSellingPricePerUnit, value))
            {
                OnPropertyChanged(nameof(FormTotalRevenueDisplay));
            }
        }
    }

    public string FormTotalRevenueDisplay =>
        string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", FormYieldAmount * FormSellingPricePerUnit);

    public string? FormNotes
    {
        get => _formNotes;
        set => SetProperty(ref _formNotes, value);
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

    public IEnumerable<HarvestQuality> HarvestQualities => Enum.GetValues<HarvestQuality>();

    private async Task LoadLookupsAndDataAsync()
    {
        try
        {
            var pList = await _plantingService.GetActivePlantingsAsync();
            Plantings = new ObservableCollection<Planting>(pList);
        }
        catch
        {
            // fallback
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
            var paged = await _harvestService.GetHarvestsPagedAsync(
                SelectedPlantingId,
                SelectedQualityFilter,
                StartDateFilter,
                EndDateFilter,
                SearchTerm,
                CurrentPage,
                _pageSize);

            Harvests = new ObservableCollection<Harvest>(paged.Items);
            TotalItems = paged.TotalCount;
            TotalPages = paged.TotalPages;
            IsEmpty = Harvests.Count == 0;

            if (SelectedPlantingId.HasValue && SelectedPlantingId.Value > 0)
            {
                GrandTotalYield = await _harvestService.GetTotalYieldByPlantingIdAsync(SelectedPlantingId.Value);
                GrandTotalRevenue = await _harvestService.GetTotalRevenueByPlantingIdAsync(SelectedPlantingId.Value);
            }
            else
            {
                GrandTotalYield = Harvests.Sum(h => h.YieldAmount);
                GrandTotalRevenue = Harvests.Sum(h => h.TotalRevenue);
            }

            OnPropertyChanged(nameof(GrandTotalYieldDisplay));
            OnPropertyChanged(nameof(GrandTotalRevenueDisplay));
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat daftar hasil panen: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenCreateForm()
    {
        IsEditing = false;
        _editingHarvestId = 0;
        FormPlantingId = Plantings.FirstOrDefault()?.PlantingId ?? 0;
        FormHarvestDate = DateTime.Today;
        FormYieldAmount = 0m;
        FormQuality = HarvestQuality.GradeA;
        FormSellingPricePerUnit = 0m;
        FormNotes = string.Empty;

        IsFormOpen = true;
    }

    private void OpenEditForm(Harvest harvest)
    {
        if (_sessionService.CurrentUser?.Role == UserRole.FieldOfficer &&
            harvest.CreatedBy != _sessionService.CurrentUser.UserId)
        {
            _dialogService.ShowWarning("Anda hanya diizinkan untuk mengubah hasil panen yang Anda catat sendiri.");
            return;
        }

        IsEditing = true;
        _editingHarvestId = harvest.HarvestId;
        FormPlantingId = harvest.PlantingId;
        FormHarvestDate = harvest.HarvestDate;
        FormYieldAmount = harvest.YieldAmount;
        FormQuality = harvest.Quality;
        FormSellingPricePerUnit = harvest.SellingPricePerUnit;
        FormNotes = harvest.Notes;

        IsFormOpen = true;
    }

    private async Task SaveFormAsync()
    {
        if (FormPlantingId <= 0)
        {
            _dialogService.ShowWarning("Silakan pilih penanaman.");
            return;
        }

        if (FormYieldAmount <= 0)
        {
            _dialogService.ShowWarning("Jumlah hasil panen harus lebih dari nol.");
            return;
        }

        if (FormSellingPricePerUnit < 0)
        {
            _dialogService.ShowWarning("Harga jual tidak boleh bernilai negatif.");
            return;
        }

        IsLoading = true;
        try
        {
            var harvest = new Harvest
            {
                HarvestId = _editingHarvestId,
                PlantingId = FormPlantingId,
                HarvestDate = FormHarvestDate,
                YieldAmount = FormYieldAmount,
                Quality = FormQuality,
                SellingPricePerUnit = FormSellingPricePerUnit,
                Notes = FormNotes
            };

            if (IsEditing)
            {
                var result = await _harvestService.UpdateHarvestAsync(harvest);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Hasil panen berhasil diperbarui.");
                    IsFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal memperbarui hasil panen.");
                }
            }
            else
            {
                var result = await _harvestService.CreateHarvestAsync(harvest);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Hasil panen berhasil dicatat.");
                    IsFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal mencatat hasil panen.");
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

    private async Task DeleteHarvestAsync(Harvest harvest)
    {
        if (!CanDelete)
        {
            _dialogService.ShowWarning("Akun Anda tidak memiliki hak akses untuk menghapus data hasil panen.");
            return;
        }

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin menghapus panen sebesar {harvest.YieldDisplay} senilai {harvest.TotalRevenueDisplay} pada penanaman {harvest.PlantingCode}?\n\nTindakan ini tidak dapat dibatalkan.",
            "Konfirmasi Hapus Panen");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _harvestService.DeleteHarvestAsync(harvest.HarvestId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Hasil panen berhasil dihapus.");
                await LoadDataAsync();
            }
            else
            {
                _dialogService.ShowError(result.ErrorMessage ?? "Gagal menghapus hasil panen.");
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
