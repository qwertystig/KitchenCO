using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using YourKitchenCo.Services;
using YourKitchenCo.ViewModel;
using YourKitchenCo.Views;

namespace YourKitchenCo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 1. Services
        builder.Services.AddSingleton<IProductService, JsonProductService>();
        builder.Services.AddSingleton<ICartService, CartService>();
        builder.Services.AddSingleton<IOrderSchedulingService, OrderSchedulingService>();
        builder.Services.AddSingleton<ICycleMenuService, CycleMenuService>();
        builder.Services.AddSingleton<ICompanyDirectoryService, MockCompanyDirectoryService>();
        builder.Services.AddSingleton<IDiscountService, MockDiscountService>();
        builder.Services.AddSingleton<IUserDirectoryService, MockUserDirectoryService>();
        builder.Services.AddSingleton<IOrderService, MockOrderService>();
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();

        // 2. Shells
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AdminShell>();

        // 3. ViewModels
        builder.Services.AddTransient<UserDashboardViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<SelectDeliveryDayViewModel>();
        builder.Services.AddTransient<ActiveOrdersViewModel>();
        builder.Services.AddTransient<OrderHistoryViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<HelpViewModel>();
        builder.Services.AddTransient<ProductDetailViewModel>();
        builder.Services.AddTransient<CartPageViewModel>();
        builder.Services.AddTransient<PaymentViewModel>();
        builder.Services.AddTransient<TaxInvoiceViewModel>();
        builder.Services.AddTransient<OrderConfirmationViewModel>();
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<AdminNotificationsViewModel>();
        builder.Services.AddTransient<AdminUsersViewModel>();
        builder.Services.AddTransient<AdminReportsViewModel>();
        builder.Services.AddTransient<AdminMenuViewModel>();
        builder.Services.AddTransient<AdminActiveOrdersViewModel>();
        builder.Services.AddTransient<AdminCompaniesViewModel>();
        builder.Services.AddTransient<AdminDiscountsViewModel>();

        // 4. Views
        builder.Services.AddTransient<UserDashboardPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<SelectDeliveryDayPage>();
        builder.Services.AddTransient<ActiveOrdersPage>();
        builder.Services.AddTransient<OrderHistoryPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<HelpPage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<PaymentPage>();
        builder.Services.AddTransient<TaxInvoicePage>();
        builder.Services.AddTransient<OrderConfirmationPage>();
        builder.Services.AddTransient<CancellationPolicyPage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<AdminNotificationsPage>();
        builder.Services.AddTransient<AdminUsersPage>();
        builder.Services.AddTransient<AdminReportsPage>();
        builder.Services.AddTransient<AdminMenuPage>();
        builder.Services.AddTransient<AdminActiveOrdersPage>();
        builder.Services.AddTransient<AdminCompaniesPage>();
        builder.Services.AddTransient<AdminDiscountsPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}