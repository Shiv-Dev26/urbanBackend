using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using urbanBackend.Data;
using urbanBackend.Models.Helper;
using urbanBackend.Models;
using Microsoft.AspNetCore.Authorization;
using urbanBackend.Models.DTO;
using System.Net;
using urbanBackend.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Web.Helpers;
using Google.Api.Gax.ResourceNames;

namespace urbanBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public CustomerController(IWebHostEnvironment hostingEnvironment, ApplicationDbContext context, IConfiguration configuration,
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

        #region addOrUpdateProductCart
        /// <summary>
        /// Add or update product in the cart.
        /// </summary>
        [HttpPost]
        [Route("addOrUpdateProductCart")]
        [AllowAnonymous]
        public async Task<IActionResult> addOrUpdateProductCart(int productId)
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
                if (!roles.Contains("Customer"))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not authorized.";
                    return Ok(_response);
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Product not found.";
                    return Ok(_response);
                }

                // Update existing cart

                var existingCart = _context.Carts
                    .Where(u => u.UserId == currentUserId && u.ProductId == productId)
                    .FirstOrDefault();

                if (existingCart != null)
                {

                    existingCart.ModifyDate = DateTime.Now;
                    existingCart.Quantity += 1;
                    _context.Update(existingCart);
                    await _context.SaveChangesAsync();


                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Cart item updated successfully.";
                    return Ok(_response);
                }
                // Add new cart
                var newCart = new Cart
                {
                    ProductId = productId,
                    CreateDate = DateTime.Now,
                    UserId = currentUserId,
                    Quantity = 1
                };
                await _context.AddAsync(newCart);
                await _context.SaveChangesAsync();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Cart and item added successfully.";
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


        #region getCartList
        /// <summary>
        /// Add or update product in the cart.
        /// </summary>
        [HttpGet]
        [Route("getCartList")]
        [AllowAnonymous]
        public async Task<IActionResult> getCartList()
        {
            try
            {
                // Step 1: Get current user ID from claims
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                // Step 2: Fetch user details
                var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);
                if (currentUserDetail == null)
                {
                    return GenerateResponse(HttpStatusCode.OK, false, ResponseMessages.msgUserNotFound);
                }

                // Step 3: Check user role
                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (!roles.Contains("Customer"))
                {
                    return GenerateResponse(HttpStatusCode.OK, false, "Not authorized.");
                }

                // Step 4: Fetch the cart items for the current user
                var cart = await _context.Carts.Where(u => u.UserId == currentUserId).ToListAsync();

                if (!cart.Any())
                {
                    return GenerateResponse(HttpStatusCode.OK, false, "No cart found for the current user.");
                }

                // Step 5: Get the product IDs from the cart items
                var productIds = cart.Select(ci => ci.ProductId).ToList();

                // Step 6: Fetch product details for the items in the cart
                var products = _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .AsEnumerable()  // Move to client-side evaluation
                    .Select(p => new CartProductsDTO
                    {
                        Name = p.Name,
                        Description = p.Description,
                        Category = p.Category,
                        Price = p.Price,
                        Quantity = cart.FirstOrDefault(ci => ci.ProductId == p.ProductId)?.Quantity ?? 0
                    })
                    .ToList(); // Use ToList for in-memory collection

                // Step 7: Calculate totals
                var totalItem = products.Sum(p => p.Quantity);
                var totalMrp = products.Sum(p => (double)p.Price * p.Quantity);

                // Dynamic discount logic (example)
                var totalDiscount = totalMrp > 1000 ? 10.0 : 5.0; // 10% discount for orders above 1000, else 5%
                var totalDiscountAmount = totalMrp * totalDiscount / 100;
                var totalSellingPrice = totalMrp - totalDiscountAmount;

                // Step 8: Prepare the response DTO
                CartDetailDTO cartDetail = new CartDetailDTO
                {
                    totalItem = totalItem,
                    totalMrp = totalMrp,
                    totalDiscount = totalDiscount,
                    totalDiscountAmount = totalDiscountAmount,
                    totalSellingPrice = totalSellingPrice,
                    CartProducts = products
                };

                // Step 9: Set response
                return GenerateResponse(HttpStatusCode.OK, true, ResponseMessages.msgListFoundSuccess, cartDetail);
            }
            catch (Exception ex)
            {
                return GenerateResponse(HttpStatusCode.InternalServerError, false, ex.Message);
            }
        }

        // Helper method to generate standardized response
        private IActionResult GenerateResponse(HttpStatusCode statusCode, bool isSuccess, string message, object data = null)
        {
            _response.StatusCode = statusCode;
            _response.IsSuccess = isSuccess;
            _response.Messages = message;
            _response.Data = data;
            return Ok(_response);
        }
        #endregion


        #region removeFromCart
        /// <summary>
        /// Remove item from cart.
        /// </summary>
        /// <param name="cartItemId">The ID of the cart item to remove.</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("RemoveFromCart")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveFromCart(int cartId, int cartItemId, int productId)
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
                if (!roles.Contains("Customer"))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not authorized.";
                    return Ok(_response);
                }

                var existingCart = _context.Carts
                             .Where(u => u.UserId == currentUserId && u.CartId == cartId && u.ProductId == productId)
                             .FirstOrDefault();

                if (existingCart == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any.";
                    return Ok(_response);
                }

                existingCart.Quantity--;

                if (existingCart.Quantity == 0)
                {
                    _context.Remove(existingCart);
                }
                else
                {
                    _context.Update(existingCart);
                }

                _context.SaveChanges();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Cart item removed successfully.";
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

        #region addOrder
        /// <summary>
        /// Add order.
        /// </summary>
        [HttpPost]
        [Route("addOrder")]
        [AllowAnonymous]
        public async Task<IActionResult> addOrder()
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
                if (!roles.Contains("Customer"))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not authorized.";
                    return Ok(_response);
                }

                var cart = _context.Carts.Where(u => u.UserId == currentUserId).ToList();
                if (!cart.Any())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any.";
                    return Ok(_response);
                }

                decimal? total = 0;

                foreach (var item in cart)
                {
                    var product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                    if (product != null)
                    {
                        total += product.Price * item.Quantity;
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.NotFound;
                        _response.IsSuccess = false;
                        _response.Messages = $"Product with ID {item.ProductId} not found.";
                        return Ok(_response);
                    }
                }

                Order order = new Order
                {
                    UserId = currentUserId,
                    OrderStatus = "Confirm",
                    PaymentStatus = "Paid",
                    OrderDate = DateTime.Now,
                    TotalAmount = total, // Set total calculated from cart items
                    DeliveryDate = DateTime.Now.AddDays(7) // Assuming 7-day delivery time
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                foreach (var item in cart)
                {
                    var product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                    if (product != null)
                    {
                        // Calculate total
                        decimal priceAtPurchase = product.Price;
                        total += priceAtPurchase * item.Quantity;

                        // Create an OrderItem for each cart item
                        OrderItem orderItem = new OrderItem
                        {
                            OrderId = order.OrderId, // Use the newly created order's OrderId
                            ProductId = item?.ProductId ?? 0,
                            Quantity = item?.Quantity ?? 1,
                            PriceAtPurchase = priceAtPurchase // Store the price at purchase time
                        };

                        // Add OrderItem to the database
                        _context.OrderItems.Add(orderItem);
                        _context.SaveChanges();
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.NotFound;
                        _response.IsSuccess = false;
                        _response.Messages = $"Product with ID {item.ProductId} not found.";
                        return Ok(_response);
                    }
                }

                // Update the total amount for the order after processing all items
                order.TotalAmount = total;
                _context.SaveChanges();

                _context.Carts.RemoveRange(cart);
                _context.SaveChanges();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Order placed successfully.";
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

        [HttpGet]
        [Route("getOrderDetail")]
        [AllowAnonymous]
        public async Task<IActionResult> getOrderDetail(int orderId)
        {
            try
            {
                // Step 1: Get current user ID from claims
                string currentUserId = HttpContext.User.Claims.FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return GenerateResponse(HttpStatusCode.Unauthorized, false, "Token expired.");
                }

                // Step 2: Fetch user details
                var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);
                if (currentUserDetail == null)
                {
                    return GenerateResponse(HttpStatusCode.NotFound, false, "User not found.");
                }

                // Step 3: Check user role
                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (!roles.Contains("Customer"))
                {
                    return GenerateResponse(HttpStatusCode.Forbidden, false, "Not authorized.");
                }

                // Step 4: Fetch order details
                var order = _context.Orders
                    .FirstOrDefault(o => o.UserId == currentUserId && o.OrderId == orderId);

                if (order == null)
                {
                    return GenerateResponse(HttpStatusCode.NotFound, false, "Order not found.");
                }

                // Step 5: Fetch order items
                var orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToList();

                if (!orderItems.Any())
                {
                    return GenerateResponse(HttpStatusCode.NotFound, false, "No items found for this order.");
                }

                var productIds = orderItems.Select(oi => oi.ProductId).ToList();
                var products = _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionary(p => p.ProductId);

                // Step 6: Calculate total amount and create OrderDTO
                decimal total = orderItems.Sum(oi => oi.PriceAtPurchase * oi.Quantity);

                var orderDTO = new OrderDTO
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    TotalAmount = total,
                    OrderStatus = order.OrderStatus,
                    PaymentStatus = order.PaymentStatus,
                    OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                    DeliveryDate = order.DeliveryDate?.ToString("yyyy-MM-dd"),
                    OrderItemDTO = orderItems.Select(oi =>
                    {
                        var product = products[oi.ProductId];
                        return new OrderItemDTO
                        {
                            orderItemId = oi.OrderItemId,
                            productId = oi.ProductId,
                            productName = product.Name,
                            productImageUrl = product.Image1,
                            productDescription = product.Description,
                            quantity = oi.Quantity,
                            priceAtPurchase = oi.PriceAtPurchase
                        };
                    }).ToList()
                };

                // Step 7: Return the order details
                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Data = orderDTO
                });
            }
            catch (Exception ex)
            {
                return GenerateResponse(HttpStatusCode.InternalServerError, false, ex.Message);
            }
        }

        [HttpGet]
        [Route("getOrderList")]
        [AllowAnonymous]
        public async Task<IActionResult> getOrderList()
        {
            try
            {
                // Step 1: Get current user ID from claims
                string currentUserId = HttpContext.User.Claims.FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return GenerateResponse(HttpStatusCode.Unauthorized, false, "Token expired.");
                }

                // Step 2: Fetch user details
                var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);
                if (currentUserDetail == null)
                {
                    return GenerateResponse(HttpStatusCode.NotFound, false, "User not found.");
                }

                // Step 3: Check user role
                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (!roles.Contains("Customer"))
                {
                    return GenerateResponse(HttpStatusCode.Forbidden, false, "Not authorized.");
                }

                // Step 4: Fetch all orders associated with the user
                var orders = _context.Orders
                    .Where(o => o.UserId == currentUserId)
                    .ToList();

                if (!orders.Any())
                {
                    return GenerateResponse(HttpStatusCode.NotFound, false, "No orders found.");
                }

                // Step 5: Create a list of OrderDTOs to return
                var orderListDTO = orders.Select(order => new OrderDTO
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    TotalAmount = order.TotalAmount ?? 0,
                    OrderStatus = order.OrderStatus,
                    PaymentStatus = order.PaymentStatus,
                    OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                    DeliveryDate = order.DeliveryDate?.ToString("yyyy-MM-dd"),
                    //OrderItemDTO = _context.OrderItems
                    //    .Where(oi => oi.OrderId == order.OrderId)
                    //    .Select(oi => new OrderItemDTO
                    //    {
                    //        OrderItemId = oi.OrderItemId,
                    //        ProductId = oi.ProductId,
                    //        Quantity = oi.Quantity,
                    //        PriceAtPurchase = oi.PriceAtPurchase
                    //    }).ToList()
                }).ToList();

                // Step 6: Return the list of orders
                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Data = orderListDTO
                });
            }
            catch (Exception ex)
            {
                return GenerateResponse(HttpStatusCode.InternalServerError, false, ex.Message);
            }
        }


        #region customerProductList
        /// <summary>
        ///  Get product list.
        /// </summary>
        [HttpGet]
        [Route("customerProductList")]
        [AllowAnonymous]
        public async Task<IActionResult> customerProductList([FromQuery] ProductListDTO model)
        {
            try
            {
                string currentUserId = HttpContext.User.Claims.FirstOrDefault()?.Value;

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);
                    if (currentUserDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgUserNotFound;
                        return Ok(_response);
                    }

                    var roles = await _userManager.GetRolesAsync(currentUserDetail);
                    if (!roles.Contains("Customer"))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "not authorized.";
                        return Ok(_response);
                    }
                }

                var products = _context.Products.ToList();

                List<CustomerProductResponseDTO> productList = products.Select(item => new CustomerProductResponseDTO
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    Price = item.Price,
                    ModifyDate = item.ModifyDate?.ToShortDateString()
                }).ToList();

                // Apply search filter
                if (!string.IsNullOrEmpty(model.searchQuery))
                {
                    // Trim whitespace from the search query
                    string trimmedSearchQuery = model.searchQuery.Trim();

                    productList = productList.Where(u => u.Name.ToLower().Contains(trimmedSearchQuery.ToLower())
                        || u.Category.ToLower().Contains(trimmedSearchQuery.ToLower())).ToList();
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
                var obj = new FilterationResponseModel<CustomerProductResponseDTO>
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



    }
}
