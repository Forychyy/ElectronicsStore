using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ElectronicsStore.Services;

// Одна карточка каталога: всё, что нужно показать на экране для одного товара.
// Разметка CatalogView.axaml берёт значения отсюда через {Binding ИмяСвойства}.
// Значения заполняет метод CatalogService.LoadCardsAsync.
public class ProductCard
{
    public int Id { get; set; }                     // product_id — чтобы открыть товар
    public string Code { get; set; } = "";          // артикул (для поиска)
    public string Name { get; set; } = "";          // наименование
    public int CategoryId { get; set; }             // для фильтра по категории
    public string Category { get; set; } = "";      // название категории
    public string Department { get; set; } = "";    // торговый отдел
    public string Content { get; set; } = "";       // краткие характеристики

    public int Stock { get; set; }                  // сумма остатков всех партий
    public string StockText { get; set; } = "";     // «В наличии: много» / «В наличии: мало»
    public IBrush? StockBrush { get; set; }         // цвет текста об остатке

    public decimal Price { get; set; }              // цена для клиента (для сортировки)
    public string PriceText { get; set; } = "";     // «24 742,50 ₽»
    public string OldPriceText { get; set; } = "";  // базовая цена, если действует скидка
    public bool HasDiscount { get; set; }           // показывать ли плашку «−25 %»

    public IBrush? Background { get; set; }         // фон карточки (красный, если остаток ≤ 3)
    public Bitmap? Image { get; set; }              // фото товара или заглушка
}
