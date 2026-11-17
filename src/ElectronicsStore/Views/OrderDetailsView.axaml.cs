using Avalonia.Controls;
using Avalonia.Interactivity;
using ElectronicsStore.Models;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class OrderDetailsView : UserControl
{
    public int OrderId { get; set; }

    // Что удаляем после подтверждения: позицию (номер) или весь заказ (null).
    private int? pendingItemId;

    public OrderDetailsView()
    {
        InitializeComponent();

        // Кнопки, доступные только администратору, включаются классом страницы.
        if (Session.IsAdmin) Classes.Add("admin");
        DatePanel.IsVisible = Session.IsAdmin;
        DeleteOrderButton.IsVisible = Session.IsStaff;

        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            ConfirmPanel.IsVisible = false;
            Order? order = await OrderService.GetOrderAsync(OrderId);

            if (order == null)
            {
                // Заказ удалён (например, удалили последнюю позицию) — возвращаемся к списку.
                MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
                return;
            }

            HeaderText.Text = $"Заказ № {order.OrderId}";
            ClientText.Text = "Клиент: " + order.ClientName;
            DateText.Text = $"Дата оформления: {order.OrderDate:dd.MM.yyyy}";
            DateBox.SelectedDate = order.OrderDate.ToDateTime(TimeOnly.MinValue);

            ItemsList.ItemsSource = order.OrderItems.OrderBy(i => i.OrderItemId).ToList();
            UnitsText.Text = $"Позиций: {order.OrderItems.Count}, единиц товара: {order.UnitsCount}";
            TotalText.Text = UiSupport.Money(order.Total);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // ИЗМЕНЕНИЕ ДАТЫ (администратор)
    private async void ChangeDateClick(object? sender, RoutedEventArgs e)
    {
        if (DateBox.SelectedDate is not DateTime date)
        {
            MainWindow.Instance.ShowWarning("Выберите дату.");
            return;
        }
        try
        {
            await OrderService.ChangeDateAsync(OrderId, DateOnly.FromDateTime(date));
            MainWindow.Instance.ShowMessage($"Дата заказа № {OrderId} изменена на {date:dd.MM.yyyy}.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // УДАЛЕНИЕ: сначала подтверждение, затем вызов сервиса.
    private void DeleteItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: OrderItem item }) return;
        pendingItemId = item.OrderItemId;
        ConfirmTitle.Text = "Удалить позицию?";
        ConfirmText.Text = $"«{item.ProductName}» × {item.Quantity} будет удалён из заказа, товар вернётся на склад.";
        ConfirmPanel.IsVisible = true;
    }

    private void DeleteOrderClick(object? sender, RoutedEventArgs e)
    {
        pendingItemId = null;
        ConfirmTitle.Text = $"Удалить заказ № {OrderId}?";
        ConfirmText.Text = "Заказ будет удалён целиком, весь товар вернётся на склад. Действие нельзя отменить.";
        ConfirmPanel.IsVisible = true;
    }

    private void ConfirmCancelClick(object? sender, RoutedEventArgs e) => ConfirmPanel.IsVisible = false;

    private async void ConfirmDeleteClick(object? sender, RoutedEventArgs e)
    {
        ConfirmPanel.IsVisible = false;
        try
        {
            await OrderService.DeleteAsync(OrderId, pendingItemId);

            if (pendingItemId == null)
            {
                MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
                MainWindow.Instance.ShowMessage($"Заказ № {OrderId} удалён, товар возвращён на склад.");
            }
            else
            {
                MainWindow.Instance.ShowMessage("Позиция удалена, товар возвращён на склад.");
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void BackClick(object? sender, RoutedEventArgs e) =>
        MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
}
