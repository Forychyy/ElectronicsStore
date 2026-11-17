using Avalonia.Controls;
using Avalonia.Interactivity;
using ElectronicsStore.Models;
using ElectronicsStore.Services;

namespace ElectronicsStore.Views;

public partial class UsersView : UserControl
{
    private User? current; // выбранный пользователь; null — создаётся новый
    private List<Role> roles = new();

    public UsersView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync(null);
    }

    private async Task LoadAsync(int? selectId)
    {
        try
        {
            if (roles.Count == 0)
            {
                roles = await UserService.GetRolesAsync();
                RoleBox.ItemsSource = roles;
            }

            var users = await UserService.GetUsersAsync();
            UsersList.ItemsSource = users;

            User? toSelect = users.FirstOrDefault(u => u.UserId == selectId);
            if (toSelect != null)
                UsersList.SelectedItem = toSelect;
            else
                ShowUser(null);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void UserSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem is User u)
            ShowUser(u);
    }

    private void NewClick(object? sender, RoutedEventArgs e)
    {
        UsersList.SelectedItem = null;
        ShowUser(null);
    }

    private void ShowUser(User? u)
    {
        current = u;
        FormTitle.Text = u == null ? "Новый пользователь" : "Изменение пользователя";
        LoginBox.Text = u?.Login ?? "";
        LoginBox.IsEnabled = u == null; // логин после создания не меняется
        PasswordBox.Text = "";
        PasswordLabel.Text = u == null ? "Пароль *" : "Новый пароль (пусто — не менять)";
        LastNameBox.Text = u?.LastName ?? "";
        FirstNameBox.Text = u?.FirstName ?? "";
        MiddleNameBox.Text = u?.MiddleName ?? "";
        RoleBox.SelectedItem = roles.FirstOrDefault(r => r.RoleId == u?.RoleId)
                               ?? roles.FirstOrDefault(r => r.RoleName == Session.RoleClient);
        DeleteButton.IsVisible = u != null;
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        if (RoleBox.SelectedItem is not Role role)
        {
            MainWindow.Instance.ShowWarning("Выберите роль.");
            return;
        }
        try
        {
            int id;
            if (current == null)
            {
                id = await UserService.CreateAsync(LoginBox.Text ?? "", PasswordBox.Text ?? "", LastNameBox.Text ?? "",
                    FirstNameBox.Text ?? "", MiddleNameBox.Text, role.RoleName);
                MainWindow.Instance.ShowMessage("Пользователь создан.");
            }
            else
            {
                id = current.UserId;
                await UserService.UpdateAsync(id, PasswordBox.Text, LastNameBox.Text ?? "", FirstNameBox.Text ?? "",
                    MiddleNameBox.Text, role.RoleId);
                MainWindow.Instance.ShowMessage("Изменения сохранены.");
            }
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
            await UserService.DeleteAsync(current.UserId);
            MainWindow.Instance.ShowMessage($"Пользователь {current.Login} удалён.");
            await LoadAsync(null);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }
}
