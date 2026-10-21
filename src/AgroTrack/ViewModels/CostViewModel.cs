using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class CostViewModel : ViewModelBase
{
    private readonly ICostService _costService;
    private readonly IPlantingService _plantingService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;

    // Filters
    private string _searchTerm = string.Empty;
    private int? _selectedPlantingId;
    private CostCategory? _selectedCategoryFilter;
    private DateTime? _startDateFilter;
    private DateTime? _endDateFilter;

    // Pagination
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalPages = 1;
    private int _totalItems = 0;
    private decimal _grandTotalCost = 0m;

    // Form Modal
    private bool _isFormOpen;
    private bool _isEditing;
    private int _editingCostId;
    private int _formPlantingId;
    private CostCategory _formCategory = CostCategory.Labor;
    private DateTime _formCostDate = DateTime.Today;
    private decimal _formAmount;
    private string _formDescription = string.Empty;

    // Collections
    private ObservableCollection<OperationalCost> _costs = new();
    private ObservableCollection<Planting> _plantings = new();

    public CostViewModel(
        ICostService costService,
        IPlantingService plantingService,
        ISessionService sessionService,
        IDialogService dialogService)
    {
        _costService = costService;
        _plantingService = plantingService;
        _sessionService = sessionService;
        _dialogService = dialogService;

        Title = "Biaya Operasional";

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
            SelectedCategoryFilter = null;
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
            if (param is OperationalCost cost)
                OpenEditForm(cost);
        });
        SaveFormCommand = new AsyncRelayCommand(async () => await SaveFormAsync(), () => !IsLoading);
        CancelFormCommand = new RelayCommand(_ => IsFormOpen = false);

        DeleteCommand = new AsyncRelayCommand<OperationalCost>(async cost =>
        {
            if (cost != null)
                await DeleteCostAsync(cost);
        }, _ => CanDelete);

        _ = LoadLookupsAndDataAsync();
    }

    public ObservableCollection<OperationalCost> Costs
    {
        get => _costs;
        set => SetProperty(ref _costs, value);
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

    public CostCategory? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set => SetProperty(ref _selectedCategoryFilter, value);
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

    public decimal GrandTotalCost
    {
        get => _grandTotalCost;
        set => SetProperty(ref _grandTotalCost, value);
    }

    public string GrandTotalCostDisplay =>
        string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", GrandTotalCost);

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

    public string FormTitle => IsEditing ? "Ubah Biaya Operasional" : "Catat Biaya Operasional Baru";

    public int FormPlantingId
    {
        get => _formPlantingId;
        set => SetProperty(ref _formPlantingId, value);
    }

    public CostCategory FormCategory
    {
        get => _formCategory;
        set => SetProperty(ref _formCategory, value);
    }

    public DateTime FormCostDate
    {
        get => _formCostDate;
        set => SetProperty(ref _formCostDate, value);
    }

    public decimal FormAmount
    {
        get => _formAmount;
        set => SetProperty(ref _formAmount, value);
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

    public IEnumerable<CostCategory> CostCategories => Enum.GetValues<CostCategory>();

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
            var paged = await _costService.GetCostsPagedAsync(
                SelectedPlantingId,
                SelectedCategoryFilter,
                StartDateFilter,
                EndDateFilter,
                SearchTerm,
                CurrentPage,
                _pageSize);

            Costs = new ObservableCollection<OperationalCost>(paged.Items);
            TotalItems = paged.TotalCount;
            TotalPages = paged.TotalPages;
            IsEmpty = Costs.Count == 0;

            // Calculate total costs of current visible list or selected planting
            if (SelectedPlantingId.HasValue && SelectedPlantingId.Value > 0)
            {
                GrandTotalCost = await _costService.GetTotalCostByPlantingIdAsync(SelectedPlantingId.Value);
            }
            else
            {
                GrandTotalCost = Costs.Sum(c => c.Amount);
            }
            OnPropertyChanged(nameof(GrandTotalCostDisplay));

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat daftar biaya operasional: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenCreateForm()
    {
        IsEditing = false;
        _editingCostId = 0;
        FormPlantingId = Plantings.FirstOrDefault()?.PlantingId ?? 0;
        FormCategory = CostCategory.Labor;
        FormCostDate = DateTime.Today;
        FormAmount = 0m;
        FormDescription = string.Empty;

        IsFormOpen = true;
    }

    private void OpenEditForm(OperationalCost cost)
    {
        if (_sessionService.CurrentUser?.Role == UserRole.FieldOfficer &&
            cost.CreatedBy != _sessionService.CurrentUser.UserId)
        {
            _dialogService.ShowWarning("Anda hanya diizinkan untuk mengubah biaya yang Anda catat sendiri.");
            return;
        }

        IsEditing = true;
        _editingCostId = cost.CostId;
        FormPlantingId = cost.PlantingId;
        FormCategory = cost.Category;
        FormCostDate = cost.CostDate;
        FormAmount = cost.Amount;
        FormDescription = cost.Description;

        IsFormOpen = true;
    }

    private async Task SaveFormAsync()
    {
        if (FormPlantingId <= 0)
        {
            _dialogService.ShowWarning("Silakan pilih penanaman.");
            return;
        }

        if (FormAmount <= 0)
        {
            _dialogService.ShowWarning("Jumlah biaya harus lebih dari nol rupiah.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FormDescription))
        {
            _dialogService.ShowWarning("Deskripsi biaya operasional wajib diisi.");
            return;
        }

        IsLoading = true;
        try
        {
            var cost = new OperationalCost
            {
                CostId = _editingCostId,
                PlantingId = FormPlantingId,
                Category = FormCategory,
                CostDate = FormCostDate,
                Amount = FormAmount,
                Description = FormDescription
            };

            if (IsEditing)
            {
                var result = await _costService.UpdateCostAsync(cost);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Biaya operasional berhasil diperbarui.");
                    IsFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal memperbarui biaya operasional.");
                }
            }
            else
            {
                var result = await _costService.CreateCostAsync(cost);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Biaya operasional berhasil dicatat.");
                    IsFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal mencatat biaya operasional.");
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

    private async Task DeleteCostAsync(OperationalCost cost)
    {
        if (!CanDelete)
        {
            _dialogService.ShowWarning("Akun Anda tidak memiliki hak akses untuk menghapus data biaya.");
            return;
        }

        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin menghapus biaya '{cost.CategoryDisplay}' sebesar {cost.AmountDisplay} pada penanaman {cost.PlantingCode}?\n\nTindakan ini tidak dapat dibatalkan.",
            "Konfirmasi Hapus Biaya");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _costService.DeleteCostAsync(cost.CostId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Biaya operasional berhasil dihapus.");
                await LoadDataAsync();
            }
            else
            {
                _dialogService.ShowError(result.ErrorMessage ?? "Gagal menghapus biaya operasional.");
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
