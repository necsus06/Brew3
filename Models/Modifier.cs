using System;
using System.Collections.Generic;

namespace Brew3.Models;

public partial class Modifier
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal PriceAddition { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
