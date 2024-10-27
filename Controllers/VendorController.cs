using AutoMapper;
using urbanBackend.Models.Dtos;
using urbanBackend.Models.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using static urbanBackend.Common.GlobalVariables;
using urbanBackend.Models;
using urbanBackend.Data;
using Microsoft.EntityFrameworkCore;

using System.Globalization;
using urbanBackend.Common;
using urbanBackend.Models.DTO;
using System.Data.Entity;

namespace urbanBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public VendorController(IWebHostEnvironment hostingEnvironment, ApplicationDbContext context, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _response = new();
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _roleManager = roleManager;
            _hostingEnvironment = hostingEnvironment;
        }

        #region addOrUpdateProduct
        /// <summary>
        ///  Get product list.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("addOrUpdateProduct")]
        [AllowAnonymous]
        public async Task<IActionResult> addOrUpdateProduct([FromBody] ProductDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }

                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (!roles.Contains("Vendor"))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "not authorized.";
                    return Ok(_response);
                }


                if (CategoryName.Male.ToString() != model.Category &&
                   CategoryName.Female.ToString() != model.Category &&
                   CategoryName.Unisex.ToString() != model.Category)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter a valid category.";
                    return Ok(_response);
                }

                if (model.ProductId > 0)
                {
                    var product = _context.Products
           .Where(u => u.VendorId == currentUserId && u.ProductId == model.ProductId).FirstOrDefault();

                    if (product == null)
                    {
                        _response.StatusCode = HttpStatusCode.NotFound; // 404
                        _response.IsSuccess = false;
                        _response.Messages = "Product does not exist.";
                        return Ok(_response);
                    }

                    // Map and update product
                    _mapper.Map(model, product);
                    product.ModifyDate = DateTime.Now;

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    var responses = _mapper.Map<ProductResponseDTO>(product);
                    responses.ModifyDate = product.ModifyDate?.ToString("dd-MM-yyyy");

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = responses;
                    _response.Messages = "Product updated successfully.";
                    return Ok(_response);
                }

                Product newProduct = new Product();
                _mapper.Map(model, newProduct);
                newProduct.VendorId = currentUserId;
                newProduct.CreateDate = DateTime.Now;

                _context.Add(newProduct);
                await _context.SaveChangesAsync();

                var response = _mapper.Map<ProductResponseDTO>(newProduct);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Product added successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region getProductList
        /// <summary>
        ///  Get product list.
        /// </summary>
        [HttpGet]
        [Route("getProductList")]
        [AllowAnonymous]
        public async Task<IActionResult> getProductList([FromQuery] ProductListDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }

                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (!roles.Contains("Vendor"))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "not authorized.";
                    return Ok(_response);
                }

                var products = _context.Products
                    .Where(u => u.VendorId == currentUserId)
                    .ToList();

                List<ProductResponseDTO> productList = products.Select(item => new ProductResponseDTO
                {
                    ProductId = item.ProductId,
                    VendorId = item.VendorId,
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    Price = item.Price,
                    StockCount = item.StockCount,
                    CreateDate = item.CreateDate.ToShortDateString(),
                    ModifyDate = item.ModifyDate?.ToShortDateString()
                }).ToList();

                // Apply search filter
                if (!string.IsNullOrEmpty(model.searchQuery))
                {
                    productList = productList.Where(u => u.Name.ToLower().Contains(model.searchQuery.ToLower())
                        || u.Category.ToLower().Contains(model.searchQuery.ToLower())).ToList();
                }
                if (!string.IsNullOrEmpty(model.category))
                {
                    productList = productList.Where(u => u.Category.ToLower() == model.category.ToLower()).ToList();
                }
                if (!string.IsNullOrEmpty(model.sortOption))
                {
                    switch (model.sortOption.ToLower())
                    {
                        case "price-asc":
                            productList = productList.OrderBy(u => u.Price).ToList();
                            break;
                        case "price-desc":
                            productList = productList.OrderByDescending(u => u.Price).ToList();
                            break;
                        default:
                            // Default sorting can be done based on another property, e.g., Name
                            productList = productList.OrderBy(u => u.Name).ToList();
                            break;
                    }
                }
                int count = productList.Count();

                // Paging logic
                int CurrentPage = model.pageNumber;
                int PageSize = model.pageSize;
                int TotalCount = count;
                int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

                var items = productList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

                var previousPage = CurrentPage > 1 ? "Yes" : "No";
                var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

                // Prepare response
                var obj = new FilterationResponseModel<ProductResponseDTO>
                {
                    totalCount = TotalCount,
                    pageSize = PageSize,
                    currentPage = CurrentPage,
                    totalPages = TotalPages,
                    previousPage = previousPage,
                    nextPage = nextPage,
                    searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery,
                    dataList = items
                };

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = obj;
                _response.Messages = ResponseMessages.msgListFoundSuccess;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region getProductDetail
        /// <summary>
        ///  Get product detail.
        /// </summary>
        [HttpGet]
        [Route("getProductDetail")]
        [AllowAnonymous]
        public async Task<IActionResult> getProductDetail(int productId)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }

                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (roles.Contains("Vendor,Customer"))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "not authorized.";
                    return Ok(_response);
                }

                var products = _context.Products
                    .Where(u => u.ProductId == productId)
                    .FirstOrDefault();

                if (products == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "not found.";
                    return Ok(_response);
                }

                var response = _mapper.Map<ProductDetailDTO>(products);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "product detail shown successfully.";
                _response.Data = response;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region deleteProduct
        /// <summary>
        ///  delete product.
        /// </summary>
        [HttpDelete]
        [Route("deleteProduct")]
        [AllowAnonymous]
        public async Task<IActionResult> deleteProduct(int productId)
        {
            string currentUserId = (HttpContext.User.Claims.First().Value);
            if (string.IsNullOrEmpty(currentUserId))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Token expired.";
                return Ok(_response);
            }

            var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (currentUserDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            var roles = await _userManager.GetRolesAsync(currentUserDetail);
            if (!roles.Contains("Vendor"))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "not authorized.";
                return Ok(_response);
            }

            var product = _context.Products
           .Where(u => u.VendorId == currentUserId && u.ProductId == productId).FirstOrDefault();

            if (product == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound; // 404
                _response.IsSuccess = false;
                _response.Messages = "Product does not exist.";
                return Ok(_response);
            }

            _context.Remove(product);
            await _context.SaveChangesAsync();

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Messages = "Product deleted successfully.";
            return Ok(_response);
        }
        #endregion
    }
}
