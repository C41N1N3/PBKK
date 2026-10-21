using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.DTOs;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class ReportViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly ILandService _landService;
    private readonly ICommodityService _commodityService;

    // Filters
    private DateTime? _startDateFrom;
    private DateTime? _startDateTo;
    private int? _selectedLandId;
    private int? _selectedCommodityId;
    private PlantingStatus? _selectedStatus;

    // Aggregates
    private decimal _totalTargetYield;
    private decimal _totalActualYield;
    private decimal _totalCosts;
    private decimal _totalRevenues;
    private decimal _totalProfit;

    // Collections
    private ObservableCollection<ProfitLossReportDto> _reports = new();
    private ObservableCollection<Land> _lands = new();
    private ObservableCollection<Commodity> _commodities = new();

    public ReportViewModel(
        IReportService reportService,
        ILandService landService,
        ICommodityService commodityService)
    {
        _reportService = reportService;
        _landService = landService;
        _commodityService = commodityService;

        Title = "Laporan Produksi & Keuntungan";

        FilterCommand = new AsyncRelayCommand(async () => await LoadReportDataAsync());
        ResetFilterCommand = new AsyncRelayCommand(async () =>
        {
            StartDateFrom = null;
            StartDateTo = null;
            SelectedLandId = null;
            SelectedCommodityId = null;
            SelectedStatus = null;
            await LoadReportDataAsync();
        });

        _ = LoadLookupsAndDataAsync();
    }

    public ObservableCollection<ProfitLossReportDto> Reports
    {
        get => _reports;
        set => SetProperty(ref _reports, value);
    }

    public ObservableCollection<Land> Lands
    {
        get => _lands;
        set => SetProperty(ref _lands, value);
    }

    public ObservableCollection<Commodity> Commodities
    {
        get => _commodities;
        set => SetProperty(ref _commodities, value);
    }

    public DateTime? StartDateFrom
    {
        get => _startDateFrom;
        set => SetProperty(ref _startDateFrom, value);
    }

    public DateTime? StartDateTo
    {
        get => _startDateTo;
        set => SetProperty(ref _startDateTo, value);
    }

    public int? SelectedLandId
    {
        get => _selectedLandId;
        set => SetProperty(ref _selectedLandId, value);
    }

    public int? SelectedCommodityId
    {
        get => _selectedCommodityId;
        set => SetProperty(ref _selectedCommodityId, value);
    }

    public PlantingStatus? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    // Aggregates
    public decimal TotalTargetYield
    {
        get => _totalTargetYield;
        set => SetProperty(ref _totalTargetYield, value);
    }

    public decimal TotalActualYield
    {
        get => _totalActualYield;
        set => SetProperty(ref _totalActualYield, value);
    }

    public decimal TotalCosts
    {
        get => _totalCosts;
        set => SetProperty(ref _totalCosts, value);
    }

    public decimal TotalRevenues
    {
        get => _totalRevenues;
        set => SetProperty(ref _totalRevenues, value);
    }

    public decimal TotalProfit
    {
        get => _totalProfit;
        set => SetProperty(ref _totalProfit, value);
    }

    public string TotalTargetYieldDisplay => $"{TotalTargetYield:N0} kg";
    public string TotalActualYieldDisplay => $"{TotalActualYield:N0} kg";
    public string TotalCostsDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalCosts);
    public string TotalRevenuesDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalRevenues);
    public string TotalProfitDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalProfit);

    public decimal OverallAchievementPercentage =>
        TotalTargetYield > 0 ? Math.Round((TotalActualYield / TotalTargetYield) * 100m, 1) : 0m;

    public string OverallAchievementDisplay =>
        TotalTargetYield > 0 ? $"{OverallAchievementPercentage:N1}%" : "-";

    public ICommand FilterCommand { get; }
    public ICommand ResetFilterCommand { get; }

    public IEnumerable<PlantingStatus> PlantingStatuses => Enum.GetValues<PlantingStatus>();

    private async Task LoadLookupsAndDataAsync()
    {
        try
        {
            var lTask = _landService.GetActiveLandsAsync();
            var cTask = _commodityService.GetActiveCommoditiesAsync();
            await Task.WhenAll(lTask, cTask);

            Lands = new ObservableCollection<Land>(await lTask);
            Commodities = new ObservableCollection<Commodity>(await cTask);
        }
        catch
        {
            // fallback
        }

        await LoadReportDataAsync();
    }

    public async Task LoadReportDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ClearError();

        try
        {
            var data = await _reportService.GetProfitLossReportAsync(
                StartDateFrom,
                StartDateTo,
                SelectedLandId,
                SelectedCommodityId,
                SelectedStatus);

            Reports = new ObservableCollection<ProfitLossReportDto>(data);

            TotalTargetYield = Reports.Sum(r => r.TargetYield);
            TotalActualYield = Reports.Sum(r => r.ActualYield);
            TotalCosts = Reports.Sum(r => r.TotalCost);
            TotalRevenues = Reports.Sum(r => r.TotalRevenue);
            TotalProfit = Reports.Sum(r => r.NetProfit);

            IsEmpty = Reports.Count == 0;

            OnPropertyChanged(nameof(TotalTargetYieldDisplay));
            OnPropertyChanged(nameof(TotalActualYieldDisplay));
            OnPropertyChanged(nameof(TotalCostsDisplay));
            OnPropertyChanged(nameof(TotalRevenuesDisplay));
            OnPropertyChanged(nameof(TotalProfitDisplay));
            OnPropertyChanged(nameof(OverallAchievementPercentage));
            OnPropertyChanged(nameof(OverallAchievementDisplay));
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat laporan produksi dan keuntungan: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
