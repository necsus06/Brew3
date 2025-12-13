using Brew3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Brew3.Views
{
    public partial class CartView : UserControl
    {
        private readonly User _user;
        private readonly ObservableCollection<CartItem> _cartItems;
        private readonly Action? _onOrderCreated;
        
        private bool _isInitialized = false;

        public CartView(User user, ObservableCollection<CartItem> cartItems, Action onOrderCreated)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (cartItems == null) throw new ArgumentNullException(nameof(cartItems));
            
            InitializeComponent();
            
            // Инициализируем поля после InitializeComponent
            _user = user;
            _cartItems = cartItems;
            _onOrderCreated = onOrderCreated;
            
            try
            {
                // Подписываемся на изменения в коллекции ПЕРЕД установкой ItemsSource
                _cartItems.CollectionChanged += CartItems_CollectionChanged;
                
                // Подписываемся на изменения свойств существующих элементов
                foreach (var item in _cartItems.ToList()) // Используем ToList() для безопасной итерации
                {
                    if (item != null && item.Product != null)
                    {
                        item.PropertyChanged += CartItem_PropertyChanged;
                    }
                }
                
                // Устанавливаем ItemsSource ПОСЛЕ всех подписок
                if (CartItemsList != null)
                {
                    CartItemsList.ItemsSource = _cartItems;
                }
                
                _isInitialized = true;
                
                // Обновляем UI с небольшой задержкой, чтобы убедиться, что все элементы инициализированы
                Loaded += CartView_Loaded;
                
                // Также обновляем сразу на случай, если Loaded уже произошел
                if (IsLoaded)
                {
                    UpdateTotal();
                    UpdateCreateOrderButton();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации корзины: {ex.Message}\n\n{ex.StackTrace}", "Критическая ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CartView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateTotal();
                UpdateCreateOrderButton();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в CartView_Loaded: {ex.Message}");
            }
        }

        private void CartItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                // Подписываемся на новые элементы
                if (e.NewItems != null)
                {
                    foreach (CartItem item in e.NewItems)
                    {
                        if (item != null && item.Product != null)
                        {
                            // Проверяем, не подписаны ли уже
                            item.PropertyChanged -= CartItem_PropertyChanged; // Сначала отписываемся, чтобы избежать дублирования
                            item.PropertyChanged += CartItem_PropertyChanged;
                        }
                    }
                }
                
                // Отписываемся от удаленных элементов
                if (e.OldItems != null)
                {
                    foreach (CartItem item in e.OldItems)
                    {
                        if (item != null)
                        {
                            item.PropertyChanged -= CartItem_PropertyChanged;
                        }
                    }
                }
                
                // Обновляем UI напрямую
                UpdateTotal();
                UpdateCreateOrderButton();
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                System.Diagnostics.Debug.WriteLine($"Ошибка в CartItems_CollectionChanged: {ex.Message}");
            }
        }

        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // Обновляем общую сумму при изменении количества или цены товара
                if (e?.PropertyName == nameof(CartItem.Quantity) || e?.PropertyName == nameof(CartItem.TotalPrice))
                {
                    // Простое обновление без Dispatcher - мы уже в UI потоке
                    UpdateTotal();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                System.Diagnostics.Debug.WriteLine($"Ошибка в CartItem_PropertyChanged: {ex.Message}");
            }
        }

        private void UpdateCreateOrderButton()
        {
            if (CreateOrderButton != null)
            {
                CreateOrderButton.IsEnabled = _cartItems.Count > 0;
            }
        }

        private void UpdateTotal()
        {
            try
            {
                if (!_isInitialized || TotalAmountText == null || _cartItems == null)
                    return;
                    
                decimal total = 0;
                foreach (var item in _cartItems)
                {
                    if (item != null && item.Product != null)
                    {
                        try
                        {
                            total += item.TotalPrice;
                        }
                        catch
                        {
                            // Пропускаем проблемные элементы
                        }
                    }
                }
                TotalAmountText.Text = $"{total:C}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateTotal: {ex.Message}");
                if (TotalAmountText != null)
                {
                    TotalAmountText.Text = "Ошибка";
                }
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                item.Quantity++;
                UpdateTotal();
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    _cartItems.Remove(item);
                }
                UpdateTotal();
                UpdateCreateOrderButton();
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                _cartItems.Remove(item);
                UpdateTotal();
                UpdateCreateOrderButton();
            }
        }

        private async void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new Database();
                
                // Генерируем номер заказа
                var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                
                // Создаем заказ
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    CreatedAt = DateTime.Now,
                    Status = "New",
                    Total = _cartItems.Sum(item => item.TotalPrice),
                    UserId = _user.Id,
                    IsTakeaway = false
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                // Добавляем товары в заказ
                foreach (var cartItem in _cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.Product.Id,
                        Quantity = cartItem.Quantity
                    };
                    db.OrderItems.Add(orderItem);
                }

                await db.SaveChangesAsync();

                // Очищаем корзину
                _cartItems.Clear();
                UpdateTotal();

                MessageBox.Show($"Заказ №{order.Id} успешно создан!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Вызываем callback для обновления списка заказов
                _onOrderCreated?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заказа: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Класс для представления товара в корзине
    public class CartItem : INotifyPropertyChanged
    {
        private Product _product;
        private int _quantity = 1;

        public Product Product 
        { 
            get => _product;
            set
            {
                _product = value;
                OnPropertyChanged(nameof(Product));
                UpdateTotalPrice(); // Обновляем TotalPrice при изменении продукта
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    UpdateTotalPrice(); // Обновляем TotalPrice при изменении количества
                }
            }
        }

        private decimal _totalPrice;

        public decimal TotalPrice
        {
            get => _totalPrice;
            private set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        private void UpdateTotalPrice()
        {
            if (Product == null)
            {
                TotalPrice = 0;
                return;
            }
            
            try
            {
                TotalPrice = Product.Price * Quantity;
            }
            catch
            {
                TotalPrice = 0;
            }
        }

        public CartItem(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            
            // Создаем копию Product, чтобы избежать проблем с EF отслеживанием
            _product = new Product
            {
                Id = product.Id,
                Name = product.Name ?? "Без названия",
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                ImagePath = product.ImagePath ?? "Images/Default/Default.png",
                IsAvailable = product.IsAvailable
            };
            
            // Инициализируем TotalPrice
            UpdateTotalPrice();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

