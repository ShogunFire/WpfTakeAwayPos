using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RestaurantDashboard.Data.Models;
using RestaurantDashboard.Data.Services;

namespace RestaurantDashboard.Components.Pages;

public partial class Dashboard
{
    [Inject] private DashboardService DashboardService { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private DateTime startDate = new(2026, 1, 1);
    private DateTime endDate = new(2026, 1, 25);
    private string selectedRestaurant = "";
    
    private DashboardMetrics? metrics;
    private List<SalesDataPoint> salesData = new();
    private List<TopItem> topItems = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender || (!isLoading && metrics != null))
        {
            await InitializeCharts();
        }
    }

    private async Task LoadDashboardData()
    {
        isLoading = true;
        StateHasChanged();
        
        // Simulate async data loading
        await Task.Delay(300);
        
        metrics = DashboardService.GetMetrics(startDate, endDate, selectedRestaurant);
        salesData = DashboardService.GetSalesOverTime(startDate, endDate, selectedRestaurant);
        topItems = DashboardService.GetTopSellingItems(startDate, endDate, selectedRestaurant);
        
        isLoading = false;
    }

    private async Task OnDateRangeChanged()
    {
        await LoadDashboardData();
    }

    private async Task OnRestaurantChanged()
    {
        await LoadDashboardData();
    }

    private async Task InitializeCharts()
    {
        try
        {
            // Sales Chart
            var salesLabels = salesData.Select(s => s.Date).ToList();
            var salesValues = salesData.Select(s => (double)s.Sales).ToList();

            await JS.InvokeVoidAsync("initSalesChart", salesLabels, salesValues);

            // Top Items Chart
            var itemLabels = topItems.Select(t => t.Name).ToList();
            var itemValues = topItems.Select(t => (double)t.Percentage).ToList();

            await JS.InvokeVoidAsync("initTopItemsChart", itemLabels, itemValues);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing charts: {ex.Message}");
        }
    }
}

