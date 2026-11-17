using System;
using System.Collections.Generic;

namespace ElectronicsStore.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public int CategoryId { get; set; }

    public int DepartmentId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductContent { get; set; }

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public decimal BasePrice { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
