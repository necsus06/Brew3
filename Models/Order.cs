using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace Brew3.Models;

public partial class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public decimal Total { get; set; }

    public bool IsTakeaway { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;

    [NotMapped]
    public decimal TotalPrice { get; set; }
}
