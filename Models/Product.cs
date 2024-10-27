using System;
using System.Collections.Generic;

namespace urbanBackend.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Image1 { get; set; }
    public string? Image2 { get; set; }
    public string? Image3 { get; set; }
    public string? Image4 { get; set; }
    public int StockCount { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public string VendorId { get; set; } = null!;

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

}
