using ElectronicsStore.Data;
using ElectronicsStore.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Services;

// Session («сеанс») помнит, кто сейчас работает с программой.
// Класс static: он один на всю программу, пишем просто Session.CurrentUser.
public static class Session
{
    // Названия ролей — ТОЧНО как в таблице roles.
    public const string RoleAdmin = "Администратор";
    public const string RoleManager = "Менеджер";
    public const string RoleClient = "Клиент";

    // Текущий пользователь. null означает «гость» (никто не вошёл).
    public static User? CurrentUser { get; private set; }

    // Название роли текущего пользователя; для гостя — пустая строка.
    public static string RoleName => CurrentUser?.Role.RoleName ?? "";

    // Уровни доступа.
    public static bool IsGuest => CurrentUser == null;
    public static bool IsClient => RoleName == RoleClient;
    public static bool IsAdmin => RoleName == RoleAdmin;
    public static bool IsStaff => RoleName == RoleManager || IsAdmin; // менеджер или администратор

    // Подпись в шапке окна: «Иванов Алексей Петрович · Клиент» или «Гость».
    public static string Caption =>
        CurrentUser == null ? "Гость" : $"{CurrentUser.FullName} · {RoleName}";

    // Вход по логину и паролю
    public static async Task LoginAsync(string login, string password)
    {
        using var db = Db.Create();
        string normalized = login.Trim().ToLower();

        // Ищем пользователя по логину (без учёта регистра) вместе с ролью
        var user = await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login.ToLower() == normalized);

        // Одинаковый текст для «нет логина» и «не тот пароль» — не подсказываем злоумышленнику
        if (user == null || user.Password != password)
            throw new AppException("Неверный логин или пароль.");

        CurrentUser = user;
    }

    // Регистрация нового клиента и сразу вход под ним.
    public static async Task RegisterAsync(string login, string password, string lastName, string firstName, string? middleName)
    {
        await UserService.CreateAsync(login, password, lastName, firstName, middleName, RoleClient, checkRights: false);
        await LoginAsync(login, password);
    }

    // Выход: снова гость.
    public static void Logout()
    {
        CurrentUser = null;
    }

    // Проверки прав. Вызываются в сервисах ПЕРЕД изменением данных.
    // Если прав нет — выбрасываем ошибку, и операция не выполняется.
    public static void RequireStaff()
    {
        if (!IsStaff)
            throw new AppException("Недостаточно прав: нужна роль менеджера или администратора.");
    }

    public static void RequireAdmin()
    {
        if (!IsAdmin)
            throw new AppException("Недостаточно прав: нужна роль администратора.");
    }
}
