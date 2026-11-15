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
using System.Windows.Navigation;
using System.IO;
using WpfApp3.Models;
using WpfApp3.Services;
using WpfApp3.Views;


namespace WpfApp3
{
    public partial class MainWindow : Window
    {
        private List<ProductViewModel> _allProducts = new List<ProductViewModel>();

        public MainWindow()
        {
            InitializeComponent();
            if (Session.CurrentUser != null)
            {
                TbUserFio.Text = $"{Session.CurrentUser.Surname} " +
                    $"{Session.CurrentUser.Name} " +
                    $"{Session.CurrentUser.Patronymic}";

                if (Session.CurrentUser.RoleId == 1 ||
                    Session.CurrentUser.RoleId == 3)
                {
                    BtnOrders.Visibility = Visibility.Visible;
                }
            }
            else
                {
                    TbUserFio.Text = "Гость";
                }

                LoadCategories();
                LoadProducts();
            }
        

        private void LoadCategories()
        {
            CbCategory.Items.Clear();
            CbCategory.Items.Add("Все категории");

            using (var conn = Db.GetContext())
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT Name FROM Category ORDER BY Name", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) CbCategory.Items.Add(r.GetString(0));
                }
            }
            CbCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            _allProducts.Clear();
            var calcDate = DateTime.Today;
            var firstDayPrev = new DateTime(calcDate.Year, calcDate.Month, 1).AddMonths(-1);
            var lastDayPrev = firstDayPrev.AddMonths(1).AddDays(-1);

            using (var conn = Db.GetContext())
            {
                conn.Open();
                string sql = @"
                    SELECT p.IdProduct, p.Name, p.Image, p.Material, p.Price, p.Description,
                           c.Name AS CategoryName,
                           pr.Name AS ProductionName,
                           ISNULL((SELECT SUM(s.Quantity) FROM StockProductItem s
                                   WHERE s.ProductId = p.IdProduct), 0) AS TotalQty,
                           (SELECT COUNT(*) FROM OrderItem oi
                            JOIN [Order] o ON o.IdOrder = oi.OrderId
                            WHERE oi.ProductId = p.IdProduct
                              AND o.DateOrder BETWEEN @d1 AND @d2) AS OrdersPrev
                    FROM Product p
                    JOIN Category c ON c.IdCategory = p.CategoryId
                    JOIN Production pr ON pr.IdProduction = p.ProductionId
                    ORDER BY p.IdProduct";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@d1", firstDayPrev);
                    cmd.Parameters.AddWithValue("@d2", lastDayPrev);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var p = new ProductViewModel();
                            p.IdProduct = r.GetInt32(0);
                            p.Name = r.GetString(1);
                            p.Image = r.GetString(2);
                            p.Material = r.GetString(3);
                            p.Price = r.GetDecimal(4);
                            p.Description = r.GetString(5);
                            p.CategoryName = r.GetString(6);
                            p.ProductionName = r.GetString(7);
                            p.TotalQuantity = r.GetInt32(8);

                            var hasOrdersPrev = r.GetInt32(9) > 0;
                            p.PriceWithDiscount = hasOrdersPrev
                                ? p.Price
                                : Math.Round(p.Price * 0.75m, 2);

                            string resourcePath = $"pack://application:,,,/Resources/Images/{p.Image}";

                            try
                            {
                                var uri = new Uri(resourcePath, UriKind.Absolute);
                                var bitmap = new System.Windows.Media.Imaging.BitmapImage(uri);
                                p.ImagePath = resourcePath;
                            }
                            catch
                            {
                                p.ImagePath = "pack://application:,,,/Resources/picture.png";
                            }

                            _allProducts.Add(p);
                        }
                    }
                 }
            }
            
            ApplyFilterAndSearch();
        }

        private IEnumerable<ProductViewModel> ApplyFilter(IEnumerable<ProductViewModel> src)
        {
            if (src == null) return new List<ProductViewModel>();
            if (CbCategory == null || CbCategory.SelectedIndex <= 0) return src;
            var cat = CbCategory.SelectedItem?.ToString();
            return src.Where(p => p.CategoryName == cat);
        }

        private IEnumerable<ProductViewModel> ApplySearch(IEnumerable<ProductViewModel> src)
        {
            
            if (src == null) return new List<ProductViewModel>();
            if (TbSearch == null) return src;

            var q = TbSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(q)) return src;

            return src.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(q)) ||
                (p.Description != null && p.Description.ToLower().Contains(q)));
        }

        private IEnumerable<ProductViewModel> ApplySort(IEnumerable<ProductViewModel> src)
        {
            switch (CbSort.SelectedIndex)
            {
                case 1: return src.OrderBy(p => p.PriceWithDiscount);
                case 2: return src.OrderByDescending(p => p.PriceWithDiscount);
                default: return src;
            }
        }

        private void ApplyFilterAndSearch()
        {
            if (LvProducts == null) return;

            IEnumerable<ProductViewModel> result = _allProducts;
            result = ApplyFilter(result);
            result = ApplySearch(result);
            result = ApplySort(result);

            LvProducts.ItemsSource = result.ToList();
        }

        private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { ApplyFilterAndSearch(); }

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { ApplyFilterAndSearch(); }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        { ApplyFilterAndSearch(); }

        private void LvProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(LvProducts.SelectedItem is ProductViewModel p)) return;

            var view = new ProductViewWindow(p.IdProduct);
            view.Owner = this;
            view.ShowDialog();
            LvProducts.SelectedItem = null;
            LoadProducts();
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            var w = new OrdersWindow();
            w.Owner = this;
            w.ShowDialog();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.CurrentUser = null;
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}