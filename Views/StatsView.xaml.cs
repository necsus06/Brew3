using System.Windows.Controls;
using Brew3.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Windows;
using System;

namespace Brew3.Views
{
    public partial class StatsView : UserControl
    {
        private int _totalOrders;
        private decimal _totalRevenue;
        private string _topProduct = "—";
        private string _currentPeriod = "AllTime"; // По умолчанию "Все время"

        public StatsView()
        {
            InitializeComponent();
            // Устанавливаем активную кнопку по умолчанию
            UpdateButtonStyles("AllTime");
            LoadStats("AllTime");
        }

        private void PeriodButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string period)
            {
                _currentPeriod = period;
                UpdateButtonStyles(period);
                LoadStats(period);
            }
        }

        private void UpdateButtonStyles(string activePeriod)
        {
            // Сбрасываем все кнопки к неактивному стилю
            TodayButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 138, 133));
            WeekButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 138, 133));
            MonthButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 138, 133));
            AllTimeButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 138, 133));

            // Устанавливаем активную кнопку
            var activeColor = System.Windows.Media.Color.FromRgb(97, 111, 83);
            switch (activePeriod)
            {
                case "Today":
                    TodayButton.Background = new System.Windows.Media.SolidColorBrush(activeColor);
                    break;
                case "Week":
                    WeekButton.Background = new System.Windows.Media.SolidColorBrush(activeColor);
                    break;
                case "Month":
                    MonthButton.Background = new System.Windows.Media.SolidColorBrush(activeColor);
                    break;
                case "AllTime":
                    AllTimeButton.Background = new System.Windows.Media.SolidColorBrush(activeColor);
                    break;
            }
        }

        private DateTime GetPeriodStartDate(string period)
        {
            var now = DateTime.Now;
            return period switch
            {
                "Today" => now.Date, // Начало сегодняшнего дня
                "Week" => now.Date.AddDays(-7), // Последние 7 дней
                "Month" => new DateTime(now.Year, now.Month, 1), // Начало текущего месяца
                "AllTime" => DateTime.MinValue, // Все время
                _ => DateTime.MinValue
            };
        }

        private async void LoadStats(string period)
        {
            using var db = new Database();

            DateTime periodStart = GetPeriodStartDate(period);
            
            // Фильтруем заказы по периоду
            var ordersQuery = db.Orders.AsQueryable();
            if (period != "AllTime")
            {
                ordersQuery = ordersQuery.Where(o => o.CreatedAt >= periodStart);
            }

            _totalOrders = await ordersQuery.CountAsync();

            // Фильтруем элементы заказов по периоду
            var orderItemsQuery = db.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .AsQueryable();
            
            if (period != "AllTime")
            {
                orderItemsQuery = orderItemsQuery.Where(oi => oi.Order.CreatedAt >= periodStart);
            }

            _totalRevenue = await orderItemsQuery
                .SumAsync(oi => oi.Quantity * oi.Product.Price);

            var topProductData = await orderItemsQuery
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            _topProduct = "—";
            if (topProductData != null)
            {
                var product = await db.Products.FindAsync(topProductData.ProductId);
                _topProduct = product?.Name ?? "—";
            }

            TotalOrdersText.Text = _totalOrders.ToString();
            TotalRevenueText.Text = $"{_totalRevenue:C}";
            TopProductText.Text = _topProduct ?? "—";
        }

        private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем папку Stats относительно папки приложения
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string statsFolder = Path.Combine(baseDir, "Stats");
                if (!Directory.Exists(statsFolder))
                {
                    Directory.CreateDirectory(statsFolder);
                }

                // Генерируем имя файла с датой и временем
                string fileName = $"Отчет_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                string filePath = Path.Combine(statsFolder, fileName);

                // Загружаем дополнительные метрики для отчета с учетом выбранного периода
                using var db = new Database();
                
                DateTime periodStart = GetPeriodStartDate(_currentPeriod);
                
                var totalProducts = await db.Products.CountAsync();
                var availableProducts = await db.Products.CountAsync(p => p.IsAvailable == true);
                var totalUsers = await db.Users.CountAsync();
                
                var ordersQuery = db.Orders.AsQueryable();
                if (_currentPeriod != "AllTime")
                {
                    ordersQuery = ordersQuery.Where(o => o.CreatedAt >= periodStart);
                }
                
                var ordersByStatus = await ordersQuery
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var orderItemsQuery = db.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .AsQueryable();
                
                if (_currentPeriod != "AllTime")
                {
                    orderItemsQuery = orderItemsQuery.Where(oi => oi.Order.CreatedAt >= periodStart);
                }

                var topProducts = await orderItemsQuery
                    .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                    .Select(g => new { 
                        Name = g.Key.Name, 
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.Quantity * x.Product.Price)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(5)
                    .ToListAsync();

                // Формируем содержимое отчета
                var reportContent = new System.Text.StringBuilder();
                reportContent.AppendLine("═══════════════════════════════════════════════════════");
                reportContent.AppendLine("                    ОТЧЕТ ПО СТАТИСТИКЕ");
                reportContent.AppendLine("═══════════════════════════════════════════════════════");
                reportContent.AppendLine();
                reportContent.AppendLine($"Дата формирования отчета: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                
                // Добавляем информацию о периоде
                string periodText = _currentPeriod switch
                {
                    "Today" => "Сегодня",
                    "Week" => "Последние 7 дней",
                    "Month" => "Текущий месяц",
                    "AllTime" => "За все время",
                    _ => "Неизвестный период"
                };
                reportContent.AppendLine($"Период отчета: {periodText}");
                if (_currentPeriod != "AllTime")
                {
                    reportContent.AppendLine($"С: {periodStart:dd.MM.yyyy HH:mm}");
                }
                reportContent.AppendLine();
                reportContent.AppendLine("───────────────────────────────────────────────────────");
                reportContent.AppendLine("ОСНОВНЫЕ МЕТРИКИ");
                reportContent.AppendLine("───────────────────────────────────────────────────────");
                reportContent.AppendLine($"Общее количество заказов: {_totalOrders}");
                reportContent.AppendLine($"Общий доход: {_totalRevenue:C}");
                reportContent.AppendLine($"Популярный товар: {_topProduct ?? "—"}");
                reportContent.AppendLine();
                reportContent.AppendLine("───────────────────────────────────────────────────────");
                reportContent.AppendLine("ДОПОЛНИТЕЛЬНАЯ ИНФОРМАЦИЯ");
                reportContent.AppendLine("───────────────────────────────────────────────────────");
                reportContent.AppendLine($"Всего товаров в базе: {totalProducts}");
                reportContent.AppendLine($"Доступных товаров: {availableProducts}");
                reportContent.AppendLine($"Всего пользователей: {totalUsers}");
                reportContent.AppendLine();
                
                if (ordersByStatus.Any())
                {
                    reportContent.AppendLine("───────────────────────────────────────────────────────");
                    reportContent.AppendLine("ЗАКАЗЫ ПО СТАТУСАМ");
                    reportContent.AppendLine("───────────────────────────────────────────────────────");
                    foreach (var statusGroup in ordersByStatus)
                    {
                        reportContent.AppendLine($"{statusGroup.Status}: {statusGroup.Count}");
                    }
                    reportContent.AppendLine();
                }

                if (topProducts.Any())
                {
                    reportContent.AppendLine("───────────────────────────────────────────────────────");
                    reportContent.AppendLine("ТОП-5 ТОВАРОВ ПО ПРОДАЖАМ");
                    reportContent.AppendLine("───────────────────────────────────────────────────────");
                    int position = 1;
                    foreach (var product in topProducts)
                    {
                        reportContent.AppendLine($"{position}. {product.Name}");
                        reportContent.AppendLine($"   Количество продаж: {product.Quantity}");
                        reportContent.AppendLine($"   Выручка: {product.Revenue:C}");
                        reportContent.AppendLine();
                        position++;
                    }
                }

                reportContent.AppendLine("═══════════════════════════════════════════════════════");
                reportContent.AppendLine($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                reportContent.AppendLine("═══════════════════════════════════════════════════════");

                // Сохраняем отчет в файл
                await File.WriteAllTextAsync(filePath, reportContent.ToString(), System.Text.Encoding.UTF8);

                MessageBox.Show(
                    $"Отчет успешно сохранен!\n\nПуть: {filePath}",
                    "Отчет создан",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при создании отчета:\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}