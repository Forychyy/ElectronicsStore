using Avalonia.Controls;
using Avalonia.Interactivity;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();

        // По умолчанию — с начала текущего месяца по сегодня.
        DateTime today = DateTime.Today;
        FromBox.SelectedDate = new DateTime(today.Year, today.Month, 1);
        ToBox.SelectedDate = today;
        GroupBox.SelectedIndex = 0;

        Loaded += async (s, e) => await BuildAsync();
    }

    private async Task BuildAsync()
    {
        if (FromBox.SelectedDate is not DateTime from || ToBox.SelectedDate is not DateTime to)
        {
            MainWindow.Instance.ShowWarning("Укажите начало и окончание периода.");
            return;
        }
        try
        {
            DateOnly start = DateOnly.FromDateTime(from), end = DateOnly.FromDateTime(to);
            bool byProduct = GroupBox.SelectedIndex == 1;

            List<ReportRow> rows = byProduct
                ? await ReportService.SalesByProductAsync(start, end)
                : await ReportService.SalesByCategoryAsync(start, end);

            NameHeader.Text = byProduct ? "Товар" : "Категория";
            RowsList.ItemsSource = rows;
            EmptyText.IsVisible = rows.Count == 0;

            decimal revenue = rows.Sum(r => r.Revenue);
            int orders = await ReportService.OrdersCountAsync(start, end);
            RevenueText.Text = UiSupport.Money(revenue);
            UnitsText.Text = rows.Sum(r => r.Units).ToString();
            AverageText.Text = orders == 0 ? "—" : UiSupport.Money(decimal.Round(revenue / orders, 2));
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void BuildClick(object? sender, RoutedEventArgs e) => await BuildAsync();

    // Быстрый выбор: предыдущий календарный месяц.
    private async void LastMonthClick(object? sender, RoutedEventArgs e)
    {
        var (start, end) = PriceRules.PreviousMonth(DateOnly.FromDateTime(DateTime.Today));
        FromBox.SelectedDate = start.ToDateTime(TimeOnly.MinValue);
        ToBox.SelectedDate = end.AddDays(-1).ToDateTime(TimeOnly.MinValue);
        await BuildAsync();
    }
}
