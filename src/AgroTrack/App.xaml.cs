using System.IO;
using System.Windows;
using AgroTrack.Data;
using AgroTrack.Repositories.Implementations;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Services.Interfaces;
using AgroTrack.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroTrack;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
        // Global exception handling
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        // Wire navigation events
        var navService = _serviceProvider.GetRequiredService<INavigationService>();
        var sessionService = _serviceProvider.GetRequiredService<ISessionService>();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainViewModel;

        navService.NavigateToLoginRequested += () =>
        {
            var loginVm = _serviceProvider.GetRequiredService<LoginViewModel>();
            mainViewModel.CurrentContentViewModel = loginVm;
        };

        navService.NavigateToMainRequested += () =>
        {
            mainViewModel.ExecuteNavigate("Dashboard");
        };

        // Start with Login or Dashboard depending on session
        if (sessionService.IsAuthenticated)
        {
            mainViewModel.ExecuteNavigate("Dashboard");
        }
        else
        {
            var loginVm = _serviceProvider.GetRequiredService<LoginViewModel>();
            mainViewModel.CurrentContentViewModel = loginVm;
        }

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Configuration
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        // 2. Data & Security
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDialogService, DialogService>();

        // 3. Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // 4. Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILandRepository, LandRepository>();
        services.AddScoped<ICommodityRepository, CommodityRepository>();
        services.AddScoped<IPlantingRepository, PlantingRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ICostRepository, CostRepository>();
        services.AddScoped<IHarvestRepository, HarvestRepository>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        // 5. Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ILandService, LandService>();
        services.AddScoped<ICommodityService, CommodityService>();
        services.AddScoped<IPlantingService, PlantingService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<ICostService, CostService>();
        services.AddScoped<IHarvestService, HarvestService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();

        // 6. ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<LandViewModel>();
        services.AddTransient<CommodityViewModel>();
        services.AddTransient<PlantingViewModel>();
        services.AddTransient<ActivityViewModel>();
        services.AddTransient<MaterialViewModel>();
        services.AddTransient<CostViewModel>();
        services.AddTransient<HarvestViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<UserManagementViewModel>();

        // 7. Views
        services.AddTransient<MainWindow>();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show($"Terjadi kesalahan sistem yang tidak terduga:\n{e.Exception.Message}",
            "Kesalahan Aplikasi", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"Kesalahan fatal sistem:\n{ex.Message}",
                "Kesalahan Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
