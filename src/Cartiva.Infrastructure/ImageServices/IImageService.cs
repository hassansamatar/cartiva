using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Infrastructure.ImageServices
{
   
    public interface IImageService
    {
        Task<string> SaveImage(IFormFile file);
        void DeleteImage(string? imageUrl);
    
}
}
