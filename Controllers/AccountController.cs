using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using urbanBackend.Data;
using urbanBackend.Models.Helper;
using urbanBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using urbanBackend.Models.DTO;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static urbanBackend.Common.GlobalVariables;
using urbanBackend.Common;
using System.Web.Helpers;

namespace urbanBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AccountController(IWebHostEnvironment hostingEnvironment, ApplicationDbContext context, IConfiguration configuration,
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

        #region Login
        /// <summary>
        ///  Login for Admin, Restaurant and Customer.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            var user = _context.ApplicationUsers
                .FirstOrDefault(u => (u.Email.ToLower() == model.emailOrPhone.ToLower()) || u.PhoneNumber.ToLower() == model.emailOrPhone.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, model.password);


            if (user == null || isValid == false)
            {
                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = false,
                    Messages = "user not exists."
                });

            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim("SecurityStamp", user.SecurityStamp),
                    // new Claim(ClaimTypes.Anonymous,user.SecurityStamp)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
            {
                token = tokenHandler.WriteToken(token),
            };
            _mapper.Map(user, loginResponseDTO);

            var userdetail = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (userdetail == null)
            {
                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = false,
                    Messages = "user not exists."
                });
            }
            loginResponseDTO.role = roles[0];

            loginResponseDTO.gender = userdetail.Gender;
            loginResponseDTO.dialCode = userdetail.DialCode;


            return Ok(new
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Messages = "Login successfully.",
                data = loginResponseDTO
            });


        }
        #endregion

        #region Register
        /// <summary>
        ///  Registration for Admin, Restaurant, and Customer.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDTO registerationRequestDTO)
        {
            // Check if the user already exists (either by email or phone number)
            var existingUser = _context.ApplicationUsers
                .FirstOrDefault(x => x.Email.ToLower() == registerationRequestDTO.email.ToLower()
                                  || x.PhoneNumber == registerationRequestDTO.phoneNumber);

            if (existingUser != null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Messages = "User with this email or phone number already exists.";
                return Ok(_response);
            }

            // Validate role
            if (Role.Vendor.ToString() != registerationRequestDTO.role
                && Role.Customer.ToString() != registerationRequestDTO.role)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Messages = "Please enter a valid role.";
                return Ok(_response);
            }

            // Create a new user
            ApplicationUser user = new()
            {
                Email = registerationRequestDTO.email,
                UserName = registerationRequestDTO.email,
                NormalizedEmail = registerationRequestDTO.email.ToUpper(),
                FirstName = registerationRequestDTO.firstName,
                LastName = registerationRequestDTO.lastName,
                PhoneNumber = registerationRequestDTO.phoneNumber,
                Gender = registerationRequestDTO.gender,
                DialCode = registerationRequestDTO.dialCode,
                StreetAddress = registerationRequestDTO.StreetAddress
            };

            try
            {
                // Create the user
                var result = await _userManager.CreateAsync(user, registerationRequestDTO.password);
                if (result.Succeeded)
                {
                    // Ensure the role exists, then assign it to the user
                    if (!await _roleManager.RoleExistsAsync(registerationRequestDTO.role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registerationRequestDTO.role));
                    }

                    await _userManager.AddToRoleAsync(user, registerationRequestDTO.role);

                    // Optionally, auto-login the user after registration
                    LoginRequestDTO loginRequestDTO = new LoginRequestDTO
                    {
                        emailOrPhone = registerationRequestDTO.email,
                        password = registerationRequestDTO.password
                    };
                    var roles = await _userManager.GetRolesAsync(user);
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(secretKey);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim("SecurityStamp", user.SecurityStamp),
                            // new Claim(ClaimTypes.Anonymous,user.SecurityStamp)
                        }),
                        Expires = DateTime.UtcNow.AddDays(7),
                        SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
                    {
                        token = tokenHandler.WriteToken(token),
                    };
                    _mapper.Map(user, loginResponseDTO);

                    var userdetail = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == user.Id);
                    if (userdetail == null)
                    {
                        return Ok(new
                        {
                            StatusCode = HttpStatusCode.OK,
                            IsSuccess = false,
                            Messages = "user not exists."
                        });
                    }
                    loginResponseDTO.role = roles[0];

                    loginResponseDTO.gender = userdetail.Gender;
                    loginResponseDTO.dialCode = userdetail.DialCode;

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = loginResponseDTO;
                    _response.Messages = "Registered successfully.";
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = $"An error occurred while registering: {ex.Message}";
                return Ok(_response);
            }

            // If user creation fails for some reason
            _response.StatusCode = HttpStatusCode.BadRequest;
            _response.IsSuccess = false;
            _response.Messages = "User registration failed.";
            return Ok(_response);
        }
        #endregion

        #region UpdateProfile
        /// <summary>
        ///  Update profile.
        /// </summary>
        [HttpPost]
        [Route("UpdateProfile")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateProfile([FromBody] UserRequestDTO model)
        {
            string currentUserId = (HttpContext.User.Claims.First().Value);
            if (string.IsNullOrEmpty(currentUserId))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Token expired.";
                return Ok(_response);
            }
            var userDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (userDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }
            if (model.Email.ToLower() != userDetail.Email.ToLower())
            {
                var userProfile = await _context.Users.Where(u => u.Email == model.Email && u.Id != currentUserId).FirstOrDefaultAsync();
                if (userProfile != null)
                {
                    if (userProfile.Id != model.Email)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Email already exists.";
                        return Ok(_response);
                    }
                }
            }
            if (model.PhoneNumber.ToLower() != userDetail.PhoneNumber.ToLower())
            {
                var userProfile = await _context.Users.Where(u => u.PhoneNumber == model.PhoneNumber && u.Id != currentUserId).FirstOrDefaultAsync();
                if (userProfile != null)
                {
                    if (userProfile.Id != model.Email)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Phone number already exists.";
                        return Ok(_response);
                    }
                }
            }
            if (Gender.Male.ToString() != model.Gender && Gender.Female.ToString() != model.Gender && Gender.Others.ToString() != model.Gender)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid gender.";
                return Ok(_response);
            }

            var mappedData = _mapper.Map(model, userDetail);
            _context.Update(userDetail);
            await _context.SaveChangesAsync();

            var userProfileDetail = await _context.ApplicationUsers.Where(u => u.Id == currentUserId).FirstOrDefaultAsync();
            var updateProfile = _mapper.Map(model, userProfileDetail);
            _context.ApplicationUsers.Update(updateProfile);
            await _context.SaveChangesAsync();

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Messages = "Profile updated successfully.";
            return Ok(_response);
        }
        #endregion

        #region GetProfileDetail
        /// <summary>
        ///  Get profile.
        /// </summary>
        [HttpGet]
        [Route("GetProfileDetail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfileDetail()
        {
            string currentUserId = (HttpContext.User.Claims.First().Value);
            if (string.IsNullOrEmpty(currentUserId))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Token expired.";
                return Ok(_response);
            }
            var userDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (userDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            var mappedData = _mapper.Map<UserDetailDTO>(userDetail);

            var userProfileDetail = await _context.ApplicationUsers.Where(u => u.Id == currentUserId).FirstOrDefaultAsync();
            var roles = await _userManager.GetRolesAsync(userDetail);
            var updateProfile = _mapper.Map(userProfileDetail, mappedData);

            mappedData.role = roles[0];

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = mappedData;
            _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
            return Ok(_response);
        }
        #endregion

        #region ResetPassword
        /// <summary>
        ///  Reset password.
        /// </summary>
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.email))
                {
                    var applicationUser = _userManager.FindByEmailAsync(model.email).GetAwaiter().GetResult();
                    if (applicationUser != null)
                    {
                        var password = Crypto.HashPassword(model.newPassword);
                        applicationUser.PasswordHash = password;

                        await _userManager.UpdateAsync(applicationUser);

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Data = new { NewPassword = model.newPassword };
                        _response.Messages = "Password reset successfully.";
                        return Ok(_response);
                    }
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user.";
                    return Ok(_response);
                }
                else
                {
                    var applicationUser = _context.ApplicationUsers.FirstOrDefault(x => (x.PhoneNumber == model.phoneNumber));
                    if (applicationUser != null)
                    {
                        var password = Crypto.HashPassword(model.newPassword);
                        applicationUser.PasswordHash = password;

                        await _userManager.UpdateAsync(applicationUser);

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Data = new { NewPassword = model.newPassword };
                        _response.Messages = "Password reset successfully.";
                        return Ok(_response);
                    }
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user.";
                    return Ok(_response);
                }
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
