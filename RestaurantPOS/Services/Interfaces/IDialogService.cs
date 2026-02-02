using System.Threading.Tasks;
using RestaurantPOS.Views.Controls;

namespace RestaurantPOS.Services.Interfaces
{
    public interface IDialogService
    {
        void Initialize(DialogBox dialogBox);
        Task<bool> Confirm(string message, string title = "Confirm");
        Task Alert(string message, string title = "Alert");
    }
}
