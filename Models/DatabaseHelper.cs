using Microsoft.EntityFrameworkCore;

namespace Brew3.Models
{
    public static class DatabaseHelper
    {
        /// <summary>
        /// Обновляет ImagePath для всех товаров, у которых ImagePath равен NULL
        /// </summary>
        public static void UpdateDefaultImagePaths()
        {
            using var db = new Database();
            
            var productsWithoutImage = db.Products
                .Where(p => string.IsNullOrEmpty(p.ImagePath))
                .ToList();

            foreach (var product in productsWithoutImage)
            {
                product.ImagePath = "Images/Default/Default.png";
            }

            db.SaveChanges();
        }

        /// <summary>
        /// Обновляет ImagePath для товаров по категориям
        /// </summary>
        public static void UpdateImagePathsByCategory()
        {
            using var db = new Database();
            
            var products = db.Products.ToList();

            foreach (var product in products)
            {
                if (string.IsNullOrEmpty(product.ImagePath))
                {
                    // Устанавливаем путь в зависимости от категории
                    product.ImagePath = product.Category switch
                    {
                        "Dishes" => "Images/Default/Default.png",
                        "Drinks" => "Images/Default/Default.png",
                        "Desserts" => "Images/Default/Default.png",
                        _ => "Images/Default/Default.png"
                    };
                }
            }

            db.SaveChanges();
        }
    }
}

