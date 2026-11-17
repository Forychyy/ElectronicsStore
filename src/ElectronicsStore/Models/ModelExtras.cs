namespace ElectronicsStore.Models;

// Дополнения к классам, созданным scaffold. Файлы моделей не правим:
// при повторном scaffold они перезапишутся, а этот файл останется.

public partial class User
{
    // ФИО одной строкой: «Иванов Алексей Петрович».
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    // Короткая форма: «Иванов А. П.»
    public string ShortName =>
        $"{LastName} {FirstName[..1]}." + (string.IsNullOrEmpty(MiddleName) ? "" : $" {MiddleName[..1]}.");

    public string RoleName => Role?.RoleName ?? "";

    // ToString решает, как объект выглядит в ComboBox и ListBox.
    public override string ToString() => $"{Login} — {FullName}";
}

public partial class Role
{
    public override string ToString() => RoleName;
}

public partial class Category
{
    public override string ToString() => CategoryName;
}

public partial class Department
{
    public override string ToString() => DepartmentName;
}

public partial class Product
{
    // Сумма остатков всех партий товара.
    public int TotalStock => ProductVariants.Sum(v => v.AvailableStock);

    public override string ToString() => $"{ProductCode} · {ProductName}";
}

public partial class ProductVariant
{
    // Как партия выглядит в выпадающем списке: «Партия 01 · чёрный (в наличии 6 шт.)»
    public override string ToString() => $"{VariantName} (в наличии {AvailableStock} шт.)";
}

public partial class OrderItem
{
    // Сумма позиции: количество × цена покупки.
    public decimal Sum => Quantity * UnitPrice;

    public string ProductName => Variant?.Product?.ProductName ?? "Товар удалён";
    public string VariantName => Variant?.VariantName ?? "";
}

public partial class Order
{
    // Итог заказа — сумма всех позиций.
    public decimal Total => OrderItems.Sum(i => i.Sum);

    // Количество единиц товара в заказе.
    public int UnitsCount => OrderItems.Sum(i => i.Quantity);

    public string ClientName => Client?.FullName ?? "";

    // Краткий состав для списка заказов: «Смартфон Nova X5 ×1, Наушники SoundAir Pro ×1»
    public string ItemsText => string.Join(", ", OrderItems.Select(i => $"{i.ProductName} ×{i.Quantity}"));
}
