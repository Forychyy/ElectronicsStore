using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ElectronicsStore.Models;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class OrdersView : UserControl
{
    private List<Order> allOrders = new();

    public OrdersView()
    {
        InitializeComponent();
        TitleText.Text = Session.IsStaff ? "Журнал заказов" : "Мои заказы";
        Loaded += async (s, e) => await LoadAsync();
    }

    // Сотрудник получает все заказы магазина, клиент — только свои (фильтр в OrderService).
    private async Task LoadAsync()
    {
        try
        {
            allOrders = await OrderService.GetOrdersAsync();
            ApplyFilter();
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

        var rows = allOrders.Where(o =>
                text == "" ||
                o.OrderId.ToString() == text ||
                o.ClientName.Contains(text, ic) ||
                o.ItemsText.Contains(text, ic))
            .ToList();

        OrdersList.ItemsSource = rows;
        CountText.Text = rows.Count.ToString();
        SumText.Text = UiSupport.Money(rows.Sum(o => o.Total));

        EmptyPanel.IsVisible = rows.Count == 0;
        EmptyTitle.Text = allOrders.Count == 0 ? "Заказов пока нет" : "Ничего не найдено";
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (OrdersList != null) ApplyFilter();
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadAsync();

    private void OrderTapped(object? sender, TappedEventArgs e)
    {
        if (OrdersList.SelectedItem is Order order)
            MainWindow.Instance.OpenPage(new OrderDetailsView { OrderId = order.OrderId });
    }
}
