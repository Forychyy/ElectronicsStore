using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Data;

// Db — «фабрика подключений». В любом месте программы пишем:
//     using var db = Db.Create();
// и получаем готовый к работе StoreContext (его создал scaffold).
public static class Db
{
    public static StoreContext Create()
    {
        // connection.txt копируется рядом с .exe (настройка в ElectronicsStore.csproj).
        string path = Path.Combine(AppContext.BaseDirectory, "connection.txt");

        // Читаем строку подключения; Trim убирает лишние пробелы и переводы строк.
        string connection = File.ReadAllText(path).Trim();

        // Говорим EF Core: работаем с SQL Server по этой строке подключения.
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseSqlServer(connection)
            .Options;

        // Контекст — объект, через который читаем и меняем таблицы БД.
        return new StoreContext(options);
    }
}
