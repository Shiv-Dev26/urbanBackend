using Microsoft.AspNetCore.Identity;

namespace urbanBackend.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DialCode { get; set; }
        public string Gender { get; set; }
        public string? ProfilePic { get; set; }
        public string? StreetAddress { get; set; }

    }
}
