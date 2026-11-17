using ElectronicsStore.Data;
using ElectronicsStore.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Services;

// Управление пользователями (раздел «Пользователи», только администратор)
// и общая проверка данных пользователя для регистрации.
public static class UserService
{
    public static async Task<List<User>> GetUsersAsync()
    {
        Session.RequireAdmin();
        using var db = Db.Create();
        return await db.Users
            .Include(u => u.Role)
            .Include(u => u.Orders)
            .OrderBy(u => u.RoleId).ThenBy(u => u.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    public static async Task<List<Role>> GetRolesAsync()
    {
        using var db = Db.Create();
        return await db.Roles.OrderBy(r => r.RoleId).AsNoTracking().ToListAsync();
    }

    // Клиенты — для выбора покупателя, когда заказ оформляет менеджер.
    public static async Task<List<User>> GetClientsAsync()
    {
        Session.RequireStaff();
        using var db = Db.Create();
        return await db.Users
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == Session.RoleClient)
            .OrderBy(u => u.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    // Проверка обязательных полей и их длины.
    private static void Validate(string login, string lastName, string firstName)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new AppException("Укажите логин.");
        if (login.Trim().Length > 50)
            throw new AppException("Логин не может быть длиннее 50 символов.");
        if (login.Trim().Contains(' '))
            throw new AppException("Логин не должен содержать пробелов.");
        if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
            throw new AppException("Укажите фамилию и имя.");
        if (lastName.Trim().Length > 60 || firstName.Trim().Length > 60)
            throw new AppException("Фамилия и имя — не длиннее 60 символов.");
    }

    // Пароль: не короче 4 символов (учебная БД хранит его как есть).
    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            throw new AppException("Пароль должен содержать не менее 4 символов.");
        if (password.Length > 255)
            throw new AppException("Пароль слишком длинный.");
    }

    // Создать пользователя. checkRights = false только для самостоятельной регистрации клиента.
    public static async Task<int> CreateAsync(string login, string password, string lastName, string firstName,
        string? middleName, string roleName, bool checkRights = true)
    {
        if (checkRights) Session.RequireAdmin();
        Validate(login, lastName, firstName);
        ValidatePassword(password);

        using var db = Db.Create();
        login = login.Trim();

        // Логин уникален (без учёта регистра)
        string normalized = login.ToLower();
        if (await db.Users.AnyAsync(u => u.Login.ToLower() == normalized))
            throw new AppException("Такой логин уже занят.");

        Role role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName)
            ?? throw new AppException($"Роль «{roleName}» не найдена в базе данных.");

        var user = new User
        {
            Login = login,
            Password = password,
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            RoleId = role.RoleId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.UserId;
    }

    // Изменить ФИО и роль пользователя; пустой пароль — оставить прежний.
    public static async Task UpdateAsync(int userId, string? newPassword, string lastName, string firstName,
        string? middleName, int roleId)
    {
        Session.RequireAdmin();
        Validate("x", lastName, firstName);
        if (!string.IsNullOrEmpty(newPassword)) ValidatePassword(newPassword);

        if (userId == Session.CurrentUser!.UserId)
        {
            using var check = Db.Create();
            int current = await check.Users.Where(u => u.UserId == userId).Select(u => u.RoleId).SingleAsync();
            if (current != roleId)
                throw new AppException("Нельзя изменить роль собственной учётной записи.");
        }

        using var db = Db.Create();
        User user = await db.Users.FindAsync(userId)
            ?? throw new AppException("Пользователь уже удалён. Обновите список.");

        user.LastName = lastName.Trim();
        user.FirstName = firstName.Trim();
        user.MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        user.RoleId = roleId;
        if (!string.IsNullOrEmpty(newPassword)) user.Password = newPassword;
        await db.SaveChangesAsync();
    }

    // Удалить пользователя без заказов.
    public static async Task DeleteAsync(int userId)
    {
        Session.RequireAdmin();
        if (userId == Session.CurrentUser!.UserId)
            throw new AppException("Нельзя удалить собственную учётную запись.");

        using var db = Db.Create();
        if (await db.Orders.AnyAsync(o => o.ClientId == userId))
            throw new AppException("У пользователя есть заказы — удалить его нельзя.");

        int deleted = await db.Users.Where(u => u.UserId == userId).ExecuteDeleteAsync();
        if (deleted == 0)
            throw new AppException("Пользователь уже удалён. Обновите список.");
    }
}
