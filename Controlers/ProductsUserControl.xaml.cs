using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Brew3.Models;
using Microsoft.EntityFrameworkCore;

namespace Brew3.Controlers
{
    public partial class ProductsUserControl : UserControl
    {
        // ✅ Событие с возможным значением null
        public event Action<Product>? AddedToCart;
        private string projectPath = string.Empty;
        string productPath = string.Empty;

        public ProductsUserControl(Product product)
        {
            InitializeComponent();
            DataContext = product;
            projectPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LoadImage(product);
            Uri uri = new Uri(productPath);
            BitmapImage bitmapImage = new BitmapImage(uri);
            BoxImage.Source = bitmapImage;
        }

        private void LoadImage(Product product)
        {
            try
            {
                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    productPath = Path.Combine(projectPath, "Images", "Save", product.ImagePath);
                }
                else
                {
                    productPath = Path.Combine(productPath, "Images", "Default", "Default.png");
                }
            }
            catch
            {
                productPath = Path.Combine(productPath, "Images", "Default", "Default.png");
            }
        }

    

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var product = DataContext as Product;
            if (product != null)
            {
                AddedToCart?.Invoke(product);
            }
        }
    }
}