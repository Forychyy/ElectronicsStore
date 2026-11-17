using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ElectronicsStore.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Views;

// Вспомогательные методы для интерфейса: цвета, картинки, деньги, тексты ошибок.
public static class UiSupport
{
    // Адрес папки Assets внутри программы.
    private const string AssetsRoot = "avares://ElectronicsStore/Assets/";

    // Уже загруженные картинки: ключ — адрес файла, значение — сама картинка.
    private static readonly Dictionary<string, Bitmap> cache = new();

    // Цвет из App.axaml по имени (x:Key), например Brush("LowStockBrush").
    public static IBrush Brush(string key) => (IBrush)Application.Current!.Resources[key]!;

    // Сумма в рублях: «24 742,50 ₽».
    public static string Money(decimal value) => $"{value:N2} ₽";

    // Картинка из папки Assets/Images по имени файла из столбца image_path.
    // Если имя пустое или такого файла нет — возвращаем заглушку picture.png.
    public static Bitmap Picture(string? fileName)
    {
        string uri = AssetsRoot + "Images/" + fileName;
        if (string.IsNullOrWhiteSpace(fileName) || !AssetLoader.Exists(new Uri(uri)))
            uri = AssetsRoot + "picture.png";

        if (!cache.TryGetValue(uri, out Bitmap? bitmap))
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            bitmap = new Bitmap(stream);
            cache[uri] = bitmap;
        }
        return bitmap;
    }

    // Превращает техническую ошибку в понятный пользователю текст.
    public static string ErrorText(Exception error)
    {
        // Наши собственные ошибки (throw new AppException("...")) — текст уже понятный.
        if (error is AppException)
            return error.Message;

        // Нарушение ограничения CHECK/UNIQUE/FOREIGN KEY при сохранении.
        if (error is DbUpdateException { InnerException: SqlException sql } && sql.Number is 547 or 2601 or 2627)
            return "Операция нарушает ограничения базы данных (связанные записи или повтор значения).";

        // Ошибки SQL Server и сохранения через EF Core
        // (EF Core может обернуть SqlException в другое исключение — проверяем всю цепочку).
        bool sqlError = false;
        for (Exception? e = error; e != null; e = e.InnerException)
            if (e is SqlException || e is DbUpdateException) sqlError = true;
        if (sqlError)
            return "Ошибка базы данных. Проверьте, что SQL Server запущен и строка " +
                   "в connection.txt верна, затем нажмите «Обновить».";

        // Не найден файл (обычно connection.txt).
        if (error is IOException)
            return "Не найден файл connection.txt рядом с программой.";

        return "Операция не выполнена: " + error.Message;
    }
}
