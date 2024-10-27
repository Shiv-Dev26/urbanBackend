using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using urbanBackend.Models;
using System.Collections.Generic;
using static urbanBackend.Models.DTO.UploadDTO;
using urbanBackend.Data;

namespace urbanBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public UploadController(IWebHostEnvironment env, ApplicationDbContext context)
        {
            _env = env;
            _context = context;

        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadProductImages([FromForm] UploadProductImageDTO dto)
        {
            // Find the product by ID to update the existing product images
            var product = await _context.Products.FindAsync(dto.ProductId);

            if (product == null)
            {
                return NotFound(new { Message = "Product not found." });
            }

            // Folder path for saving images
            string imagesPath = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(imagesPath))
                Directory.CreateDirectory(imagesPath);

            // Delete existing images if they exist
            DeleteProductImages(product, imagesPath);

            // List to hold the relative paths of the uploaded images
            var imagePaths = new List<string>();

            // Iterate over each uploaded image
            if (dto.ProductImage != null)
            {
                foreach (var imageFile in dto.ProductImage)
                {
                    var imagePath = await SaveImage(imageFile, imagesPath);
                    imagePaths.Add(imagePath);
                }
            }

            // Update image properties with the new paths if they exist
            if (imagePaths.Count > 0) product.Image1 = imagePaths.ElementAtOrDefault(0);
            if (imagePaths.Count > 1) product.Image2 = imagePaths.ElementAtOrDefault(1);
            if (imagePaths.Count > 2) product.Image3 = imagePaths.ElementAtOrDefault(2);
            if (imagePaths.Count > 3) product.Image4 = imagePaths.ElementAtOrDefault(3);

            // Update the product record in the database
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { product });
        }

        // Method to delete existing product images
        private void DeleteProductImages(Product product, string imagesPath)
        {
            // Define image properties to delete
            var imagesToDelete = new List<string>
            {
                product.Image1,
                product.Image2,
                product.Image3,
                product.Image4
            };

            foreach (var imagePath in imagesToDelete)
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    string fullPath = Path.Combine(imagesPath, Path.GetFileName(imagePath));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath); // Delete the image from wwwroot
                    }
                }
            }

            // Reset image properties to null
            product.Image1 = null;
            product.Image2 = null;
            product.Image3 = null;
            product.Image4 = null;
        }



        [HttpPost("uploadProfilePic")]
        public async Task<IActionResult> UploadProfilePic([FromForm] UploadProfilePicDto dto)
        {
            // Define the directory for profile pictures
            string profilePicPath = Path.Combine(_env.WebRootPath, "images", "profilePics");
            if (!Directory.Exists(profilePicPath))
                Directory.CreateDirectory(profilePicPath);

            // Find the user in the database
            var user = await _context.Users.FindAsync(dto.id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user already has a profile picture and delete it
            if (!string.IsNullOrEmpty(user.ProfilePic))
            {
                string oldImagePath = Path.Combine(profilePicPath, Path.GetFileName(user.ProfilePic));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath); // Delete the old image from wwwroot
                }
            }

            // Save the new profile picture and get the path
            string savedPath = await SaveProfileImage(dto.profilePic, profilePicPath);

            // Update user's profile picture path in the database
            user.ProfilePic = savedPath;
            await _context.SaveChangesAsync();

            return Ok(new { ProfilePicPath = savedPath });
        }

        private async Task<string> SaveImage(IFormFile image, string folderPath)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return Path.Combine("images", "products", fileName).Replace("\\", "/");
        }
        private async Task<string> SaveProfileImage(IFormFile image, string folderPath)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return Path.Combine("images", "profilePics", fileName).Replace("\\", "/");
        }
    }
}
