using ElectronicsStore.Data;
using ElectronicsStore.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Services;

// Все операции с заказами: оформить, показать, изменить дату, удалить.
// Права проверяются здесь, а не только скрытием кнопок в интерфейсе.
public static class OrderService
{
    // ---------- 1. Оформление заказа. Возвращает номер нового заказа. ----------
    public static async Task<int> CreateOrderAsync(int variantId, int quantity,
                                                   int clientId, decimal shownPrice)
    {
        // Проверки, которым не нужна база данных.
        if (Session.CurrentUser == null)
            throw new AppException("Гость не может оформить заказ. Войдите в систему.");
        if (!Session.IsStaff && clientId != Session.CurrentUser.UserId)
            throw new AppException("Клиент может оформить заказ только на себя.");
        if (quantity <= 0)
            throw new AppException("Количество должно быть целым числом больше нуля.");

        using var db = Db.Create();

        // Транзакция: всё, что ниже, сохранится ВМЕСТЕ или не сохранится ВООБЩЕ.
        using var transaction = await db.Database.BeginTransactionAsync();

        // Находим партию вместе с товаром (нужны базовая цена и категория).
        ProductVariant variant = await db.ProductVariants
            .Include(v => v.Product)
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.VariantId == variantId)
            ?? throw new AppException("Партия не найдена. Обновите карточку товара.");

        // Пересчитываем цену: вдруг она изменилась, пока форма была открыта.
        decimal price = await CatalogService.GetPriceAsync(db, variant.Product, clientId);
        if (price != shownPrice)
            throw new AppException("Цена изменилась. Обновите карточку товара.");

        // Уменьшаем остаток ОДНОЙ командой SQL и только если товара хватает:
        // UPDATE product_variants SET available_stock = available_stock - @quantity
        // WHERE batch_id = @variantId AND available_stock >= @quantity
        int changed = await db.ProductVariants
            .Where(v => v.VariantId == variantId && v.AvailableStock >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(
                v => v.AvailableStock,
                v => v.AvailableStock - quantity));

        if (changed == 0) // ни одна строка не изменилась — товара не хватило
            throw new AppException("Недостаточно товара на складе. Обновите карточку товара.");

        // Создаём заказ и его позицию как обычные объекты C#.
        var order = new Order
        {
            OrderDate = DateOnly.FromDateTime(DateTime.Today),
            ClientId = clientId
        };
        order.OrderItems.Add(new OrderItem
        {
            VariantId = variantId,
            Quantity = quantity,
            UnitPrice = price // запоминаем цену покупки
        });

        db.Orders.Add(order);
        await db.SaveChangesAsync();      // INSERT в orders, затем в order_items
        await transaction.CommitAsync();  // заказ и списание остатка — вместе
        return order.OrderId;
    }

    // Заказ вместе с клиентом и составом.
    private static IQueryable<Order> WithDetails(StoreContext db) => db.Orders
        .Include(o => o.Client)
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Variant)
            .ThenInclude(v => v.Product);

    // ---------- 2. Список заказов: сотрудник видит все, клиент — только свои. ----------
    public static async Task<List<Order>> GetOrdersAsync()
    {
        if (Session.CurrentUser == null)
            throw new AppException("Войдите в систему, чтобы увидеть заказы.");

        using var db = Db.Create();
        IQueryable<Order> query = WithDetails(db);

        if (!Session.IsStaff)
            query = query.Where(o => o.ClientId == Session.CurrentUser.UserId);

        return await query
            .OrderByDescending(o => o.OrderDate)  // новые сверху
            .ThenByDescending(o => o.OrderId)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    // ---------- 3. Один заказ со всем составом. null — если заказа уже нет. ----------
    public static async Task<Order?> GetOrderAsync(int orderId)
    {
        using var db = Db.Create();
        Order? order = await WithDetails(db)
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.OrderId == orderId);

        if (order != null && !Session.IsStaff && order.ClientId != Session.CurrentUser?.UserId)
            throw new AppException("Нет доступа к этому заказу.");

        return order;
    }

    // ---------- 4. Изменить дату заказа (только администратор). ----------
    public static async Task ChangeDateAsync(int orderId, DateOnly newDate)
    {
        Session.RequireAdmin();
        if (newDate > DateOnly.FromDateTime(DateTime.Today))
            throw new AppException("Дата заказа не может быть позже сегодняшней.");

        using var db = Db.Create();
        Order order = await db.Orders.FindAsync(orderId)
            ?? throw new AppException("Заказ уже удалён. Обновите список.");

        order.OrderDate = newDate;    // меняем свойство объекта...
        await db.SaveChangesAsync();  // ...EF сам отправит UPDATE в базу
    }

    // ---------- 5. Удаление с возвратом товара на склад ----------
    // itemId = номер позиции → удалить одну позицию (только администратор);
    // itemId = null          → удалить весь заказ (менеджер или администратор).
    public static async Task DeleteAsync(int orderId, int? itemId)
    {
        if (itemId == null)
            Session.RequireStaff();
        else
            Session.RequireAdmin();

        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();

        // Какие позиции удаляем: одну выбранную или все позиции заказа.
        List<OrderItem> items = await db.OrderItems
            .Where(i => i.OrderId == orderId && (itemId == null || i.OrderItemId == itemId))
            .ToListAsync();

        if (items.Count == 0)
            throw new AppException("Эти данные уже удалены. Обновите список.");

        // Возвращаем товар: прибавляем количество к текущему остатку в базе.
        foreach (OrderItem item in items)
        {
            await db.ProductVariants
                .Where(v => v.VariantId == item.VariantId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    v => v.AvailableStock,
                    v => v.AvailableStock + item.Quantity));
        }

        db.OrderItems.RemoveRange(items);
        await db.SaveChangesAsync();

        // Если в заказе не осталось позиций — удаляем и сам заказ.
        bool hasItems = await db.OrderItems.AnyAsync(i => i.OrderId == orderId);
        if (!hasItems)
            await db.Orders.Where(o => o.OrderId == orderId).ExecuteDeleteAsync();

        await transaction.CommitAsync(); // возврат товара и удаление — вместе
    }
}
