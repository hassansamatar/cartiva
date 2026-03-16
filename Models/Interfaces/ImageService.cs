using Microsoft.AspNetCore.Http;

namespace Models.Interfaces
{
    public interface IImageService
    {
        Task<string> SaveImage(IFormFile file);
        void DeleteImage(string? imageUrl);
    }
}