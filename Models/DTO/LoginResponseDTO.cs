namespace urbanBackend.Models.DTO
{
    public class LoginResponseDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string profilepic { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string role { get; set; }
        public string token { get; set; }
        public string phoneOTP { get; set; }
    }

    public class UserDetailDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string profilepic { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string? StreetAddress { get; set; }
        public string phoneNumber { get; set; }
        public string role { get; set; }

    }
}
