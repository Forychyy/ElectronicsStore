using ElectronicsStore.Data;
using ElectronicsStore.Models;
using ElectronicsStore.Views;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Services;

// Чтение данных для каталога и расчёт цены товара для конкретного клиента.
public static class CatalogService
{
    // Покупал ли клиент товары этой категории в прошлом календарном месяце?
    // i.Variant.Product и i.Order — навигационные свойства: EF сам соединит таблицы.
    public static async Task<bool> HadOrdersAsync(StoreContext db, int clientId, int categoryId)
    {
        var (start, end) = PriceRules.PreviousMonth(DateOnly.FromDateTime(DateTime.Today));

        return await db.OrderItems.AnyAsync(i =>
            i.Order.ClientId == clientId &&
            i.Variant.Product.CategoryId == categoryId &&
            i.Order.OrderDate >= start &&
            i.Order.OrderDate < end);
    }

    // Цена единицы товара для клиента. Без клиента (гость, сотрудник) — базовая цена.
    public static async Task<decimal> GetPriceAsync(StoreContext db, Product product, int? clientId)
    {
        if (clientId == null)
            return product.BasePrice;

        bool hadOrders = await HadOrdersAsync(db, clientId.Value, product.CategoryId);
        return PriceRules.Price(product.BasePrice, hadOrders);
    }

    public static async Task<List<Category>> GetCategoriesAsync()
    {
        using var db = Db.Create();
        return await db.Categories.OrderBy(c => c.CategoryName).AsNoTracking().ToListAsync();
    }

    public static async Task<List<Department>> GetDepartmentsAsync()
    {
        using var db = Db.Create();
        return await db.Departments.OrderBy(d => d.DepartmentName).AsNoTracking().ToListAsync();
    }

    // Все карточки каталога. Цены считаются для клиента clientId (null — без скидки).
    public static async Task<List<ProductCard>> LoadCardsAsync(int? clientId)
    {
        using var db = Db.Create();

        // Товары вместе со связанными данными (Include = «загрузи заодно»).
        List<Product> products = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Department)
            .Include(p => p.ProductVariants)   // партии — чтобы сложить остатки
            .AsNoTracking()
            .ToListAsync();

        var cards = new List<ProductCard>();
        foreach (Product p in products)
        {
            int stock = p.TotalStock;
            decimal price = await GetPriceAsync(db, p, clientId);
            bool discount = price < p.BasePrice;

            cards.Add(new ProductCard
            {
                Id = p.ProductId,
                Code = p.ProductCode,
                Name = p.ProductName,
                CategoryId = p.CategoryId,
                Category = p.Category.CategoryName,
                Department = p.Department.DepartmentName,
                Content = p.ProductContent ?? "Характеристики не указаны",

                Stock = stock,
                StockText = stock == 0 ? "Нет в наличии" : "В наличии: " + PriceRules.Availability(stock),
                StockBrush = UiSupport.Brush(stock > PriceRules.ManyThreshold ? "SuccessBrush" : "DangerBrush"),

                Price = price,
                PriceText = UiSupport.Money(price),
                OldPriceText = discount ? UiSupport.Money(p.BasePrice) : "",
                HasDiscount = discount,

                Background = UiSupport.Brush(PriceRules.LowStock(stock) ? "LowStockBrush" : "NormalBrush"),
                Image = UiSupport.Picture(p.ImagePath)
            });
        }
        return cards;
    }

    // Один товар со всеми партиями (для страницы товара).
    public static async Task<Product?> GetProductAsync(int productId)
    {
        using var db = Db.Create();
        return await db.Products
            .Include(p => p.Category)
            .Include(p => p.Department)
            .Include(p => p.ProductVariants)
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProductId == productId);
    }

    // Цена товара для выбранного клиента (вызывается со страницы товара).
    public static async Task<decimal> GetPriceAsync(int productId, int? clientId)
    {
        using var db = Db.Create();
        Product product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.ProductId == productId)
            ?? throw new AppException("Товар не найден. Обновите каталог.");
        return await GetPriceAsync(db, product, clientId);
    }
}
