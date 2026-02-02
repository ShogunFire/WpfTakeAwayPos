using System.Threading.Tasks;

namespace RestaurantPOS.Services.Interfaces
{
    public interface INavigationGuard
    {
        Task<bool> CanNavigateAwayAsync();
    }
}
