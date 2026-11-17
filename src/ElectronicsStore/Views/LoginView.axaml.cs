using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        // Enter в любом поле — то же, что кнопка «Войти»
        LoginBox.KeyDown += OnFieldKeyDown;
        PasswordBox.KeyDown += OnFieldKeyDown;

        Loaded += (s, e) => LoginBox.Focus();
    }

    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            LoginClick(sender, new RoutedEventArgs());
    }

    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        StatusText.IsVisible = false;

        string login = (LoginBox.Text ?? "").Trim();
        string password = PasswordBox.Text ?? "";
        if (login == "" || password == "")
        {
            ShowStatus("Введите логин и пароль.");
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            await Session.LoginAsync(login, password);

            // Успешный вход: переходим в каталог, шапка перестроится под роль.
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            MainWindow.Instance.ShowMessage($"Здравствуйте, {Session.CurrentUser!.FirstName}!");
        }
        catch (Exception ex)
        {
            ShowStatus(UiSupport.ErrorText(ex));
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    // Гость только просматривает каталог.
    private void GuestClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
    }

    private void RegisterClick(object? sender, RoutedEventArgs e) =>
        MainWindow.Instance.OpenPage(new RegistrationView());

    private void ShowStatus(string text)
    {
        StatusText.Text = text;
        StatusText.IsVisible = true;
    }
}
