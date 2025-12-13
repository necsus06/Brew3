using Brew3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Brew3.Views
{
    public partial class OrdersView : UserControl
    {
        private readonly User _user;
        private DispatcherTimer? _statusUpdateTimer;
        private readonly string[] _statuses = { "New", "В обработке", "Готовится", "Готов" };

        public OrdersView(User user)
        {
            InitializeComponent();
            _user = user;
            LoadOrders();
            StartStatusUpdateTimer();
            this.Unloaded += OrdersView_Unloaded;
        }

        private void OrdersView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _statusUpdateTimer?.Stop();
        }

        private void StartStatusUpdateTimer()
        {
            // Таймер для автоматического изменения статуса заказов (каждые 5 секунд)
            _statusUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _statusUpdateTimer.Tick += async (s, e) => await UpdateOrderStatuses();
            _statusUpdateTimer.Start();
        }

        private async System.Threading.Tasks.Task UpdateOrderStatuses()
        {
            try
            {
                using var db = new Database();
                var orders = await db.Orders
                    .Where(o => o.UserId == _user.Id)
                    .ToListAsync();

                bool hasChanges = false;
                foreach (var order in orders)
                {
                    int currentIndex = Array.IndexOf(_statuses, order.Status);
                    if (currentIndex >= 0 && currentIndex < _statuses.Length - 1)
                    {
                        order.Status = _statuses[currentIndex + 1];
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    await db.SaveChangesAsync();
                    LoadOrders(); // Обновляем отображение
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении статусов: {ex.Message}");
            }
        }

        public void RefreshOrders()
        {
            LoadOrders();
        }

        private async void LoadOrders()
        {
            using var db = new Database();
            var orders = await db.Orders
                .Where(o => o.UserId == _user.Id)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            foreach (var order in orders)
            {
                order.TotalPrice = order.OrderItems.Sum(oi => oi.Quantity * oi.Product.Price);
            }

            OrdersList.ItemsSource = orders;
        }

        private async void CloseOrder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Order order)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите закрыть заказ №{order.OrderNumber}?",
                    "Закрыть заказ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var db = new Database();
                        
                        // Удаляем все элементы заказа
                        var orderItems = await db.OrderItems
                            .Where(oi => oi.OrderId == order.Id)
                            .ToListAsync();
                        db.OrderItems.RemoveRange(orderItems);
                        
                        // Удаляем сам заказ
                        var orderToDelete = await db.Orders.FindAsync(order.Id);
                        if (orderToDelete != null)
                        {
                            db.Orders.Remove(orderToDelete);
                        }
                        
                        await db.SaveChangesAsync();
                        
                        MessageBox.Show(
                            $"Заказ №{order.OrderNumber} успешно закрыт.",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        
                        // Обновляем список заказов
                        LoadOrders();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при закрытии заказа: {ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}