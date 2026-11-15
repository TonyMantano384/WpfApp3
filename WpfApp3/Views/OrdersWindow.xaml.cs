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
using System.Windows.Shapes;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.Views
{
    /// <summary>
    /// Логика взаимодействия для OrdersWindow.xaml
    /// </summary>
    public partial class OrdersWindow : Window
    {
        public OrdersWindow()
        {
            InitializeComponent();

            if (Session.CurrentUser?.RoleId == 1)
                BtnEdit.Visibility = Visibility.Visible;

            LoadOrders();
        }

        private void LoadOrders()
        {
            var list = new List<OrderRow>();
            using (var conn = Db.GetContext())
            {
                conn.Open();
                string sql = @"
                SELECT o.IdOrder, o.DateOrder,
                       u.Surname + ' ' + u.Name + ' ' + u.Patronymic AS Fio
                FROM [Order] o
                JOIN [User] u ON u.IdUser = o.UserId
                ORDER BY o.DateOrder DESC, o.IdOrder DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new OrderRow
                            {
                                IdOrder = r.GetInt32(0),
                                DateOrder = r.GetDateTime(1),
                                Fio = r.GetString(2)
                            });
                        }
                        DgOrders.ItemsSource = list;
                    }
                }
            }
        }
        private void BtnComposition_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgOrders.SelectedItem is OrderRow row))
            {
                MessageBox.Show("Выберите заказ из списка для просмотра состава.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var w = new OrderCompositionWindow(row.IdOrder) { Owner = this };
            w.ShowDialog();
            LoadOrders();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgOrders.SelectedItem is OrderRow row))
            {
                MessageBox.Show("Выберите заказ для редактирования.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var w = new OrderEditWindow(row.IdOrder) { Owner = this };
            w.ShowDialog();
            LoadOrders();
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgOrders.SelectedItem is OrderRow row))
            {
                MessageBox.Show("Выберите заказ для удаления.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var res = MessageBox.Show(
                $"Удалить заказ №{row.IdOrder} от {row.DateOrder:dd.MM.yyyy}?\n" +
                "Все товарные позиции заказа также будут удалены.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            using (var conn = Db.GetContext())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {

                    // Возврат остатков на склад перед удалением заказа
                    using (var cmd = new SqlCommand(@"
                UPDATE sp SET sp.Quantity = sp.Quantity + oi.Quantity
                FROM StockProductItem sp
                JOIN OrderItem oi ON oi.ProductId = sp.ProductId AND oi.SizeId = sp.SizeId
                WHERE oi.OrderId = @id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", row.IdOrder);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SqlCommand("DELETE FROM [Order] WHERE IdOrder=@id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", row.IdOrder);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();

                    MessageBox.Show("Заказ удалён.", "Готово",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrders();
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}