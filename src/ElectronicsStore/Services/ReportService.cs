using ElectronicsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Services;

// Одна строка отчёта о продажах.
public class ReportRow
{
    public string Name { get; set; } = "";   // категория или товар
    public int Orders { get; set; }          // число заказов
    public int Units { get; set; }           // продано единиц
    public decimal Revenue { get; set; }     // выручка, руб.
    public double Share { get; set; }        // доля в выручке, 0..1 (для полоски)
    public string RevenueText => $"{Revenue:N2} ₽";
    public string ShareText => $"{Share:P0}";
}

// Формирование отчётов о продажах за период (менеджер и администратор).
public static class ReportService
{
    public static async Task<List<ReportRow>> SalesByCategoryAsync(DateOnly from, DateOnly to)
    {
        Session.RequireStaff();
        CheckPeriod(from, to);

        using var db = Db.Create();
        var rows = await db.OrderItems
            .Where(i => i.Order.OrderDate >= from && i.Order.OrderDate <= to)
            .GroupBy(i => i.Variant.Product.Category.CategoryName)
            .Select(g => new ReportRow
            {
                Name = g.Key,
                Orders = g.Select(i => i.OrderId).Distinct().Count(),
                Units = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
            })
            .ToListAsync();

        return WithShares(rows);
    }

    public static async Task<List<ReportRow>> SalesByProductAsync(DateOnly from, DateOnly to)
    {
        Session.RequireStaff();
        CheckPeriod(from, to);

        using var db = Db.Create();
        var rows = await db.OrderItems
            .Where(i => i.Order.OrderDate >= from && i.Order.OrderDate <= to)
            .GroupBy(i => i.Variant.Product.ProductName)
            .Select(g => new ReportRow
            {
                Name = g.Key,
                Orders = g.Select(i => i.OrderId).Distinct().Count(),
                Units = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
            })
            .ToListAsync();

        return WithShares(rows);
    }

    // Число заказов за период (для расчёта среднего чека).
    public static async Task<int> OrdersCountAsync(DateOnly from, DateOnly to)
    {
        Session.RequireStaff();
        using var db = Db.Create();
        return await db.Orders.CountAsync(o => o.OrderDate >= from && o.OrderDate <= to);
    }

    private static void CheckPeriod(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new AppException("Начало периода должно быть не позже его окончания.");
    }

    // Доля каждой строки в общей выручке; сортировка по убыванию выручки.
    private static List<ReportRow> WithShares(List<ReportRow> rows)
    {
        decimal total = rows.Sum(r => r.Revenue);
        foreach (var r in rows)
            r.Share = total == 0 ? 0 : (double)(r.Revenue / total);
        return rows.OrderByDescending(r => r.Revenue).ToList();
    }
}
