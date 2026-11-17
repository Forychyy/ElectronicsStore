using System;
using System.Collections.Generic;

namespace ElectronicsStore.Models;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
