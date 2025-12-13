using System;
using System.Collections.Generic;

namespace Brew3.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }
    public string ImagePath { get; set; } = "/Images/Default/Default.jpg";

    public bool IsAvailable { get; set; }



    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();

    public virtual ICollection<Modifier> Modifiers { get; set; } = new List<Modifier>();
}
