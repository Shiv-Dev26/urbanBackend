namespace urbanBackend.Models.DTO
{
    public class UploadDTO
    {
        public partial class UploadProductImageDTO
        {
            public int ProductId { get; set; }
            public List<IFormFile>? ProductImage { get; set; }
        }
        public class UploadProfilePicDto
        {
            public IFormFile profilePic { get; set; }
            public string id { get; set; }
        }
    }
}
