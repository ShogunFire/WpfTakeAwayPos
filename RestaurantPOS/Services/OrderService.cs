using RestaurantPOS.Models;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class OrderService
    {
        private List<Order> orders;

        public OrderService()
        {
            orders = new List<Order>();
        }

        public void AddOrder(Order order)
        {
            orders.Add(order);
        }

        public List<Order> GetAllOrders()
        {
            return orders;
        }

        public Order GetOrderById(int orderId)
        {
            return orders.FirstOrDefault(o => o.OrderId == orderId);
        }

        public void RemoveOrder(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order != null)
            {
                orders.Remove(order);
            }
        }

        public void UpdateOrder(Order updatedOrder)
        {
            var order = GetOrderById(updatedOrder.OrderId);
            if (order != null)
            {
                    order.OrderLines = updatedOrder.OrderLines;
                order.TotalAmount = updatedOrder.TotalAmount;
                // Update other properties as needed
            }
        }
    }
}