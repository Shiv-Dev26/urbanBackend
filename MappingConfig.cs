using AutoMapper;
using urbanBackend.Models;
using urbanBackend.Models.DTO;

namespace urbanBackend
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<LoginResponseDTO, ApplicationUser>().ReverseMap();
            CreateMap<RegisterationRequestDTO, ApplicationUser>().ReverseMap();
            CreateMap<UserDetailDTO, ApplicationUser>().ReverseMap();
            CreateMap<UserRequestDTO, ApplicationUser>().ReverseMap();

            CreateMap<ProductDTO, Product>().ReverseMap();
            CreateMap<ProductResponseDTO, Product>().ReverseMap();
            CreateMap<ProductDetailDTO, Product>().ReverseMap();
            CreateMap<ProductDTO, ProductResponseDTO>().ReverseMap();
            CreateMap<CustomerProductResponseDTO, Product>().ReverseMap();
            CreateMap<ProductDTO, CustomerProductResponseDTO>().ReverseMap();


            CreateMap<OrderDTO, OrderItemDTO>().ReverseMap();
            CreateMap<OrderItemDTO, OrderItem>().ReverseMap();
            CreateMap<OrderItemDTO, Product>().ReverseMap();
            CreateMap<OrderDTO, OrderItem>().ReverseMap();


        }
    }
}
