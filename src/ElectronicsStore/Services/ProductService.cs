using ElectronicsStore.Data;
using ElectronicsStore.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsStore.Services;

// Ведение каталога товаров (раздел «Товары»): менеджер и администратор.
public static class ProductService
{
    public static async Task<List<Product>> GetProductsAsync()
    {
        Session.RequireStaff();
        using var db = Db.Create();
        return await db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
            .OrderBy(p => p.ProductCode)
            .AsNoTracking()
            .ToListAsync();
    }

    // Сохранить товар: productId = null — новый товар, иначе изменение существующего.
    public static async Task<int> SaveAsync(int? productId, string code, string name, int categoryId,
        int departmentId, string? content, string? description, string? imagePath, string priceText)
    {
        Session.RequireStaff();

        code = code.Trim().ToUpper();
        name = name.Trim();
        if (code == "" || name == "")
            throw new AppException("Укажите артикул и наименование товара.");
        if (code.Length > 20)
            throw new AppException("Артикул — не длиннее 20 символов.");
        if (name.Length > 150)
            throw new AppException("Наименование — не длиннее 150 символов.");
        if (content?.Length > 300)
            throw new AppException("Краткие характеристики — не длиннее 300 символов.");
        if (!decimal.TryParse(priceText.Trim().Replace('.', ','), out decimal price) || price < 0)
            throw new AppException("Цена должна быть неотрицательным числом.");
        price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);

        using var db = Db.Create();

        if (await db.Products.AnyAsync(p => p.ProductCode == code && p.ProductId != productId))
            throw new AppException($"Товар с артикулом {code} уже есть в каталоге.");

        Product product;
        if (productId == null)
        {
            product = new Product();
            db.Products.Add(product);
        }
        else
        {
            product = await db.Products.FindAsync(productId.Value)
                ?? throw new AppException("Товар уже удалён. Обновите список.");
        }

        product.ProductCode = code;
        product.ProductName = name;
        product.CategoryId = categoryId;
        product.DepartmentId = departmentId;
        product.ProductContent = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        product.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        product.ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        product.BasePrice = price;

        await db.SaveChangesAsync();
        return product.ProductId;
    }

    // Удалить товар можно, только если его ещё ни разу не продавали.
    public static async Task DeleteAsync(int productId)
    {
        Session.RequireStaff();
        using var db = Db.Create();

        if (await db.OrderItems.AnyAsync(i => i.Variant.ProductId == productId))
            throw new AppException("Товар уже есть в заказах — удалить его нельзя.");

        // Партии удалятся каскадно (ON DELETE CASCADE).
        int deleted = await db.Products.Where(p => p.ProductId == productId).ExecuteDeleteAsync();
        if (deleted == 0)
            throw new AppException("Товар уже удалён. Обновите список.");
    }

    // Добавить партию товара на склад.
    public static async Task AddVariantAsync(int productId, string name, string stockText)
    {
        Session.RequireStaff();
        name = name.Trim();
        if (name == "" || name.Length > 30)
            throw new AppException("Название партии — от 1 до 30 символов.");
        int stock = ParseStock(stockText);

        using var db = Db.Create();
        if (await db.ProductVariants.AnyAsync(v => v.ProductId == productId && v.VariantName == name))
            throw new AppException("Партия с таким названием у товара уже есть.");

        db.ProductVariants.Add(new ProductVariant { ProductId = productId, VariantName = name, AvailableStock = stock });
        await db.SaveChangesAsync();
    }

    // Установить остаток партии (после пересчёта на складе или поступления товара).
    public static async Task SetStockAsync(int variantId, string stockText)
    {
        Session.RequireStaff();
        int stock = ParseStock(stockText);

        using var db = Db.Create();
        int changed = await db.ProductVariants
            .Where(v => v.VariantId == variantId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.AvailableStock, stock));
        if (changed == 0)
            throw new AppException("Партия уже удалена. Обновите список.");
    }

    private static int ParseStock(string text)
    {
        if (!int.TryParse(text.Trim(), out int stock) || stock < 0)
            throw new AppException("Остаток должен быть целым неотрицательным числом.");
        return stock;
    }
}
