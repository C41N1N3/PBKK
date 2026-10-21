using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class MaterialViewModel : ViewModelBase
{
    private readonly IMaterialService _materialService;
    private readonly IActivityService _activityService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;

    // View Tabs
    private int _selectedTabIndex = 0; // 0 = Master Bahan, 1 = Riwayat Penggunaan

    // Material Filters
    private string _searchTerm = string.Empty;
    private MaterialCategory? _selectedCategoryFilter;
    private bool? _isLowStockFilter;

    // Pagination
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalPages = 1;
    private int _totalItems = 0;

    // Material Modal Form
    private bool _isMaterialFormOpen;
    private bool _isEditingMaterial;
    private int _editingMaterialId;
    private string _formMaterialCode = string.Empty;
    private string _formMaterialName = string.Empty;
    private MaterialCategory _formCategory = MaterialCategory.Fertilizer;
    private string _formUnit = "kg";
    private decimal _formStock;
    private decimal _formMinimumStock;
    private decimal _formUnitPrice;
    private bool _formIsActive = true;

    // Usage Modal Form
    private bool _isUsageFormOpen;
    private int _formUsageActivityId;
    private int _formUsageMaterialId;
    private decimal _formUsageQuantity;
    private decimal _formUsageUnitPrice;

    // Collections
    private ObservableCollection<AgriculturalMaterial> _materials = new();
    private ObservableCollection<MaterialUsage> _usages = new();
    private ObservableCollection<FarmingActivity> _activities = new();

    public MaterialViewModel(
        IMaterialService materialService,
        IActivityService activityService,
        ISessionService sessionService,
        IDialogService dialogService)
    {
        _materialService = materialService;
        _activityService = activityService;
        _sessionService = sessionService;
        _dialogService = dialogService;

        Title = "Bahan Pertanian & Pengelolaan Stok";

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
            SelectedCategoryFilter = null;
            IsLowStockFilter = null;
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

        // Material Form Commands
        OpenCreateMaterialCommand = new RelayCommand(_ => OpenCreateMaterial());
        OpenEditMaterialCommand = new RelayCommand(param =>
        {
            if (param is AgriculturalMaterial m)
                OpenEditMaterial(m);
        });
        SaveMaterialCommand = new AsyncRelayCommand(async () => await SaveMaterialAsync(), () => !IsLoading);
        CancelMaterialCommand = new RelayCommand(_ => IsMaterialFormOpen = false);

        // Usage Form Commands
        OpenRecordUsageCommand = new RelayCommand(_ => OpenRecordUsage());
        SaveUsageCommand = new AsyncRelayCommand(async () => await SaveUsageAsync(), () => !IsLoading);
        CancelUsageCommand = new RelayCommand(_ => IsUsageFormOpen = false);
        CancelUsageTransactionCommand = new AsyncRelayCommand<MaterialUsage>(async u =>
        {
            if (u != null)
                await CancelUsageAsync(u);
        });

        _ = LoadLookupsAndDataAsync();
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                _currentPage = 1;
                _ = LoadDataAsync();
            }
        }
    }

    public ObservableCollection<AgriculturalMaterial> Materials
    {
        get => _materials;
        set => SetProperty(ref _materials, value);
    }

    public ObservableCollection<MaterialUsage> Usages
    {
        get => _usages;
        set => SetProperty(ref _usages, value);
    }

    public ObservableCollection<FarmingActivity> Activities
    {
        get => _activities;
        set => SetProperty(ref _activities, value);
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public MaterialCategory? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set => SetProperty(ref _selectedCategoryFilter, value);
    }

    public bool? IsLowStockFilter
    {
        get => _isLowStockFilter;
        set => SetProperty(ref _isLowStockFilter, value);
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

    public bool CanManageMaterials => _sessionService.CurrentUser?.Role == UserRole.Administrator ||
                                      _sessionService.CurrentUser?.Role == UserRole.Manager;

    // Material Form Properties
    public bool IsMaterialFormOpen
    {
        get => _isMaterialFormOpen;
        set => SetProperty(ref _isMaterialFormOpen, value);
    }

    public bool IsEditingMaterial
    {
        get => _isEditingMaterial;
        set => SetProperty(ref _isEditingMaterial, value);
    }

    public string MaterialFormTitle => IsEditingMaterial ? "Ubah Bahan Pertanian" : "Tambah Bahan Pertanian Baru";

    public string FormMaterialCode
    {
        get => _formMaterialCode;
        set => SetProperty(ref _formMaterialCode, value);
    }

    public string FormMaterialName
    {
        get => _formMaterialName;
        set => SetProperty(ref _formMaterialName, value);
    }

    public MaterialCategory FormCategory
    {
        get => _formCategory;
        set => SetProperty(ref _formCategory, value);
    }

    public string FormUnit
    {
        get => _formUnit;
        set => SetProperty(ref _formUnit, value);
    }

    public decimal FormStock
    {
        get => _formStock;
        set => SetProperty(ref _formStock, value);
    }

    public decimal FormMinimumStock
    {
        get => _formMinimumStock;
        set => SetProperty(ref _formMinimumStock, value);
    }

    public decimal FormUnitPrice
    {
        get => _formUnitPrice;
        set => SetProperty(ref _formUnitPrice, value);
    }

    public bool FormIsActive
    {
        get => _formIsActive;
        set => SetProperty(ref _formIsActive, value);
    }

    // Usage Form Properties
    public bool IsUsageFormOpen
    {
        get => _isUsageFormOpen;
        set => SetProperty(ref _isUsageFormOpen, value);
    }

    public int FormUsageActivityId
    {
        get => _formUsageActivityId;
        set => SetProperty(ref _formUsageActivityId, value);
    }

    public int FormUsageMaterialId
    {
        get => _formUsageMaterialId;
        set
        {
            if (SetProperty(ref _formUsageMaterialId, value))
            {
                var mat = Materials.FirstOrDefault(m => m.MaterialId == value);
                if (mat != null)
                {
                    FormUsageUnitPrice = mat.UnitPrice;
                }
                OnPropertyChanged(nameof(FormUsageSubtotalDisplay));
            }
        }
    }

    public decimal FormUsageQuantity
    {
        get => _formUsageQuantity;
        set
        {
            if (SetProperty(ref _formUsageQuantity, value))
            {
                OnPropertyChanged(nameof(FormUsageSubtotalDisplay));
            }
        }
    }

    public decimal FormUsageUnitPrice
    {
        get => _formUsageUnitPrice;
        set
        {
            if (SetProperty(ref _formUsageUnitPrice, value))
            {
                OnPropertyChanged(nameof(FormUsageSubtotalDisplay));
            }
        }
    }

    public string FormUsageSubtotalDisplay =>
        string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", FormUsageQuantity * FormUsageUnitPrice);

    // Commands
    public ICommand LoadDataCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand OpenCreateMaterialCommand { get; }
    public ICommand OpenEditMaterialCommand { get; }
    public ICommand SaveMaterialCommand { get; }
    public ICommand CancelMaterialCommand { get; }
    public ICommand OpenRecordUsageCommand { get; }
    public ICommand SaveUsageCommand { get; }
    public ICommand CancelUsageCommand { get; }
    public ICommand CancelUsageTransactionCommand { get; }

    public IEnumerable<MaterialCategory> MaterialCategories => Enum.GetValues<MaterialCategory>();

    private async Task LoadLookupsAndDataAsync()
    {
        try
        {
            var actsPaged = await _activityService.GetActivitiesPagedAsync(null, null, null, null, null, null, null, 1, 50);
            Activities = new ObservableCollection<FarmingActivity>(actsPaged.Items);
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
            if (SelectedTabIndex == 0)
            {
                var paged = await _materialService.GetMaterialsPagedAsync(
                    SearchTerm,
                    SelectedCategoryFilter,
                    IsLowStockFilter,
                    null,
                    CurrentPage,
                    _pageSize);

                Materials = new ObservableCollection<AgriculturalMaterial>(paged.Items);
                TotalItems = paged.TotalCount;
                TotalPages = paged.TotalPages;
                IsEmpty = Materials.Count == 0;
            }
            else
            {
                var pagedUsages = await _materialService.GetUsagesPagedAsync(null, null, CurrentPage, _pageSize);
                Usages = new ObservableCollection<MaterialUsage>(pagedUsages.Items);
                TotalItems = pagedUsages.TotalCount;
                TotalPages = pagedUsages.TotalPages;
                IsEmpty = Usages.Count == 0;
            }

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat data bahan atau penggunaan: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenCreateMaterial()
    {
        if (!CanManageMaterials)
        {
            _dialogService.ShowWarning("Akun Anda tidak memiliki hak akses untuk menambah bahan.");
            return;
        }

        IsEditingMaterial = false;
        _editingMaterialId = 0;
        FormMaterialCode = string.Empty;
        FormMaterialName = string.Empty;
        FormCategory = MaterialCategory.Fertilizer;
        FormUnit = "kg";
        FormStock = 0m;
        FormMinimumStock = 10m;
        FormUnitPrice = 0m;
        FormIsActive = true;

        IsMaterialFormOpen = true;
    }

    private void OpenEditMaterial(AgriculturalMaterial m)
    {
        if (!CanManageMaterials)
        {
            _dialogService.ShowWarning("Akun Anda tidak memiliki hak akses untuk mengubah bahan.");
            return;
        }

        IsEditingMaterial = true;
        _editingMaterialId = m.MaterialId;
        FormMaterialCode = m.MaterialCode;
        FormMaterialName = m.MaterialName;
        FormCategory = m.Category;
        FormUnit = m.Unit;
        FormStock = m.Stock;
        FormMinimumStock = m.MinimumStock;
        FormUnitPrice = m.UnitPrice;
        FormIsActive = m.IsActive;

        IsMaterialFormOpen = true;
    }

    private async Task SaveMaterialAsync()
    {
        if (string.IsNullOrWhiteSpace(FormMaterialCode))
        {
            _dialogService.ShowWarning("Kode bahan wajib diisi.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FormMaterialName))
        {
            _dialogService.ShowWarning("Nama bahan wajib diisi.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FormUnit))
        {
            _dialogService.ShowWarning("Satuan bahan wajib diisi.");
            return;
        }

        if (FormStock < 0)
        {
            _dialogService.ShowWarning("Stok tidak boleh bernilai negatif.");
            return;
        }

        IsLoading = true;
        try
        {
            var mat = new AgriculturalMaterial
            {
                MaterialId = _editingMaterialId,
                MaterialCode = FormMaterialCode,
                MaterialName = FormMaterialName,
                Category = FormCategory,
                Unit = FormUnit,
                Stock = FormStock,
                MinimumStock = FormMinimumStock,
                UnitPrice = FormUnitPrice,
                IsActive = FormIsActive
            };

            if (IsEditingMaterial)
            {
                var result = await _materialService.UpdateMaterialAsync(mat);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Data bahan berhasil diperbarui.");
                    IsMaterialFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal memperbarui bahan.");
                }
            }
            else
            {
                var result = await _materialService.CreateMaterialAsync(mat);
                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Bahan baru berhasil ditambahkan.");
                    IsMaterialFormOpen = false;
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.ErrorMessage ?? "Gagal menambahkan bahan.");
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

    private void OpenRecordUsage()
    {
        FormUsageActivityId = Activities.FirstOrDefault()?.ActivityId ?? 0;
        var firstMat = Materials.FirstOrDefault(m => m.IsActive);
        FormUsageMaterialId = firstMat?.MaterialId ?? 0;
        FormUsageUnitPrice = firstMat?.UnitPrice ?? 0m;
        FormUsageQuantity = 1m;

        IsUsageFormOpen = true;
    }

    private async Task SaveUsageAsync()
    {
        if (FormUsageActivityId <= 0)
        {
            _dialogService.ShowWarning("Silakan pilih aktivitas lapangan.");
            return;
        }

        if (FormUsageMaterialId <= 0)
        {
            _dialogService.ShowWarning("Silakan pilih bahan pertanian.");
            return;
        }

        if (FormUsageQuantity <= 0)
        {
            _dialogService.ShowWarning("Jumlah pemakaian harus lebih dari nol.");
            return;
        }

        IsLoading = true;
        try
        {
            var usage = new MaterialUsage
            {
                ActivityId = FormUsageActivityId,
                MaterialId = FormUsageMaterialId,
                Quantity = FormUsageQuantity,
                PriceAtUsage = FormUsageUnitPrice
            };

            var result = await _materialService.RecordMaterialUsageAsync(usage);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Penggunaan bahan berhasil dicatat dan stok berhasil dikurangi secara otomatis.");
                IsUsageFormOpen = false;
                await LoadDataAsync();
            }
            else
            {
                _dialogService.ShowError(result.ErrorMessage ?? "Gagal mencatat penggunaan bahan.");
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

    private async Task CancelUsageAsync(MaterialUsage usage)
    {
        var confirm = _dialogService.ShowConfirmation(
            $"Apakah Anda yakin ingin membatalkan penggunaan bahan '{usage.MaterialName}' sebanyak {usage.QuantityDisplay}?\n\nStok akan dikembalikan secara otomatis ke persediaan gudang.",
            "Konfirmasi Pembatalan Penggunaan Bahan");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _materialService.CancelMaterialUsageAsync(usage.MaterialUsageId);
            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Penggunaan bahan dibatalkan dan stok telah dikembalikan ke persediaan.");
                await LoadDataAsync();
            }
            else
            {
                _dialogService.ShowError(result.ErrorMessage ?? "Gagal membatalkan penggunaan bahan.");
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
}
