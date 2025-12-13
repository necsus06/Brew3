using System;
using System.Collections.Generic;

namespace Brew3.Models;

public partial class Ingredient
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Unit { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal MinStockThreshold { get; set; }

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
}
