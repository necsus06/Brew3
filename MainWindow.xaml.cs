using Brew3.Controlers;
using Brew3.Models;
using Brew3.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Brew3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Database db = new Database();
        private List<Product> AllProductsList = new List<Product>();
        private User currentuser = new User();
        private string currentcategory = string.Empty;
        private readonly ObservableCollection<CartItem> cartItems = new ObservableCollection<CartItem>();

        public MainWindow(User user)
        {
            InitializeComponent();
            currentuser = user;
            
            // Подписываемся на событие загрузки окна для установки логина
            this.Loaded += MainWindow_Loaded;

            try
            {
                // Загружаем RoleNavigation для текущего пользователя из базы данных
                var userWithRole = db.Users
                    .Include(u => u.RoleNavigation)
                    .FirstOrDefault(u => u.Id == currentuser.Id);
                
                if (userWithRole != null && userWithRole.RoleNavigation != null)
                {
                    currentuser.RoleNavigation = userWithRole.RoleNavigation;
                }

                // Загружаем ВСЕ товары (для отладки сначала загружаем все, потом фильтруем)
                var allProductsInDb = db.Products.ToList();
                
                // Фильтруем доступные товары (IsAvailable == true, NULL считается как false)
                AllProductsList = allProductsInDb
                    .Where(p => p.IsAvailable == true)
                    .ToList();

                // Если доступных товаров нет, но есть товары в базе - показываем сообщение
                if (AllProductsList.Count == 0 && allProductsInDb.Count > 0)
                {
                    MessageBox.Show(
                        $"В базе данных найдено товаров: {allProductsInDb.Count}, но ни один не помечен как доступный (IsAvailable = true).\n\n" +
                        "Проверьте значение IsAvailable в базе данных.",
                        "Внимание",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // Устанавливаем изображение по умолчанию для товаров без ImagePath
                foreach (var product in AllProductsList)
                {
                    if (string.IsNullOrEmpty(product.ImagePath))
                    {
                        product.ImagePath = "Images/Default/Default.png";
                    }
                }

                // Отображаем товары
                if (AllProductsList.Count > 0)
                {
                    ShowProductsView(AllProductsList);
                }
                else
                {
                    ShowProductsView(new List<Product>()); // Покажет сообщение "Товары не найдены"
                }

                // Скрываем кнопки в зависимости от роли пользователя
                // Role: 1 - Admin, 2 - Client, 3 - Manager
                if (currentuser.Role == 2) // Client
                {
                    // Для клиента скрываем только кнопку добавления товаров
                    AddButton.Visibility = Visibility.Collapsed;
                    // Кнопка отчетности доступна для Admin (1) и Manager (3)
                }
                else if (currentuser.Role == 1 || currentuser.Role == 3)
                {
                    // Для Admin и Manager показываем все кнопки, включая отчетность
                    // StatsButton уже виден по умолчанию
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем логин пользователя после загрузки окна
            if (UserLoginText != null)
            {
                UserLoginText.Text = $"Пользователь: {currentuser.Login ?? "Неизвестно"}";
            }
        }

        // === Отображение списка товаров в MainContentRegion ===
        private void ShowProductsView(List<Product> products)
        {
            if (products == null || products.Count == 0)
            {
                // Если товаров нет, показываем сообщение
                var textBlock = new TextBlock
                {
                    Text = "Товары не найдены",
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };
                MainContentRegion.Content = textBlock;
                return;
            }

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20)
            };

            foreach (var product in products)
            {
                var control = new ProductsUserControl(product);
                control.Margin = new Thickness(10);
                // Размеры заданы в самом UserControl, не переопределяем их здесь
                control.AddedToCart += OnProductAdded; // подписка на добавление
                wrapPanel.Children.Add(control);
            }

            scrollViewer.Content = wrapPanel;
            MainContentRegion.Content = scrollViewer;
        }

        // === Обработчик добавления товара в корзину ===
        private void OnProductAdded(Product product)
        {
            try
            {
                if (product == null) return;
                
                // Создаем копию товара, чтобы избежать проблем с EF отслеживанием
                var productCopy = new Product
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Category = product.Category,
                    ImagePath = product.ImagePath,
                    IsAvailable = product.IsAvailable
                };
                
                // Проверяем, есть ли уже такой товар в корзине
                var existingItem = cartItems.FirstOrDefault(item => item?.Product?.Id == productCopy.Id);
                
                if (existingItem != null)
                {
                    // Увеличиваем количество
                    existingItem.Quantity++;
                }
                else
                {
                    // Добавляем новый товар
                    cartItems.Add(new CartItem(productCopy));
                }
                
                MessageBox.Show($"Товар «{product.Name}» добавлен в корзину.", "Корзина",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Фильтрация ===
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SortAndShow();
        }

        private void SortAndShow()
        {
            var query = db.Products.AsQueryable()
                .Where(p => p.IsAvailable == true);

            if (SearchBox != null && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string term = SearchBox.Text.ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrEmpty(currentcategory))
            {
                query = query.Where(p => p.Category == currentcategory);
            }

            var sorted = query.ToList();
            ShowProductsView(sorted);
        }

        private void All_Click(object sender, RoutedEventArgs e)
        {
            currentcategory = "";
            SortAndShow();
        }

        private void Dishes_Click(object sender, RoutedEventArgs e)
        {
            currentcategory = "Dishes";
            SortAndShow();
        }

        private void Drinks_Click(object sender, RoutedEventArgs e)
        {
            currentcategory = "Drinks";
            SortAndShow();
        }

        private void Deserts_Click(object sender, RoutedEventArgs e)
        {
            currentcategory = "Desserts";
            SortAndShow();
        }

        // === Навигация ===
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowProductsView(AllProductsList);
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем новое представление каждый раз
            var ordersView = new OrdersView(currentuser);
            MainContentRegion.Content = ordersView;
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем новое представление каждый раз
            MainContentRegion.Content = new StatsView();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем новое представление каждый раз
            MainContentRegion.Content = new AddProductView();
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем новое представление каждый раз
                var cartView = new CartView(currentuser, cartItems, OnOrderCreated);
                MainContentRegion.Content = cartView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии корзины: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOrderCreated()
        {
            // Переключаемся на страницу заказов (она создастся заново с актуальными данными)
            var ordersView = new OrdersView(currentuser);
            MainContentRegion.Content = ordersView;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите выйти из аккаунта?",
                    "Выход",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Закрываем текущее окно и открываем окно входа
                    var loginWindow = new Views.LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при выходе из аккаунта: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}