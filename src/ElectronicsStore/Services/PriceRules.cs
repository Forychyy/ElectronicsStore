namespace ElectronicsStore.Services;

// Правила расчёта. Здесь нет ни базы данных, ни окон — только арифметика.
// Поэтому эти методы легко проверить отдельно от интерфейса.
public static class PriceRules
{
    // Скидка клиенту, если в прошлом месяце он не покупал товары этой категории, %.
    public const decimal Discount = 25m;

    // Остаток больше 5 — «много», иначе «мало».
    public const int ManyThreshold = 5;

    // Красная подсветка карточки: остаток 3 и меньше.
    public const int LowStockThreshold = 3;

    // Границы предыдущего календарного месяца для даты day.
    // Пример: day = 07.11.2026 → Start = 01.10.2026, End = 01.11.2026.
    // Start входит в период, End — уже нет.
    public static (DateOnly Start, DateOnly End) PreviousMonth(DateOnly day)
    {
        DateOnly end = new DateOnly(day.Year, day.Month, 1); // 1-е число этого месяца
        DateOnly start = end.AddMonths(-1);                  // 1-е число прошлого месяца
        return (start, end);
    }

    // Цена за единицу: P = P0 × (1 − d / 100).
    // Скидка действует, если у клиента НЕ было заказов этой категории в прошлом месяце.
    public static decimal Price(decimal basePrice, bool hadOrdersLastMonth)
    {
        decimal discount = hadOrdersLastMonth ? 0m : Discount;
        decimal price = basePrice * (1 - discount / 100m);

        // Округляем до копеек: 2 знака, половина копейки — вверх.
        return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    // Стоимость позиции: S = P × q.
    public static decimal Total(decimal unitPrice, int quantity) =>
        decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);

    // Текст об остатке: больше 5 — «много», иначе «мало».
    public static string Availability(int stock) => stock > ManyThreshold ? "много" : "мало";

    // Красная подсветка карточки: остаток 3 или меньше.
    public static bool LowStock(int stock) => stock <= LowStockThreshold;
}
