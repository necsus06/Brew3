# Подробная документация проекта Brew3

## Содержание
1. [Общая архитектура приложения](#общая-архитектура-приложения)
2. [Структура окон и их назначение](#структура-окон-и-их-назначение)
3. [Модели данных и база данных](#модели-данных-и-база-данных)
4. [Алгоритмы и паттерны проектирования](#алгоритмы-и-паттерны-проектирования)
5. [Детальное описание функционала](#детальное-описание-функционала)

---

## Общая архитектура приложения

### Технологический стек
- **Платформа:** .NET 8.0 (Windows)
- **UI Framework:** WPF (Windows Presentation Foundation)
- **ORM:** Entity Framework Core 8.0
- **База данных:** SQL Server
- **Язык программирования:** C#

### Архитектурный паттерн
Приложение использует **упрощенный вариант MVVM (Model-View-ViewModel)**:
- **Model** - классы в папке `Models/` (Product, Order, User и т.д.)
- **View** - XAML файлы и UserControl'ы в папке `Views/`
- **ViewModel** - частично реализован через code-behind файлы (.xaml.cs)

### Структура проекта
```
Brew3/
├── App.xaml / App.xaml.cs          # Точка входа приложения
├── MainWindow.xaml / .cs            # Главное окно приложения
├── Models/                          # Модели данных (Entity Framework)
│   ├── Database.cs                  # DbContext для работы с БД
│   ├── Product.cs                   # Модель товара
│   ├── Order.cs                     # Модель заказа
│   ├── User.cs                      # Модель пользователя
│   └── ...
├── Views/                           # Окна и представления
│   ├── LoginWindow.xaml / .cs       # Окно авторизации
│   ├── CartView.xaml / .cs          # Представление корзины
│   ├── OrdersView.xaml / .cs        # Представление заказов
│   ├── StatsView.xaml / .cs         # Представление статистики
│   └── AddProductView.xaml / .cs    # Представление добавления товара
└── Controlers/                      # Пользовательские контролы
    └── ProductsUserControl.xaml/.cs # Карточка товара
```

---

## Структура окон и их назначение

### 1. App.xaml.cs - Точка входа приложения

**Назначение:** Инициализация приложения и глобальная обработка ошибок.

**Принцип работы:**
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // Подписываемся на события необработанных исключений
    this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
}
```

**Алгоритм:**
1. При запуске приложения вызывается `OnStartup`
2. Регистрируются глобальные обработчики исключений
3. При любой необработанной ошибке показывается MessageBox с деталями
4. Приложение не закрывается (`e.Handled = true`), что позволяет продолжить работу

**Зачем это нужно:**
- Предотвращает внезапное закрытие приложения
- Помогает диагностировать ошибки в продакшене
- Улучшает пользовательский опыт

---

### 2. LoginWindow - Окно авторизации

**Назначение:** Аутентификация пользователей перед входом в систему.

**Структура:**
- Поля ввода: `LoginBox`, `PasswordBox`
- Кнопка входа: `LoginButton`
- Сообщение об ошибке: `ErrorMessage`

**Алгоритм работы:**

```csharp
// 1. При создании окна загружаются все пользователи из БД
users = db.Users.Include(user => user.RoleNavigation).ToList();

// 2. При нажатии "Войти"
private void LoginButton_Click(object sender, RoutedEventArgs e)
{
    // 2.1. Проверка заполненности полей
    if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        return;
    
    // 2.2. Поиск пользователя в списке
    User? user = users.FirstOrDefault(
        u => u.Login == login && u.PasswordHash == password
    );
    
    // 2.3. Если найден - открываем MainWindow
    if (user != null)
    {
        MainWindow mainWindow = new MainWindow(user);
        mainWindow.Show();
        this.Close();
    }
    else
    {
        ErrorMessage.Visibility = Visibility.Visible;
    }
}
```

**Используемые алгоритмы:**
- **Линейный поиск** (`FirstOrDefault`) - O(n) сложность, но для небольшого количества пользователей это приемлемо
- **Eager Loading** (`Include`) - загружаем связанные данные (роли) сразу, чтобы избежать N+1 проблемы

**Особенности:**
- Пароли хранятся в открытом виде (`PasswordHash`), что небезопасно для продакшена
- Все пользователи загружаются в память при старте (для небольшого количества это нормально)

---

### 3. MainWindow - Главное окно приложения

**Назначение:** Центральный хаб приложения, управление навигацией и отображением контента.

**Структура окна:**
```
┌─────────────────────────────────────────────────┐
│ Шапка (Header)                                  │
│ - Логотип "BrewLog"                             │
│ - Кнопки фильтров (Всё, Блюда, Напитки, Десерты)│
│ - Поле поиска                                    │
├──────────┬──────────────────────────────────────┤
│          │                                      │
│ Левая    │ Основная область контента           │
│ панель   │ (MainContentRegion)                 │
│          │                                      │
│ - Меню   │ Здесь отображаются:                 │
│ - Корзина│ - Список товаров                    │
│ - Заказы │ - Корзина                            │
│ - Отчеты │ - Заказы                            │
│ - Добавить│ - Статистика                       │
│ - Выйти  │ - Форма добавления товара           │
│          │                                      │
│ Пользователь: [логин]                          │
└──────────┴──────────────────────────────────────┘
```

**Ключевые поля класса:**
```csharp
private readonly Database db = new Database();           // Подключение к БД
private List<Product> AllProductsList;                   // Кэш всех товаров
private User currentuser;                                // Текущий пользователь
private string currentcategory = string.Empty;          // Выбранная категория
private readonly ObservableCollection<CartItem> cartItems; // Корзина
```

**Алгоритм инициализации:**

```csharp
public MainWindow(User user)
{
    InitializeComponent();
    currentuser = user;
    
    // 1. Загружаем роль пользователя из БД
    var userWithRole = db.Users
        .Include(u => u.RoleNavigation)
        .FirstOrDefault(u => u.Id == currentuser.Id);
    
    // 2. Загружаем все доступные товары
    AllProductsList = db.Products
        .Where(p => p.IsAvailable == true)
        .ToList();
    
    // 3. Устанавливаем изображения по умолчанию
    foreach (var product in AllProductsList)
    {
        if (string.IsNullOrEmpty(product.ImagePath))
            product.ImagePath = "Images/Default/Default.png";
    }
    
    // 4. Отображаем товары
    ShowProductsView(AllProductsList);
    
    // 5. Скрываем кнопки в зависимости от роли
    if (currentuser.Role == 2) // Client
        AddButton.Visibility = Visibility.Collapsed;
}
```

**Алгоритм фильтрации и поиска:**

```csharp
private void SortAndShow()
{
    // 1. Начинаем с базового запроса - только доступные товары
    var query = db.Products.AsQueryable()
        .Where(p => p.IsAvailable == true);
    
    // 2. Применяем фильтр по поисковому запросу
    if (!string.IsNullOrWhiteSpace(SearchBox.Text))
    {
        string term = SearchBox.Text.ToLower();
        query = query.Where(p =>
            (p.Name != null && p.Name.ToLower().Contains(term)) ||
            (p.Description != null && p.Description.ToLower().Contains(term))
        );
    }
    
    // 3. Применяем фильтр по категории
    if (!string.IsNullOrEmpty(currentcategory))
        query = query.Where(p => p.Category == currentcategory);
    
    // 4. Выполняем запрос и отображаем
    var sorted = query.ToList();
    ShowProductsView(sorted);
}
```

**Почему используется `AsQueryable()`:**
- Позволяет строить запрос динамически
- Запрос выполняется только при вызове `ToList()`
- Эффективно - фильтрация происходит на стороне БД, а не в памяти

**Алгоритм отображения товаров:**

```csharp
private void ShowProductsView(List<Product> products)
{
    // 1. Создаем ScrollViewer для прокрутки
    var scrollViewer = new ScrollViewer {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    };
    
    // 2. Создаем WrapPanel для размещения карточек
    var wrapPanel = new WrapPanel {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(20)
    };
    
    // 3. Для каждого товара создаем карточку
    foreach (var product in products)
    {
        var control = new ProductsUserControl(product);
        control.AddedToCart += OnProductAdded; // Подписываемся на событие
        wrapPanel.Children.Add(control);
    }
    
    // 4. Устанавливаем содержимое
    scrollViewer.Content = wrapPanel;
    MainContentRegion.Content = scrollViewer;
}
```

**Почему WrapPanel:**
- Автоматически переносит карточки на новую строку
- Адаптивный layout - карточки подстраиваются под размер окна
- Проще, чем Grid с фиксированными колонками

---

### 4. ProductsUserControl - Карточка товара

**Назначение:** Визуальное представление одного товара в списке.

**Структура карточки:**
- Размер: 950x270 пикселей
- Левая часть: Изображение товара (300x250)
- Правая часть: Название, описание, цена, кнопка "Добавить"

**Алгоритм загрузки изображения:**

```csharp
private void LoadImage(Product product)
{
    // 1. Получаем путь к изображению из продукта
    string imagePath = product.ImagePath;
    
    // 2. Нормализуем путь (убираем начальный /, заменяем \ на /)
    if (imagePath.StartsWith("/"))
        imagePath = imagePath.Substring(1);
    imagePath = imagePath.Replace('\\', '/');
    
    // 3. Если путь относительный - делаем абсолютным
    if (!Path.IsPathRooted(imagePath))
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        imagePath = Path.Combine(baseDir, imagePath);
        imagePath = Path.GetFullPath(imagePath); // Нормализуем путь
    }
    
    // 4. Проверяем существование файла
    if (File.Exists(imagePath))
    {
        // Загружаем изображение
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Загружаем сразу
        bitmap.EndInit();
        BoxImage.Source = bitmap;
    }
    else
    {
        LoadDefaultImage(); // Fallback на изображение по умолчанию
    }
}
```

**Почему такая сложная логика:**
- Поддержка относительных и абсолютных путей
- Корректная работа с кириллическими символами в путях
- Обработка разных форматов путей (Windows/Linux стиль)
- Fallback на изображение по умолчанию при ошибках

**Событие добавления в корзину:**

```csharp
public event Action<Product>? AddedToCart;

private void Button_Click(object sender, RoutedEventArgs e)
{
    var product = DataContext as Product;
    if (product != null)
        AddedToCart?.Invoke(product); // Вызываем событие
}
```

**Паттерн:** Observer (Наблюдатель) - карточка не знает, что делать с товаром, просто уведомляет подписчиков.

---

### 5. CartView - Представление корзины

**Назначение:** Управление товарами в корзине перед оформлением заказа.

**Ключевые компоненты:**

**Класс CartItem:**
```csharp
public class CartItem : INotifyPropertyChanged
{
    private Product _product;
    private int _quantity = 1;
    private decimal _totalPrice;
    
    // При изменении Quantity или Product автоматически пересчитывается TotalPrice
    public decimal TotalPrice => Product.Price * Quantity;
}
```

**Почему создается копия Product:**
```csharp
public CartItem(Product product)
{
    // Создаем копию, чтобы избежать проблем с Entity Framework отслеживанием
    _product = new Product {
        Id = product.Id,
        Name = product.Name,
        // ... копируем все свойства
    };
}
```

**Проблема, которую это решает:**
- Entity Framework отслеживает изменения объектов
- Если использовать оригинальный Product, EF может пытаться сохранить изменения
- Копия изолирует корзину от БД

**Алгоритм работы корзины:**

```csharp
// 1. При добавлении товара в корзину
private void OnProductAdded(Product product)
{
    // Проверяем, есть ли уже такой товар
    var existingItem = cartItems.FirstOrDefault(
        item => item?.Product?.Id == productCopy.Id
    );
    
    if (existingItem != null)
        existingItem.Quantity++; // Увеличиваем количество
    else
        cartItems.Add(new CartItem(productCopy)); // Добавляем новый
}
```

**Почему ObservableCollection:**
- Автоматически уведомляет UI об изменениях
- При добавлении/удалении элементов список обновляется автоматически
- Реализует паттерн Observer для UI

**Алгоритм создания заказа:**

```csharp
private async void CreateOrder_Click(object sender, RoutedEventArgs e)
{
    // 1. Генерируем уникальный номер заказа
    var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    
    // 2. Создаем заказ
    var order = new Order {
        OrderNumber = orderNumber,
        CreatedAt = DateTime.Now,
        Status = "New",
        Total = cartItems.Sum(item => item.TotalPrice),
        UserId = _user.Id
    };
    db.Orders.Add(order);
    await db.SaveChangesAsync(); // Сохраняем, чтобы получить Id
    
    // 3. Добавляем товары в заказ
    foreach (var cartItem in _cartItems)
    {
        var orderItem = new OrderItem {
            OrderId = order.Id,
            ProductId = cartItem.Product.Id,
            Quantity = cartItem.Quantity
        };
        db.OrderItems.Add(orderItem);
    }
    
    await db.SaveChangesAsync();
    
    // 4. Очищаем корзину
    _cartItems.Clear();
}
```

**Почему два SaveChanges:**
- Первый нужен, чтобы получить `order.Id` из БД
- Второй сохраняет связанные `OrderItem` с правильным `OrderId`

---

### 6. OrdersView - Представление заказов

**Назначение:** Отображение истории заказов пользователя.

**Алгоритм автоматического обновления статусов:**

```csharp
private void StartStatusUpdateTimer()
{
    // Создаем таймер, который срабатывает каждые 5 секунд
    _statusUpdateTimer = new DispatcherTimer {
        Interval = TimeSpan.FromSeconds(5)
    };
    _statusUpdateTimer.Tick += async (s, e) => await UpdateOrderStatuses();
    _statusUpdateTimer.Start();
}

private async Task UpdateOrderStatuses()
{
    // 1. Загружаем все заказы пользователя
    var orders = await db.Orders
        .Where(o => o.UserId == _user.Id)
        .ToListAsync();
    
    // 2. Для каждого заказа проверяем статус
    foreach (var order in orders)
    {
        int currentIndex = Array.IndexOf(_statuses, order.Status);
        // Если статус не последний - переходим к следующему
        if (currentIndex >= 0 && currentIndex < _statuses.Length - 1)
        {
            order.Status = _statuses[currentIndex + 1];
            hasChanges = true;
        }
    }
    
    // 3. Сохраняем изменения
    if (hasChanges)
    {
        await db.SaveChangesAsync();
        LoadOrders(); // Обновляем отображение
    }
}
```

**Почему DispatcherTimer:**
- Работает в UI потоке
- Безопасен для обновления интерфейса
- Проще, чем Task.Delay в цикле

**Алгоритм загрузки заказов:**

```csharp
private async void LoadOrders()
{
    // 1. Загружаем заказы с связанными данными (Eager Loading)
    var orders = await db.Orders
        .Where(o => o.UserId == _user.Id)
        .Include(o => o.OrderItems)        // Загружаем элементы заказа
            .ThenInclude(oi => oi.Product)  // Загружаем товары для каждого элемента
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    
    // 2. Вычисляем TotalPrice для каждого заказа
    foreach (var order in orders)
    {
        order.TotalPrice = order.OrderItems.Sum(
            oi => oi.Quantity * oi.Product.Price
        );
    }
    
    // 3. Устанавливаем источник данных для списка
    OrdersList.ItemsSource = orders;
}
```

**Почему Include/ThenInclude:**
- Без них EF выполнил бы отдельный запрос для каждого заказа (N+1 проблема)
- С ними все данные загружаются одним запросом с JOIN'ами
- Значительно быстрее для больших объемов данных

**Алгоритм закрытия заказа:**

```csharp
private async void CloseOrder_Click(object sender, RoutedEventArgs e)
{
    // 1. Получаем заказ из Tag кнопки
    if (sender is Button button && button.Tag is Order order)
    {
        // 2. Подтверждение
        var result = MessageBox.Show(...);
        
        if (result == MessageBoxResult.Yes)
        {
            // 3. Удаляем все элементы заказа
            var orderItems = await db.OrderItems
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();
            db.OrderItems.RemoveRange(orderItems);
            
            // 4. Удаляем сам заказ
            var orderToDelete = await db.Orders.FindAsync(order.Id);
            db.Orders.Remove(orderToDelete);
            
            // 5. Сохраняем изменения
            await db.SaveChangesAsync();
            
            // 6. Обновляем список
            LoadOrders();
        }
    }
}
```

**Почему сначала удаляем OrderItems:**
- В БД есть внешний ключ: OrderItems ссылается на Orders
- Если удалить Order сначала, возникнет ошибка нарушения целостности
- Правильный порядок: сначала зависимые записи, потом основная

---

### 7. StatsView - Представление статистики

**Назначение:** Отображение аналитики и генерация отчетов.

**Алгоритм фильтрации по периодам:**

```csharp
private DateTime GetPeriodStartDate(string period)
{
    var now = DateTime.Now;
    return period switch
    {
        "Today" => now.Date,                    // Начало сегодня
        "Week" => now.Date.AddDays(-7),         // 7 дней назад
        "Month" => new DateTime(now.Year, now.Month, 1), // Начало месяца
        "AllTime" => DateTime.MinValue,         // Все время
        _ => DateTime.MinValue
    };
}

private async void LoadStats(string period)
{
    DateTime periodStart = GetPeriodStartDate(period);
    
    // 1. Фильтруем заказы по периоду
    var ordersQuery = db.Orders.AsQueryable();
    if (period != "AllTime")
        ordersQuery = ordersQuery.Where(o => o.CreatedAt >= periodStart);
    
    _totalOrders = await ordersQuery.CountAsync();
    
    // 2. Фильтруем элементы заказов по периоду
    var orderItemsQuery = db.OrderItems
        .Include(oi => oi.Order)  // Нужно для фильтрации по дате заказа
        .Include(oi => oi.Product)
        .AsQueryable();
    
    if (period != "AllTime")
        orderItemsQuery = orderItemsQuery.Where(
            oi => oi.Order.CreatedAt >= periodStart
        );
    
    // 3. Вычисляем доход
    _totalRevenue = await orderItemsQuery
        .SumAsync(oi => oi.Quantity * oi.Product.Price);
    
    // 4. Находим популярный товар
    var topProductData = await orderItemsQuery
        .GroupBy(oi => oi.ProductId)
        .Select(g => new { 
            ProductId = g.Key, 
            Count = g.Sum(x => x.Quantity) 
        })
        .OrderByDescending(x => x.Count)
        .FirstOrDefaultAsync();
}
```

**Почему фильтрация по периодам:**
- Без фильтрации статистика накапливается бесконечно
- С фильтрацией можно анализировать конкретные периоды
- Позволяет сравнивать показатели за разные периоды

**Алгоритм генерации отчета:**

```csharp
private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
{
    // 1. Создаем папку для отчетов (если не существует)
    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    string statsFolder = Path.Combine(baseDir, "Stats");
    Directory.CreateDirectory(statsFolder);
    
    // 2. Генерируем имя файла с датой и временем
    string fileName = $"Отчет_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
    
    // 3. Загружаем данные с учетом выбранного периода
    DateTime periodStart = GetPeriodStartDate(_currentPeriod);
    // ... загрузка данных ...
    
    // 4. Формируем содержимое отчета
    var reportContent = new StringBuilder();
    reportContent.AppendLine("═══════════════════════════════════════════════════════");
    reportContent.AppendLine("                    ОТЧЕТ ПО СТАТИСТИКЕ");
    // ... добавляем данные ...
    
    // 5. Сохраняем в файл
    await File.WriteAllTextAsync(filePath, reportContent.ToString(), Encoding.UTF8);
}
```

**Почему StringBuilder:**
- Эффективнее, чем конкатенация строк
- При множественных AppendLine не создает промежуточные строки
- Лучше для производительности при больших отчетах

---

### 8. AddProductView - Форма добавления товара

**Назначение:** Добавление новых товаров в систему (только для Admin/Manager).

**Алгоритм сохранения:**

```csharp
private async void SaveButton_Click(object sender, RoutedEventArgs e)
{
    // 1. Валидация данных
    if (string.IsNullOrWhiteSpace(NameBox.Text) ||
        string.IsNullOrWhiteSpace(DescriptionBox.Text) ||
        !decimal.TryParse(PriceBox.Text, out var price))
    {
        MessageBox.Show("Заполните все поля корректно");
        return;
    }
    
    // 2. Создаем объект Product
    var product = new Product {
        Name = NameBox.Text.Trim(),
        Description = DescriptionBox.Text.Trim(),
        Price = price,
        Category = (CategoryBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Dishes",
        ImagePath = string.IsNullOrWhiteSpace(ImagePathBox.Text)
            ? "Images/Default/Default.png"
            : ImagePathBox.Text.Trim(),
        IsAvailable = true
    };
    
    // 3. Сохраняем в БД
    using var db = new Database();
    db.Products.Add(product);
    await db.SaveChangesAsync();
    
    // 4. Очищаем форму
    NameBox.Clear();
    // ...
}
```

**Почему Trim():**
- Убирает пробелы в начале и конце
- Предотвращает ошибки из-за случайных пробелов
- Улучшает качество данных в БД

---

## Модели данных и база данных

### Entity Framework Core - DbContext

**Класс Database (Models/Database.cs):**

```csharp
public partial class Database : DbContext
{
    // DbSet - это коллекции, представляющие таблицы в БД
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<User> Users { get; set; }
    // ...
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Строка подключения к SQL Server
        optionsBuilder.UseSqlServer(
            "Server=.;Database=Brew_db;Trusted_Connection=True; TrustServerCertificate=True"
        );
    }
}
```

**Принцип работы Entity Framework:**
1. **DbContext** - контекст базы данных, управляет подключением
2. **DbSet<T>** - представление таблицы в БД
3. **LINQ запросы** преобразуются в SQL
4. **Отслеживание изменений** - EF отслеживает изменения объектов
5. **SaveChanges()** - отправляет изменения в БД

**Почему `using var db = new Database()`:**
- `using` автоматически вызывает `Dispose()` при выходе из блока
- Освобождает подключение к БД
- Предотвращает утечки ресурсов

### Модели данных

**Product (Товар):**
```csharp
public partial class Product
{
    public int Id { get; set; }                    // Первичный ключ
    public string Name { get; set; }               // Название
    public string? Category { get; set; }          // Категория (nullable)
    public decimal Price { get; set; }             // Цена
    public string? Description { get; set; }       // Описание (nullable)
    public string ImagePath { get; set; }          // Путь к изображению
    public bool IsAvailable { get; set; }          // Доступность
    
    // Навигационные свойства (связи)
    public virtual ICollection<OrderItem> OrderItems { get; set; }
}
```

**Order (Заказ):**
```csharp
public partial class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }        // Уникальный номер
    public DateTime CreatedAt { get; set; }        // Дата создания
    public string Status { get; set; }            // Статус: "New", "В обработке", и т.д.
    public decimal Total { get; set; }            // Общая сумма
    public int UserId { get; set; }                // Внешний ключ на User
    
    // Навигационные свойства
    public virtual User User { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; }
    
    // Вычисляемое свойство (не сохраняется в БД)
    [NotMapped]
    public decimal TotalPrice { get; set; }
}
```

**User (Пользователь):**
```csharp
public partial class User
{
    public int Id { get; set; }
    public string Login { get; set; }
    public string PasswordHash { get; set; }        // Пароль (в открытом виде!)
    public int Role { get; set; }                  // 1=Admin, 2=Client, 3=Manager
    public bool IsActive { get; set; }
    
    // Навигационное свойство
    public virtual Role RoleNavigation { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
}
```

**Связи между таблицами:**
- User → Orders (один ко многим)
- Order → OrderItems (один ко многим)
- OrderItem → Product (многие к одному)
- User → Role (многие к одному)

---

## Алгоритмы и паттерны проектирования

### 1. Паттерн Observer (Наблюдатель)

**Где используется:**
- `INotifyPropertyChanged` в `CartItem`
- `ObservableCollection` для корзины
- События (`event Action<Product> AddedToCart`)

**Как работает:**
```csharp
// Подписчик
cartItems.CollectionChanged += CartItems_CollectionChanged;

// Когда коллекция изменяется, автоматически вызывается обработчик
private void CartItems_CollectionChanged(...)
{
    UpdateTotal(); // Обновляем сумму
}
```

**Зачем:**
- Автоматическое обновление UI при изменении данных
- Разделение логики и представления
- Реактивное программирование

### 2. Паттерн Repository (упрощенный)

**Где используется:**
- Класс `Database` как репозиторий
- Все операции с БД через DbContext

**Как работает:**
```csharp
using var db = new Database();
var products = db.Products.Where(p => p.IsAvailable).ToList();
```

**Зачем:**
- Инкапсуляция доступа к данным
- Единая точка входа для работы с БД
- Легко тестировать и модифицировать

### 3. Паттерн Factory (упрощенный)

**Где используется:**
- Создание `ProductsUserControl` для каждого товара
- Создание представлений (Views) в MainWindow

**Как работает:**
```csharp
foreach (var product in products)
{
    var control = new ProductsUserControl(product); // Фабрика
    wrapPanel.Children.Add(control);
}
```

### 4. Алгоритм поиска (Linear Search)

**Где используется:**
- Поиск пользователя при авторизации
- Поиск товара в корзине

```csharp
User? user = users.FirstOrDefault(
    u => u.Login == login && u.PasswordHash == password
);
```

**Сложность:** O(n)
**Почему используется:**
- Для небольшого количества данных (до 1000) достаточно быстро
- Простота реализации
- Для больших объемов лучше использовать Dictionary или индексы БД

### 5. Алгоритм группировки и агрегации

**Где используется:**
- Статистика по товарам
- ТОП товаров

```csharp
var topProducts = await db.OrderItems
    .GroupBy(oi => oi.ProductId)              // Группируем по товару
    .Select(g => new { 
        ProductId = g.Key,
        Quantity = g.Sum(x => x.Quantity),    // Суммируем количество
        Revenue = g.Sum(x => x.Quantity * x.Product.Price) // Суммируем выручку
    })
    .OrderByDescending(x => x.Quantity)        // Сортируем по убыванию
    .Take(5)                                   // Берем топ-5
    .ToListAsync();
```

**Как работает:**
1. `GroupBy` - группирует элементы по ключу (ProductId)
2. `Select` - преобразует группы в объекты с агрегированными данными
3. `Sum` - суммирует значения в группе
4. `OrderByDescending` - сортирует по убыванию
5. `Take(5)` - берет первые 5 элементов

**Почему эффективно:**
- Выполняется на стороне БД (SQL GROUP BY)
- Не загружает все данные в память
- Быстро даже для больших объемов

### 6. Алгоритм фильтрации по дате

**Где используется:**
- Статистика по периодам

```csharp
DateTime periodStart = GetPeriodStartDate(period);
var ordersQuery = db.Orders.AsQueryable();

if (period != "AllTime")
    ordersQuery = ordersQuery.Where(o => o.CreatedAt >= periodStart);
```

**Почему AsQueryable:**
- Позволяет строить запрос динамически
- Запрос выполняется только при вызове `ToListAsync()`
- Можно добавлять условия по мере необходимости

### 7. Алгоритм нормализации путей

**Где используется:**
- Загрузка изображений

```csharp
// 1. Убираем начальный слеш
if (imagePath.StartsWith("/"))
    imagePath = imagePath.Substring(1);

// 2. Нормализуем разделители
imagePath = imagePath.Replace('\\', '/');

// 3. Делаем абсолютным
if (!Path.IsPathRooted(imagePath))
    imagePath = Path.Combine(baseDir, imagePath);
```

**Зачем:**
- Поддержка разных форматов путей
- Корректная работа на разных ОС
- Обработка относительных путей

---

## Детальное описание функционала

### Система авторизации

**Алгоритм:**
1. При запуске приложения открывается `LoginWindow`
2. Загружаются все пользователи из БД с их ролями
3. Пользователь вводит логин и пароль
4. Выполняется поиск пользователя в списке
5. При успехе открывается `MainWindow` с данными пользователя
6. При неудаче показывается сообщение об ошибке

**Проблемы безопасности:**
- Пароли хранятся в открытом виде (не хэшируются)
- Все пользователи загружаются в память
- Нет защиты от брутфорса

**Рекомендации для продакшена:**
- Использовать хэширование паролей (BCrypt, Argon2)
- Проверять пароль через запрос к БД, а не загружать всех пользователей
- Добавить ограничение попыток входа

### Система ролей

**Роли:**
- `1` - Admin (Администратор) - полный доступ
- `2` - Client (Клиент) - только покупки
- `3` - Manager (Менеджер) - доступ к отчетам и добавлению товаров

**Реализация:**
```csharp
// Проверка роли при инициализации MainWindow
if (currentuser.Role == 2) // Client
{
    AddButton.Visibility = Visibility.Collapsed; // Скрываем кнопку добавления
}
// StatsButton виден для Role 1 и 3, скрыт для Role 2
```

**Почему по ID, а не по Name:**
- Быстрее (числовое сравнение)
- Не зависит от локализации
- Меньше вероятность ошибок

### Система корзины

**Структура данных:**
- `ObservableCollection<CartItem>` - коллекция товаров в корзине
- `CartItem` - обертка над `Product` с количеством

**Алгоритм добавления:**
1. Пользователь нажимает "Добавить" на карточке товара
2. Вызывается событие `AddedToCart`
3. В `MainWindow.OnProductAdded` проверяется, есть ли товар в корзине
4. Если есть - увеличивается количество
5. Если нет - создается новый `CartItem`

**Почему копия Product:**
- Изоляция от Entity Framework
- Можно изменять количество без влияния на БД
- Предотвращает конфликты отслеживания

**Алгоритм обновления суммы:**
```csharp
private void UpdateTotal()
{
    decimal total = 0;
    foreach (var item in _cartItems)
    {
        if (item != null && item.Product != null)
            total += item.TotalPrice; // TotalPrice = Price * Quantity
    }
    TotalAmountText.Text = $"{total:C}";
}
```

**Почему пересчет при каждом изменении:**
- Простота реализации
- Гарантирует актуальность данных
- Для небольшого количества товаров достаточно быстро

### Система заказов

**Жизненный цикл заказа:**
1. **Создание:** `Status = "New"`
2. **Автоматическое обновление:** каждые 5 секунд статус меняется:
   - "New" → "В обработке" → "Готовится" → "Готов"
3. **Закрытие:** пользователь может закрыть заказ (удалить)

**Алгоритм создания заказа:**
1. Генерация уникального номера: `ORD-YYYYMMDD-GUID`
2. Создание записи `Order` в БД
3. Сохранение для получения `Id`
4. Создание записей `OrderItem` для каждого товара
5. Сохранение всех элементов
6. Очистка корзины

**Почему GUID в номере:**
- Гарантирует уникальность
- Даже при одновременном создании заказов номера не совпадут
- Легко отслеживать заказы

### Система статистики

**Метрики:**
1. **Общее количество заказов** - `COUNT(Orders)`
2. **Общий доход** - `SUM(OrderItems.Quantity * Products.Price)`
3. **Популярный товар** - товар с максимальным количеством продаж

**Алгоритм вычисления дохода:**
```csharp
_totalRevenue = await db.OrderItems
    .Include(oi => oi.Product)  // Загружаем цены товаров
    .SumAsync(oi => oi.Quantity * oi.Product.Price);
```

**Почему Include:**
- Без него EF выполнил бы отдельный запрос для каждого OrderItem
- С Include все данные загружаются одним запросом с JOIN

**Фильтрация по периодам:**
- Позволяет анализировать данные за конкретный период
- Предотвращает накопление статистики
- Удобно для сравнения периодов

### Система отчетов

**Формат отчета:**
- Текстовый файл (.txt)
- Кодировка UTF-8 (поддержка кириллицы)
- Имя файла: `Отчет_YYYY-MM-DD_HH-mm-ss.txt`

**Структура отчета:**
1. Заголовок с датой формирования
2. Период отчета
3. Основные метрики (заказы, доход, популярный товар)
4. Дополнительная информация (товары, пользователи)
5. Заказы по статусам
6. ТОП-5 товаров

**Почему текстовый формат:**
- Простота реализации
- Легко открыть в любом редакторе
- Не требует дополнительных библиотек

---

## Важные технические детали

### Работа с асинхронностью

**Почему async/await:**
- Не блокирует UI поток
- Приложение остается отзывчивым во время операций с БД
- Лучший пользовательский опыт

**Пример:**
```csharp
private async void LoadOrders()
{
    // await не блокирует UI, пока выполняется запрос
    var orders = await db.Orders.ToListAsync();
    OrdersList.ItemsSource = orders;
}
```

### Обработка ошибок

**Стратегия:**
- Try-catch блоки в критических местах
- Глобальные обработчики в `App.xaml.cs`
- Понятные сообщения пользователю

**Почему важно:**
- Предотвращает краш приложения
- Помогает диагностировать проблемы
- Улучшает пользовательский опыт

### Управление ресурсами

**Using statements:**
```csharp
using var db = new Database();
// Автоматически вызывает Dispose() при выходе из блока
```

**Почему важно:**
- Освобождает подключения к БД
- Предотвращает утечки памяти
- Правильное управление ресурсами

---

## Заключение

Проект Brew3 представляет собой WPF-приложение для управления заказами в кафе/ресторане. 

**Основные принципы:**
- Простота и понятность кода
- Использование стандартных паттернов WPF
- Эффективная работа с БД через Entity Framework
- Реактивное обновление UI через ObservableCollection и INotifyPropertyChanged

**Сильные стороны:**
- Четкая структура проекта
- Разделение на модели, представления и контролы
- Обработка ошибок
- Асинхронные операции для отзывчивости UI

**Области для улучшения:**
- Безопасность (хэширование паролей)
- Оптимизация запросов к БД
- Добавление unit-тестов
- Вынос конфигурации (строки подключения) в файлы

Документация создана для понимания архитектуры и принципов работы приложения.

