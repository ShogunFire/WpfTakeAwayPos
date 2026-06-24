namespace RestaurantDashboard.Data.Services;

public class ReportFilterState
{
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);
    public string SelectedLocationId { get; set; } = string.Empty;

    public event Func<Task>? FiltersChanged;

    public async Task NotifyFiltersChangedAsync()
    {
        if (FiltersChanged == null)
        {
            return;
        }

        foreach (var handler in FiltersChanged.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                await handler();
            }
            catch
            {
            }
        }
    }
}
