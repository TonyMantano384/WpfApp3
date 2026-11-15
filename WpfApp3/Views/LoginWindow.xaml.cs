using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
            public LoginWindow() => InitializeComponent();

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин для входа в систему.",
                                "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var conn = Db.GetContext())
                {
                    conn.Open();
                    string sql = "SELECT u.IdUser, u.Surname, u.Name, u.Patronymic, u.RoleId FROM [User] u WHERE u.Login = @login";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Пользователь с таким логином не найден.\n" +
                                                "Проверьте правильность ввода или обратитесь к администратору.",
                                                "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            Session.CurrentUser = new User
                            {
                                IdUser = reader.GetInt32(0),
                                Surname = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Patronymic = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Login = login,
                                RoleId = reader.GetInt32(4)
                            };
                            var main = new MainWindow();
                            main.Show();
                            Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message + "\n\nГде: " + ex.StackTrace,
                                    "Подробная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
       
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
