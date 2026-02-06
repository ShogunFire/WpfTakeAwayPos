using RestaurantPOS.Models;

namespace RestaurantPOS.Services.Interfaces
{
    public interface IShiftService
    {
        Shift GetActiveShift();
        Shift StartNewShift(decimal openingCash);
        Shift EndShift(decimal declaredCash, decimal expectedCash, string notes = null);
        long GetActiveShiftId();
    }
}
