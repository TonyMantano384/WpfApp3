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
    /// Логика взаимодействия для OrderCompositionWindow.xaml
    /// </summary>
    public partial class OrderCompositionWindow : Window
    {
        private readonly int _orderId;

        public OrderCompositionWindow(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;
            TbHeader.Text = $"Состав заказа №{_orderId}";
            LoadComposition();
        }

        private void LoadComposition()
        {
            var items = new List<CompositionRow>();
            decimal total = 0;

            var today = DateTime.Today;
            var firstDayPrev = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var lastDayPrev = firstDayPrev.AddMonths(1).AddDays(-1);

            using (var conn = Db.GetContext())
            {
                conn.Open();
                string sql = @"
                SELECT oi.IdOrderItem, oi.ProductId, oi.SizeId,
                       p.Name, pr.Name AS Production, s.NumberSize,
                       oi.Quantity, p.Price,
                       (SELECT COUNT(*) FROM OrderItem x
                        JOIN [Order] o2 ON o2.IdOrder = x.OrderId
                        WHERE x.ProductId = p.IdProduct
                          AND o2.DateOrder BETWEEN @d1 AND @d2) AS OrdersPrev
                FROM OrderItem oi
                JOIN Product p ON p.IdProduct = oi.ProductId
                JOIN Production pr ON pr.IdProduction = p.ProductionId
                JOIN Size s ON s.IdSize = oi.SizeId
                WHERE oi.OrderId = @id
                ORDER BY oi.IdOrderItem";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", _orderId);
                    cmd.Parameters.AddWithValue("@d1", firstDayPrev);
                    cmd.Parameters.AddWithValue("@d2", lastDayPrev);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var price = r.GetDecimal(7);
                            var hasPrev = r.GetInt32(8) > 0;
                            var priceDisc = hasPrev ? price : Math.Round(price * 0.75m, 2);
                            var qty = r.GetInt32(6);
                            var sum = priceDisc * qty;
                            total += sum;

                            items.Add(new CompositionRow
                            {
                                IdOrderItem = r.GetInt32(0),
                                ProductId = r.GetInt32(1),
                                SizeId = r.GetInt32(2),
                                Name = r.GetString(3),
                                Production = r.GetString(4),
                                Size = r.GetDecimal(5).ToString("0.0"),
                                Quantity = qty,
                                UnitPrice = priceDisc,
                                Sum = sum
                            });
                        }
                    }
                }
            }
            DgItems.ItemsSource = items;
            TbTotal.Text = $"Итого: {total:C}";
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

                        MessageBox.Show("Позиция удалена.", "Готово",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadComposition();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        MessageBox.Show("Не удалось удалить позицию: " + ex.Message,
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}