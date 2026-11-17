using Avalonia.Controls;
using Avalonia.Interactivity;
using ElectronicsStore.Models;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class ProductView : UserControl
{
    // Какой товар открыть — задаёт каталог: new ProductView { ProductId = ... }
    public int ProductId { get; set; }

    private Product? product;
    private decimal unitPrice;     // цена, которую видит пользователь (проверяется при сохранении)
    private bool priceReady;       // цена для выбранного клиента рассчитана

    public ProductView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            product = await CatalogService.GetProductAsync(ProductId);
            if (product == null)
            {
                MainWindow.Instance.ShowWarning("Товар не найден. Обновите каталог.");
                MainWindow.Instance.GoBack();
                return;
            }

            ProductImage.Source = UiSupport.Picture(product.ImagePath);
            CategoryText.Text = product.Category.CategoryName;
            CodeText.Text = "Артикул " + product.ProductCode;
            NameText.Text = product.ProductName;
            DepartmentText.Text = "Отдел: " + product.Department.DepartmentName;
            ContentText.Text = product.ProductContent ?? "Не указаны";
            DescriptionText.Text = product.Description ?? "Описание отсутствует";
            BasePriceText.Text = UiSupport.Money(product.BasePrice);

            // Предлагаются только партии, где остаток строго больше нуля.
            var variants = product.ProductVariants
                .Where(v => v.AvailableStock > 0)
                .OrderBy(v => v.VariantName)
                .ToList();
            VariantBox.ItemsSource = variants;
            VariantBox.SelectedIndex = variants.Count > 0 ? 0 : -1;
            NoStockText.IsVisible = variants.Count == 0;

            // Менеджер и администратор выбирают покупателя; клиент оформляет заказ на себя.
            if (Session.IsStaff)
            {
                int? keep = (ClientBox.SelectedItem as User)?.UserId; // сохранить выбор при перечитывании
                var clients = await UserService.GetClientsAsync();
                ClientPanel.IsVisible = true;
                ClientBox.ItemsSource = clients;
                ClientBox.SelectedItem = clients.FirstOrDefault(c => c.UserId == keep);
            }

            await UpdatePriceAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Клиент, для которого считается цена: выбранный в списке или текущий пользователь.
    private int? SelectedClientId =>
        Session.IsStaff ? (ClientBox.SelectedItem as User)?.UserId : Session.CurrentUser?.UserId;

    // Цена зависит от клиента: скидка 25 %, если в прошлом месяце он не покупал товары этой категории.
    private async Task UpdatePriceAsync()
    {
        priceReady = false;
        if (product == null) return;

        int? clientId = SelectedClientId;
        if (clientId == null)
        {
            UnitPriceText.Text = "выберите покупателя";
            DiscountText.IsVisible = false;
            UpdateTotal();
            return;
        }

        unitPrice = await CatalogService.GetPriceAsync(ProductId, clientId);
        priceReady = true;

        UnitPriceText.Text = UiSupport.Money(unitPrice);
        DiscountText.IsVisible = unitPrice < product.BasePrice;
        DiscountText.Text = $"Скидка {PriceRules.Discount:0} %: в прошлом месяце не было покупок в категории «{product.Category.CategoryName}»";
        UpdateTotal();
    }

    // Количество — строго целое положительное число.
    private bool TryGetQuantity(out int quantity, out string error)
    {
        error = "";
        if (!int.TryParse((QuantityBox.Text ?? "").Trim(), out quantity) || quantity <= 0)
        {
            error = "Введите целое число больше нуля.";
            return false;
        }
        if (VariantBox.SelectedItem is ProductVariant v && quantity > v.AvailableStock)
        {
            error = $"В выбранной партии только {v.AvailableStock} шт.";
            return false;
        }
        return true;
    }

    private void UpdateTotal()
    {
        // События TextChanged/SelectionChanged могут прийти ещё во время InitializeComponent.
        if (OrderButton == null || QuantityError == null) return;

        bool ok = TryGetQuantity(out int quantity, out string error);
        QuantityError.Text = error;
        QuantityError.IsVisible = !ok && !string.IsNullOrEmpty(QuantityBox.Text);

        TotalText.Text = ok && priceReady ? UiSupport.Money(PriceRules.Total(unitPrice, quantity)) : "—";
        OrderButton.IsEnabled = ok && priceReady && VariantBox.SelectedItem != null;
    }

    private void QuantityChanged(object? sender, TextChangedEventArgs e) => UpdateTotal();

    private void InputChanged(object? sender, SelectionChangedEventArgs e) => UpdateTotal();

    private async void ClientChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            await UpdatePriceAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // ОФОРМЛЕНИЕ: сервис заново проверяет права, цену и остаток в транзакции.
    private async void OrderClick(object? sender, RoutedEventArgs e)
    {
        if (VariantBox.SelectedItem is not ProductVariant variant || SelectedClientId is not int clientId)
        {
            MainWindow.Instance.ShowWarning("Выберите покупателя и партию товара.");
            return;
        }
        if (!TryGetQuantity(out int quantity, out string error))
        {
            MainWindow.Instance.ShowWarning(error);
            return;
        }

        OrderButton.IsEnabled = false;
        try
        {
            int orderId = await OrderService.CreateOrderAsync(variant.VariantId, quantity, clientId, unitPrice);
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            MainWindow.Instance.ShowMessage($"Заказ № {orderId} оформлен на сумму {UiSupport.Money(PriceRules.Total(unitPrice, quantity))}.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
            await LoadAsync(); // остаток или цена могли измениться — перечитываем
        }
    }

    // «Назад» до сохранения ничего не записывает в БД.
    private void BackClick(object? sender, RoutedEventArgs e) =>
        MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
}
