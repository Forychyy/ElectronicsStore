using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ElectronicsStore.Models;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class ProductsView : UserControl
{
    private List<Product> allProducts = new();
    private Product? current; // выбранный товар; null — создаётся новый

    public ProductsView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync(null);
    }

    // Загрузка справочников и списка товаров. selectId — какой товар выделить после загрузки.
    private async Task LoadAsync(int? selectId)
    {
        try
        {
            if (CategoryBox.ItemsSource == null)
            {
                CategoryBox.ItemsSource = await CatalogService.GetCategoriesAsync();
                DepartmentBox.ItemsSource = await CatalogService.GetDepartmentsAsync();
            }

            allProducts = await ProductService.GetProductsAsync();
            ApplyFilter();

            Product? toSelect = allProducts.FirstOrDefault(p => p.ProductId == selectId);
            if (toSelect != null)
                ProductsList.SelectedItem = toSelect;
            else
                ShowProduct(null);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void ApplyFilter()
    {
        string text = (SearchBox.Text ?? "").Trim();
        var ic = StringComparison.OrdinalIgnoreCase;
        ProductsList.ItemsSource = allProducts
            .Where(p => text == "" || p.ProductName.Contains(text, ic) || p.ProductCode.Contains(text, ic))
            .ToList();
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (ProductsList != null) ApplyFilter();
    }

    private void ProductSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (ProductsList.SelectedItem is Product p)
            ShowProduct(p);
    }

    private void NewClick(object? sender, RoutedEventArgs e)
    {
        ProductsList.SelectedItem = null;
        ShowProduct(null);
    }

    // Заполнить форму данными товара (или очистить для нового).
    private void ShowProduct(Product? p)
    {
        current = p;
        FormTitle.Text = p == null ? "Новый товар" : $"Товар {p.ProductCode}";
        CodeBox.Text = p?.ProductCode ?? "";
        NameBox.Text = p?.ProductName ?? "";
        PriceBox.Text = p == null ? "" : p.BasePrice.ToString("0.00");
        ContentBox.Text = p?.ProductContent ?? "";
        DescriptionBox.Text = p?.Description ?? "";
        ImageBox.Text = p?.ImagePath ?? "";

        var categories = (List<Category>)CategoryBox.ItemsSource!;
        var departments = (List<Department>)DepartmentBox.ItemsSource!;
        CategoryBox.SelectedItem = categories.FirstOrDefault(c => c.CategoryId == p?.CategoryId) ?? categories.FirstOrDefault();
        DepartmentBox.SelectedItem = departments.FirstOrDefault(d => d.DepartmentId == p?.DepartmentId) ?? departments.FirstOrDefault();

        DeleteButton.IsVisible = p != null;
        VariantsPanel.IsVisible = p != null;
        VariantsList.ItemsSource = p?.ProductVariants.OrderBy(v => v.VariantName).ToList();
        NewVariantName.Text = "";
        NewVariantStock.Text = "";
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        if (CategoryBox.SelectedItem is not Category category || DepartmentBox.SelectedItem is not Department department)
        {
            MainWindow.Instance.ShowWarning("Выберите категорию и отдел.");
            return;
        }
        try
        {
            int id = await ProductService.SaveAsync(current?.ProductId, CodeBox.Text ?? "", NameBox.Text ?? "",
                category.CategoryId, department.DepartmentId, ContentBox.Text, DescriptionBox.Text,
                ImageBox.Text, PriceBox.Text ?? "");
            MainWindow.Instance.ShowMessage(current == null ? "Товар добавлен в каталог." : "Изменения сохранены.");
            await LoadAsync(id);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void DeleteClick(object? sender, RoutedEventArgs e)
    {
        if (current == null) return;
        try
        {
            await ProductService.DeleteAsync(current.ProductId);
            MainWindow.Instance.ShowMessage($"Товар {current.ProductCode} удалён.");
            await LoadAsync(null);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Сохранить остаток партии: значение берётся из поля в той же строке.
    private async void SaveStockClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProductVariant variant } button || current == null) return;

        TextBox? box = button.GetVisualParent()?.GetVisualChildren()
            .OfType<TextBox>().FirstOrDefault();
        try
        {
            await ProductService.SetStockAsync(variant.VariantId, box?.Text ?? "");
            MainWindow.Instance.ShowMessage($"Остаток партии «{variant.VariantName}» сохранён.");
            await LoadAsync(current.ProductId);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void AddVariantClick(object? sender, RoutedEventArgs e)
    {
        if (current == null) return;
        try
        {
            await ProductService.AddVariantAsync(current.ProductId, NewVariantName.Text ?? "", NewVariantStock.Text ?? "");
            MainWindow.Instance.ShowMessage("Партия добавлена.");
            await LoadAsync(current.ProductId);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }
}
