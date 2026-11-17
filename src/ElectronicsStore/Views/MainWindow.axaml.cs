using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class MainWindow : Window
{
    // Единственный экземпляр главного окна: страницы обращаются к нему для навигации.
    public static MainWindow Instance { get; private set; } = null!;

    // История открытых страниц для кнопки «Назад».
    private readonly Stack<UserControl> history = new();

    // Таймер, который прячет всплывающее уведомление.
    private readonly DispatcherTimer toastTimer = new() { Interval = TimeSpan.FromSeconds(4) };

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        toastTimer.Tick += (s, e) => HideToast();

        // Стартовая страница — вход в систему.
        OpenPage(new LoginView(), clearHistory: true);
    }

    // Открыть страницу. clearHistory: true — начать историю заново (вход, выход, пункт меню).
    public void OpenPage(UserControl page, bool clearHistory = false)
    {
        if (clearHistory)
            history.Clear();
        else if (PageHost.Content is UserControl current)
            history.Push(current); // текущую страницу кладём в историю

        ShowPage(page);
    }

    // Вернуться на предыдущую страницу (если её нет — в каталог).
    public void GoBack()
    {
        if (history.Count > 0)
            ShowPage(history.Pop());
        else
            OpenPage(new CatalogView(), clearHistory: true);
    }

    // Показать страницу и обновить шапку под роль пользователя.
    private void ShowPage(UserControl page)
    {
        PageHost.Content = page;

        // Заголовок окна берётся из свойства Tag страницы.
        Title = "Магазин электроники «Вольт» — " + (page.Tag ?? "");
        UpdateHeader(page);
    }

    private void UpdateHeader(UserControl page)
    {
        bool loggedIn = !Session.IsGuest;
        bool onLogin = page is LoginView or RegistrationView;

        UserText.Text = loggedIn ? Session.CurrentUser!.FullName : "Гость";
        RoleText.Text = loggedIn ? Session.RoleName : "вход не выполнен";
        AuthButton.Content = loggedIn ? "Выйти" : "Войти";
        AuthButton.IsVisible = !onLogin;

        // Разграничение доступа в интерфейсе: кнопки видны только нужным ролям.
        CatalogButton.IsVisible = !onLogin;
        OrdersButton.IsVisible = loggedIn && !onLogin;
        OrdersButton.Content = Session.IsStaff ? "Заказы" : "Мои заказы";
        ProductsButton.IsVisible = Session.IsStaff;
        ReportsButton.IsVisible = Session.IsStaff;
        UsersButton.IsVisible = Session.IsAdmin;

        // Подсветка текущего раздела.
        SetActive(CatalogButton, page is CatalogView or ProductView);
        SetActive(OrdersButton, page is OrdersView or OrderDetailsView);
        SetActive(ProductsButton, page is ProductsView);
        SetActive(ReportsButton, page is ReportsView);
        SetActive(UsersButton, page is UsersView);
    }

    private static void SetActive(Button button, bool active)
    {
        if (active && !button.Classes.Contains("active"))
            button.Classes.Add("active");
        else if (!active)
            button.Classes.Remove("active");
    }

    // ---------- УВЕДОМЛЕНИЯ ----------

    public void ShowMessage(string text) => ShowToast(text, "SuccessBrush");

    public void ShowWarning(string text) => ShowToast(text, "WarningBrush");

    public void ShowError(Exception error)
    {
        System.Diagnostics.Trace.WriteLine(error); // подробности — в окно Output
        ShowToast(UiSupport.ErrorText(error), "DangerBrush");
    }

    private void ShowToast(string text, string colorKey)
    {
        Toast.Background = UiSupport.Brush("NormalBrush");
        ToastStripe.Background = UiSupport.Brush(colorKey);
        ToastText.Text = text;
        Toast.IsVisible = true;

        toastTimer.Stop();
        toastTimer.Start();
    }

    private void HideToast()
    {
        toastTimer.Stop();
        Toast.IsVisible = false;
    }

    private void ToastPressed(object? sender, PointerPressedEventArgs e) => HideToast();

    // ---------- ОБРАБОТЧИКИ НАВИГАЦИИ ----------

    private void CatalogClick(object? sender, RoutedEventArgs e) =>
        OpenPage(new CatalogView(), clearHistory: true);

    private void OrdersClick(object? sender, RoutedEventArgs e) =>
        OpenPage(new OrdersView(), clearHistory: true);

    private void ProductsClick(object? sender, RoutedEventArgs e) =>
        OpenPage(new ProductsView(), clearHistory: true);

    private void ReportsClick(object? sender, RoutedEventArgs e) =>
        OpenPage(new ReportsView(), clearHistory: true);

    private void UsersClick(object? sender, RoutedEventArgs e) =>
        OpenPage(new UsersView(), clearHistory: true);

    // «Войти» для гостя, «Выйти» для остальных.
    private void AuthClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        OpenPage(new LoginView(), clearHistory: true);
    }
}
