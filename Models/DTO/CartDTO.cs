namespace urbanBackend.Models.DTO
{

    public partial class CartDetailDTO
    {
        public int? totalItem { get; set; }
        public double? totalMrp { get; set; }
        public double? totalDiscount { get; set; }
        public double? totalDiscountAmount { get; set; }
        public double? totalSellingPrice { get; set; }

        public List<CartProductsDTO> CartProducts { get; set; }
    }

    public partial class CartProductsDTO
    {
        public string Name { get; set; }

        public string? Description { get; set; }

        public string Category { get; set; }

        public decimal Price { get; set; }
        public int? Quantity { get; set; }


    }

    public partial class OrderDTO
    {
        public int OrderId { get; set; }

        public string UserId { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderDate { get; set; }

        public string? DeliveryDate { get; set; }
        public List<OrderItemDTO> OrderItemDTO { get; set; }


    }
    public partial class OrderItemDTO
    {
        public int orderItemId { get; set; }

        public int productId { get; set; }
        public string productName { get; set; }
        public string productImageUrl { get; set; }
        public string productDescription { get; set; }


        public int quantity { get; set; }

        public decimal priceAtPurchase { get; set; }

    }
}
