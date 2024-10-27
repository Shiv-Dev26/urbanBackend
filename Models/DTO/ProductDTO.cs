using System.ComponentModel.DataAnnotations;

namespace urbanBackend.Models.DTO
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string Category { get; set; }

        public decimal Price { get; set; }

        public int StockCount { get; set; }


    }
    public class ProductResponseDTO
    {
        public int ProductId { get; set; }
        public string VendorId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public string Category { get; set; }
        public decimal Price { get; set; }
        public string? Image1 { get; set; }
        public string? Image2 { get; set; }
        public string? Image3 { get; set; }
        public string? Image4 { get; set; }
        public int StockCount { get; set; }
        public string? CreateDate { get; set; }
        public string? ModifyDate { get; set; }
    }

    public class ProductDetailDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public string? Image1 { get; set; }
        public string? Image2 { get; set; }
        public string? Image3 { get; set; }
        public string? Image4 { get; set; }
        public decimal Price { get; set; }
    }


    public class CustomerProductResponseDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string? ModifyDate { get; set; }
    }


    public class ProductListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? category { get; set; }
        public string? searchQuery { get; set; }
        public string? sortOption { get; set; } 
    }

}
