using Brew3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Brew3.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private string login = string.Empty;
        private string password = string.Empty; 
        private Database db = new Database();
        List<User> users = new List<User>();

        public LoginWindow()
        {
            InitializeComponent();
            try
            {
                users = db.Users.Include(user => user.RoleNavigation).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                users = new List<User>();
            }
        }
        


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            login = LoginBox.Text;
            password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите пароль");
                return;
            }


            User? user = users.FirstOrDefault(u => u.Login == login && u.PasswordHash == password);

            if (user != null)
            {
                try
                {
                    MainWindow mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии главного окна: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ErrorMessage.Visibility = Visibility.Visible;
            }

        }

    }
}
