using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Cartiva.Infrastructure.ImageServices
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveImage(IFormFile file)
        {
            string root = _environment.WebRootPath;
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            string folder = Path.Combine(root, "images/products");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/images/products/" + fileName;
        }

        public void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            string root = _environment.WebRootPath;
            string path = Path.Combine(root, imageUrl.TrimStart('/'));

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}