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
    /// Логика взаимодействия для OrderEditWindow.xaml
    /// </summary>
    public partial class OrderEditWindow : Window
    {
        private readonly int _orderId;

        public OrderEditWindow(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;
            TbHeader.Text = $"Редактирование заказа №{_orderId}";
            LoadData();
        }

        private void LoadData()
        {
            using (var conn = Db.GetContext())
            {
                conn.Open();

                using (var cmd = new SqlCommand(
                    "SELECT DateOrder FROM [Order] WHERE IdOrder=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _orderId);
                    DpDate.SelectedDate = (DateTime)cmd.ExecuteScalar();
                }

                var list = new List<CompositionRow>();
                using (var cmd = new SqlCommand(@"
                SELECT oi.IdOrderItem, oi.ProductId, oi.SizeId,
                       p.Name, s.NumberSize, oi.Quantity
                FROM OrderItem oi
                JOIN Product p ON p.IdProduct = oi.ProductId
                JOIN Size s ON s.IdSize = oi.SizeId
                WHERE oi.OrderId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _orderId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new CompositionRow
                            {
                                IdOrderItem = r.GetInt32(0),
                                ProductId = r.GetInt32(1),
                                SizeId = r.GetInt32(2),
                                Name = r.GetString(3),
                                Size = r.GetDecimal(4).ToString("0.0"),
                                Quantity = r.GetInt32(5)
                            });
                        }
                    }
                }
                DgItems.ItemsSource = list;
            }
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgItems.SelectedItem is CompositionRow row))
            {
                MessageBox.Show("Выберите позицию для удаления.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show("Удалить позицию из заказа?",
                                "Подтверждение", MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            using (var conn = Db.GetContext())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE StockProductItem SET Quantity = Quantity + @q " +
                            "WHERE ProductId=@p AND SizeId=@s", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@q", row.Quantity);
                            cmd.Parameters.AddWithValue("@p", row.ProductId);
                            cmd.Parameters.AddWithValue("@s", row.SizeId);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SqlCommand(
                            "DELETE FROM OrderItem WHERE IdOrderItem=@id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", row.IdOrderItem);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        MessageBox.Show("Не удалось удалить позицию: " + ex.Message,
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
            }   }
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DpDate.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату заказа.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (DpDate.SelectedDate > DateTime.Today)
            {
                MessageBox.Show("Дата заказа не может быть в будущем.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var conn = Db.GetContext())
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE [Order] SET DateOrder=@d WHERE IdOrder=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@d", DpDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@id", _orderId);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Изменения сохранены.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
