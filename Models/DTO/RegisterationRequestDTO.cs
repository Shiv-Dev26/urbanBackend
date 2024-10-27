using System.ComponentModel.DataAnnotations;

namespace urbanBackend.Models.DTO
{
    public class RegisterationRequestDTO
    {
        [Required]
        [EmailAddress]
        public string email { get; set; }
        [Required]
        public string firstName { get; set; }
        public string lastName { get; set; }
        [Required]
        public string? gender { get; set; }
        [Required]
        public string? dialCode { get; set; }
        [Required]
        public string phoneNumber { get; set; }
      
        public string StreetAddress { get; set; }
       
        [Required]
        public string password { get; set; }
        [Required]
        public string role { get; set; }
    }

    public class ResetPasswordDTO
    {
        public string email { get; set; }
        public string phoneNumber { get; set; }
        [Required]
        public string newPassword { get; set; }
    }

    public class UserRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string? DialCode { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
    }
}
