using Brew3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Brew3.Views
{
    public partial class AddProductView : UserControl
    {
        public AddProductView()
        {
            InitializeComponent();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) ||
                string.IsNullOrWhiteSpace(DescriptionBox.Text) ||
                !decimal.TryParse(PriceBox.Text, out var price))
            {
                MessageBox.Show("Заполните название, описание и корректную цену.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var product = new Product
            {
                Name = NameBox.Text.Trim(),
                Description = DescriptionBox.Text.Trim(),
                Price = price,
                Category = (CategoryBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Dishes",
                ImagePath = string.IsNullOrWhiteSpace(ImagePathBox.Text)
                    ? "Images/Default/Default.png"
                    : ImagePathBox.Text.Trim(),
                IsAvailable = true
            };

            using var db = new Database();
            db.Products.Add(product);
            await db.SaveChangesAsync();

            MessageBox.Show($"Товар «{product.Name}» добавлен!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Сброс формы
            NameBox.Clear(); DescriptionBox.Clear(); PriceBox.Clear(); ImagePathBox.Clear();
            CategoryBox.SelectedIndex = 0;
        }

        private void UpdateImagePathsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DatabaseHelper.UpdateDefaultImagePaths();
                MessageBox.Show("Изображения для всех товаров обновлены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении изображений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}