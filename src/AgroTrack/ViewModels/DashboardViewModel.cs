using System.Collections.ObjectModel;
using System.Windows.Input;
using AgroTrack.Commands;
using AgroTrack.DTOs;
using AgroTrack.Models;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;

    private DashboardSummaryDto _summary = new();
    private ObservableCollection<TargetVsActualDto> _targetVsActuals = new();
    private ObservableCollection<MonthlyHarvestDto> _monthlyTrends = new();
    private ObservableCollection<CategoryCostDto> _categoryCosts = new();
    private ObservableCollection<CommodityDistributionDto> _commodityDistributions = new();
    private ObservableCollection<UpcomingHarvestDto> _upcomingHarvests = new();
    private ObservableCollection<FarmingActivity> _recentActivities = new();

    private string _selectedPeriod = "Semua Waktu";

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        Title = "Dashboard Produksi Pertanian";

        RefreshCommand = new AsyncRelayCommand(async () => await LoadDashboardDataAsync());
        ChangePeriodCommand = new AsyncRelayCommand<string>(async period =>
        {
            if (!string.IsNullOrWhiteSpace(period))
            {
                SelectedPeriod = period;
                await LoadDashboardDataAsync();
            }
        });

        _ = LoadDashboardDataAsync();
    }

    public DashboardSummaryDto Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public ObservableCollection<TargetVsActualDto> TargetVsActuals
    {
        get => _targetVsActuals;
        set => SetProperty(ref _targetVsActuals, value);
    }

    public ObservableCollection<MonthlyHarvestDto> MonthlyTrends
    {
        get => _monthlyTrends;
        set => SetProperty(ref _monthlyTrends, value);
    }

    public ObservableCollection<CategoryCostDto> CategoryCosts
    {
        get => _categoryCosts;
        set => SetProperty(ref _categoryCosts, value);
    }

    public ObservableCollection<CommodityDistributionDto> CommodityDistributions
    {
        get => _commodityDistributions;
        set => SetProperty(ref _commodityDistributions, value);
    }

    public ObservableCollection<UpcomingHarvestDto> UpcomingHarvests
    {
        get => _upcomingHarvests;
        set => SetProperty(ref _upcomingHarvests, value);
    }

    public ObservableCollection<FarmingActivity> RecentActivities
    {
        get => _recentActivities;
        set => SetProperty(ref _recentActivities, value);
    }

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set => SetProperty(ref _selectedPeriod, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ChangePeriodCommand { get; }

    public async Task LoadDashboardDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ClearError();

        try
        {
            DateTime? startDate = null;
            DateTime? endDate = DateTime.Today;

            if (SelectedPeriod == "Bulan Ini")
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }
            else if (SelectedPeriod == "3 Bulan Terakhir")
            {
                startDate = DateTime.Today.AddMonths(-3);
            }
            else if (SelectedPeriod == "Tahun Ini")
            {
                startDate = new DateTime(DateTime.Today.Year, 1, 1);
            }
            else
            {
                endDate = null;
            }

            // Load all aggregated database metrics in parallel
            var summaryTask = _dashboardService.GetSummaryAsync(startDate, endDate);
            var targetTask = _dashboardService.GetTargetVsActualAsync(6);
            var trendTask = _dashboardService.GetMonthlyHarvestTrendsAsync(6);
            var costTask = _dashboardService.GetCostByCategoryDistributionAsync(startDate, endDate);
            var commodityTask = _dashboardService.GetCommodityDistributionAsync();
            var upcomingTask = _dashboardService.GetUpcomingHarvestsAsync(5);
            var recentActivitiesTask = _dashboardService.GetRecentActivitiesAsync(5);

            await Task.WhenAll(
                summaryTask, targetTask, trendTask, costTask, commodityTask, upcomingTask, recentActivitiesTask);

            Summary = await summaryTask;
            TargetVsActuals = new ObservableCollection<TargetVsActualDto>(await targetTask);
            MonthlyTrends = new ObservableCollection<MonthlyHarvestDto>(await trendTask);
            CategoryCosts = new ObservableCollection<CategoryCostDto>(await costTask);
            CommodityDistributions = new ObservableCollection<CommodityDistributionDto>(await commodityTask);
            UpcomingHarvests = new ObservableCollection<UpcomingHarvestDto>(await upcomingTask);
            RecentActivities = new ObservableCollection<FarmingActivity>(await recentActivitiesTask);

            IsEmpty = Summary.TotalActiveLands == 0 && Summary.ActivePlantingsCount == 0;
        }
        catch (Exception ex)
        {
            SetError($"Gagal memuat statistik dashboard: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
