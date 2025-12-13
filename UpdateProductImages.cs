using Brew3.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Brew3
{
    /// <summary>
    /// Вспомогательный класс для обновления путей к изображениям товаров
    /// </summary>
    public static class ProductImageUpdater
    {
        /// <summary>
        /// Обновляет пути к изображениям на основе соответствия имен файлов именам товаров
        /// </summary>
        /// <param name="imagesFolderPath">Полный путь к папке с изображениями</param>
        public static void UpdateImagesFromFolder(string imagesFolderPath = @"C:\Users\necsu\source\repos\Brew3\Images\Save")
        {
            if (!Directory.Exists(imagesFolderPath))
            {
                throw new DirectoryNotFoundException($"Папка не найдена: {imagesFolderPath}");
            }

            using var db = new Database();
            var products = db.Products.ToList();
            var imageFiles = Directory.GetFiles(imagesFolderPath, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int updatedCount = 0;

            foreach (var product in products)
            {
                // Ищем файл, имя которого совпадает с именем товара (без учета регистра)
                var matchingFile = imageFiles.FirstOrDefault(f =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(f);
                    return product.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                           product.Name.Contains(fileName, StringComparison.OrdinalIgnoreCase) ||
                           fileName.Contains(product.Name, StringComparison.OrdinalIgnoreCase);
                });

                if (matchingFile != null)
                {
                    string relativePath = $"Images/Save/{Path.GetFileName(matchingFile)}";
                    product.ImagePath = relativePath;
                    updatedCount++;
                }
            }

            db.SaveChanges();
            Console.WriteLine($"Обновлено {updatedCount} из {products.Count} товаров.");
        }

        /// <summary>
        /// Обновляет путь к изображению для конкретного товара по ID
        /// </summary>
        public static void UpdateImageById(int productId, string imageFileName)
        {
            using var db = new Database();
            var product = db.Products.Find(productId);
            
            if (product != null)
            {
                product.ImagePath = $"Images/Save/{imageFileName}";
                db.SaveChanges();
                Console.WriteLine($"Изображение обновлено для товара: {product.Name}");
            }
            else
            {
                throw new Exception($"Товар с ID {productId} не найден");
            }
        }

        /// <summary>
        /// Обновляет путь к изображению для конкретного товара по имени
        /// </summary>
        public static void UpdateImageByName(string productName, string imageFileName)
        {
            using var db = new Database();
            var product = db.Products.FirstOrDefault(p => p.Name == productName);
            
            if (product != null)
            {
                product.ImagePath = $"Images/Save/{imageFileName}";
                db.SaveChanges();
                Console.WriteLine($"Изображение обновлено для товара: {product.Name}");
            }
            else
            {
                throw new Exception($"Товар с названием '{productName}' не найден");
            }
        }

        /// <summary>
        /// Показывает список всех товаров и доступных изображений для ручного сопоставления
        /// </summary>
        public static void ShowMappingInfo(string imagesFolderPath = @"C:\Users\necsu\source\repos\Brew3\Images\Save")
        {
            using var db = new Database();
            var products = db.Products.ToList();
            
            Console.WriteLine("\n=== ТОВАРЫ В БАЗЕ ДАННЫХ ===");
            foreach (var product in products)
            {
                Console.WriteLine($"ID: {product.Id}, Название: {product.Name}, Текущий путь: {product.ImagePath ?? "NULL"}");
            }

            if (Directory.Exists(imagesFolderPath))
            {
                var imageFiles = Directory.GetFiles(imagesFolderPath, "*.*")
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                Console.WriteLine("\n=== ДОСТУПНЫЕ ИЗОБРАЖЕНИЯ ===");
                foreach (var imageFile in imageFiles)
                {
                    Console.WriteLine($"  {imageFile}");
                }
            }
            else
            {
                Console.WriteLine($"\nПапка с изображениями не найдена: {imagesFolderPath}");
            }
        }
    }
}

