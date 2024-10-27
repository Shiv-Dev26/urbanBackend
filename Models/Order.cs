using System;
using System.Collections.Generic;

namespace urbanBackend.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string UserId { get; set; } = null!;

    public decimal? TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    
}
