using Avalonia.Controls;
using Avalonia.Interactivity;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
    }

    private async void RegisterClick(object? sender, RoutedEventArgs e)
    {
        StatusText.IsVisible = false;
        SaveButton.IsEnabled = false;
        try
        {
            if ((PasswordBox.Text ?? "") != (Password2Box.Text ?? ""))
                throw new AppException("Пароли не совпадают.");

            // Проверка полей и уникальности логина — в UserService.
            await Session.RegisterAsync(LoginBox.Text ?? "", PasswordBox.Text ?? "", LastNameBox.Text ?? "",
                FirstNameBox.Text ?? "", MiddleNameBox.Text);

            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            MainWindow.Instance.ShowMessage("Регистрация завершена. Добро пожаловать!");
        }
        catch (Exception ex)
        {
            StatusText.Text = UiSupport.ErrorText(ex);
            StatusText.IsVisible = true;
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void BackClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.GoBack();
}
