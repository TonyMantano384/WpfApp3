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
using System.IO;
using System.Windows.Media.Imaging;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.Views
{
    /// <summary>
    /// Логика взаимодействия для ProductViewWindow.xaml
    /// </summary>
    public partial class ProductViewWindow : Window
    {
        private readonly int _productId;
        private readonly List<CartItem> _cart = new List<CartItem>();
        private decimal _priceDisc;

        public ProductViewWindow(int productId)
        {
            InitializeComponent();
            _productId = productId;
            LoadProduct();
            LoadSizes();
        }

        private void LoadProduct()
        {

            var calcDate = DateTime.Today;
            var firstDayPrev = new DateTime(calcDate.Year, calcDate.Month, 1).AddMonths(-1);
            var lastDayPrev = firstDayPrev.AddMonths(1).AddDays(-1);

            using (var conn = Db.GetContext())
            {
                conn.Open();
                string sql = @"
                    SELECT p.Name, p.Image, p.Material, p.Price, p.Description,
                           c.Name AS CategoryName, pr.Name AS ProductionName,
                           (SELECT COUNT(*) FROM OrderItem oi
                            JOIN [Order] o ON o.IdOrder = oi.OrderId
                            WHERE oi.ProductId = p.IdProduct
                              AND o.DateOrder BETWEEN @d1 AND @d2) AS OrdersPrev
                    FROM Product p
                    JOIN Category c ON c.IdCategory = p.CategoryId
                    JOIN Production pr ON pr.IdProduction = p.ProductionId
                    WHERE p.IdProduct = @id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", _productId);
                    cmd.Parameters.AddWithValue("@d1", firstDayPrev);
                    cmd.Parameters.AddWithValue("@d2", lastDayPrev);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return;

                        TbName.Text = r.GetString(0);
                        var image = r.GetString(1);
                        TbMaterial.Text = r.GetString(2);
                        var price = r.GetDecimal(3);
                        TbDescription.Text = r.GetString(4);
                        TbCategory.Text = "Категория: " + r.GetString(5);
                        TbProduction.Text = "Производство: " + r.GetString(6);

                        var hasPrevOrders = r.GetInt32(7) > 0;
                        _priceDisc = hasPrevOrders ? price : Math.Round(price * 0.75m, 2);

                        TbPriceDisc.Text = _priceDisc.ToString("C");
                        if (_priceDisc < price)
                        {
                            TbPriceOld.Text = price.ToString("C");
                            TbPriceOld.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            TbPriceOld.Visibility = Visibility.Collapsed;
                        }

                        var imgPath = System.IO.Path.Combine("Resources/Images", image);
                        ImgProduct.Source = new BitmapImage(new Uri(
                            File.Exists(imgPath)
                                ? "pack://application:,,,/Resources/Images/" + image
                                : "pack://application:,,,/Resources/picture.png"));
                    }
                }
            }
        }

        private void LoadSizes()
        {
            var sizes = new List<SizeItem>();
            using (var conn = Db.GetContext())
            {
                conn.Open();
                string sql = @"
                    SELECT s.IdSize, s.NumberSize, sp.Quantity
                    FROM StockProductItem sp
                    JOIN Size s ON s.IdSize = sp.SizeId
                    WHERE sp.ProductId = @id AND sp.Quantity > 0
                    ORDER BY s.NumberSize";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", _productId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            sizes.Add(new SizeItem
                            {
                                IdSize = r.GetInt32(0),
                                NumberSize = r.GetDecimal(1),
                                Available = r.GetInt32(2)
                            });
                        }
                    }
                }
            }
            CbSize.ItemsSource = sizes;
            CbSize.DisplayMemberPath = "Display";
            CbSize.SelectedIndex = sizes.Count > 0 ? 0 : -1;
        }

        private void BtnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (!(CbSize.SelectedItem is SizeItem size))
            {
                MessageBox.Show("Не выбран размер обуви. Выберите размер из списка перед добавлением в заказ.",
                                "Ошибка оформления", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int qty;
            if (!int.TryParse(TbQuantity.Text, out qty) || qty <= 0)
            {
                MessageBox.Show("Некорректное количество. Введите целое число больше нуля.",
                                "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (qty > size.Available)
            {
                MessageBox.Show(
                    string.Format("Недостаточно пар размера {0:0.0}. Доступно: {1}.",
                                  size.NumberSize, size.Available),
                    "Ошибка оформления", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var existing = _cart.FirstOrDefault(c => c.SizeId == size.IdSize);
            if (existing != null) existing.Quantity += qty;
            else _cart.Add(new CartItem { ProductId = _productId, SizeId = size.IdSize, Quantity = qty });

            MessageBox.Show("Товар добавлен в заказ.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("Заказ пуст. Добавьте хотя бы одну товарную позицию.",
                                "Ошибка оформления", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Для оформления заказа требуется авторизация.",
                                "Требуется вход", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Подтвердить оформление заказа?", "Подтверждение",
                                MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            using (var conn = Db.GetContext())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int orderId;
                        using (var cmd = new SqlCommand(
                            "INSERT INTO [Order] (DateOrder, UserId) OUTPUT INSERTED.IdOrder VALUES (@d, @u)",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@d", DateTime.Today);
                            cmd.Parameters.AddWithValue("@u", Session.CurrentUser.IdUser);
                            orderId = (int)cmd.ExecuteScalar();
                        }

                        foreach (var item in _cart)
                        {
                            using (var cmd = new SqlCommand(
                                "INSERT INTO OrderItem (OrderId, ProductId, SizeId, Quantity) VALUES (@o, @p, @s, @q)",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@o", orderId);
                                cmd.Parameters.AddWithValue("@p", item.ProductId);
                                cmd.Parameters.AddWithValue("@s", item.SizeId);
                                cmd.Parameters.AddWithValue("@q", item.Quantity);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new SqlCommand(
                                "UPDATE StockProductItem SET Quantity = Quantity - @q WHERE ProductId=@p AND SizeId=@s",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@q", item.Quantity);
                                cmd.Parameters.AddWithValue("@p", item.ProductId);
                                cmd.Parameters.AddWithValue("@s", item.SizeId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        MessageBox.Show(
                            string.Format("Заказ №{0} оформлен {1:dd.MM.yyyy}.", orderId, DateTime.Today),
                            "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        _cart.Clear();
                        Close();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        MessageBox.Show("Не удалось оформить заказ: " + ex.Message,
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}