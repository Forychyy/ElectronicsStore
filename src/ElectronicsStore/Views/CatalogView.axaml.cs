using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ElectronicsStore.Models;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class CatalogView : UserControl
{
    // Все карточки из БД; фильтры применяются к этому списку без повторного запроса.
    private List<ProductCard> allCards = new();

    // Пункт «Все категории» снимает фильтр по категории.
    private static readonly Category AllCategories = new() { CategoryId = 0, CategoryName = "Все категории" };

    public CatalogView()
    {
        InitializeComponent();
        SortBox.SelectedIndex = 0;

        if (Session.IsGuest)
            SubtitleText.Text = "Войдите в систему, чтобы оформить заказ";
        else if (Session.IsStaff)
            SubtitleText.Text = "Указаны базовые цены. Цена для клиента рассчитывается при оформлении";
        else
            SubtitleText.Text = "Цены указаны с учётом вашей персональной скидки";

        Loaded += async (s, e) => await LoadAsync();
    }

    // ЗАГРУЗКА ДАННЫХ ИЗ БАЗЫ
    private async Task LoadAsync()
    {
        try
        {
            if (CategoryBox.ItemsSource == null)
            {
                var categories = new List<Category> { AllCategories };
                categories.AddRange(await CatalogService.GetCategoriesAsync());
                CategoryBox.ItemsSource = categories;
                CategoryBox.SelectedIndex = 0;
            }

            // Цены со скидкой считаются только для клиента; гость и сотрудник видят базовые.
            int? clientId = Session.IsClient ? Session.CurrentUser!.UserId : null;
            allCards = await CatalogService.LoadCardsAsync(clientId);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // ПОИСК, ФИЛЬТР И СОРТИРОВКА (по уже загруженному списку)
    private void ApplyFilters()
    {
        string text = (SearchBox.Text ?? "").Trim();
        var ic = StringComparison.OrdinalIgnoreCase; // без учёта регистра
        int categoryId = (CategoryBox.SelectedItem as Category)?.CategoryId ?? 0;

        IEnumerable<ProductCard> rows = allCards.Where(c =>
            (categoryId == 0 || c.CategoryId == categoryId) &&
            (text == "" || c.Name.Contains(text, ic) || c.Code.Contains(text, ic)));

        rows = SortBox.SelectedIndex switch
        {
            1 => rows.OrderBy(c => c.Price).ThenBy(c => c.Name),
            2 => rows.OrderByDescending(c => c.Price).ThenBy(c => c.Name),
            _ => rows.OrderBy(c => c.Name)
        };

        List<ProductCard> result = rows.ToList();
        CardsList.ItemsSource = result;
        EmptyPanel.IsVisible = result.Count == 0 && allCards.Count > 0;
        CountText.Text = $"Показано {result.Count} из {allCards.Count}";
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilters();

    private void FilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CardsList != null) ApplyFilters();
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadAsync();

    // ЩЕЛЧОК ПО КАРТОЧКЕ — страница товара
    private void CardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { Tag: ProductCard card }) return;

        if (Session.IsGuest)
        {
            MainWindow.Instance.ShowWarning("Чтобы оформить заказ, войдите в систему.");
            return;
        }

        MainWindow.Instance.OpenPage(new ProductView { ProductId = card.Id });
    }
}
