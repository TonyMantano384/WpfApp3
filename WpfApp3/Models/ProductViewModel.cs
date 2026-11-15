using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApp3.Models
{
    public class ProductViewModel
    {
        public int IdProduct { get; set; }
        public string Name { get; set; } = "";
        public string Image { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string Material { get; set; } = "";
        public string Description { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string ProductionName { get; set; } = "";
        public decimal Price { get; set; }
        public decimal PriceWithDiscount { get; set; }
        public int TotalQuantity { get; set; }

        public decimal OldPrice => Price;
        public bool HasDiscount => PriceWithDiscount < Price;
        public bool IsLowStock => TotalQuantity <= 3;

        public string QuantityText =>
            TotalQuantity > 5 ? "Количество: много" : "Количество: мало";

        public Brush RowBackground =>
            TotalQuantity <= 3
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x80))
                : Brushes.White;
    }

    public class SizeItem
    {
        public int IdSize { get; set; }
        public decimal NumberSize { get; set; }
        public int Available { get; set; }
        public string Display => $"{NumberSize:0.0} (доступно: {Available})";
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderRow
    {
        public int IdOrder { get; set; }
        public DateTime DateOrder { get; set; }
        public string Fio { get; set; } = "";
    }

    public class CompositionRow
    {
        public int IdOrderItem { get; set; }
        public int ProductId { get; set; }
        public int SizeId { get; set; }
        public string Name { get; set; } = "";
        public string Production { get; set; } = "";
        public string Size { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Sum { get; set; }
    }
}