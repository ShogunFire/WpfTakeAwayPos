using RestaurantShared.DTOs;

namespace RestaurantPOS.Services.Interfaces
{
    public interface ISyncEventService
    {
        void CreateEvent(string type, object payload);
        void CreateEvent(EventDto @event);
    }
}
