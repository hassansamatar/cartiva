using Microsoft.AspNetCore.Hosting;

namespace Cartiva.Infrastructure.EmailServices
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _templatePath;

        public EmailTemplateService(IWebHostEnvironment env)
        {
            _templatePath = Path.Combine(env.WebRootPath, "templates", "email");
        }

        public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> replacements)
        {
            var filePath = Path.Combine(_templatePath, $"{templateName}.html");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Email template '{templateName}' not found.", filePath);

            var template = await File.ReadAllTextAsync(filePath);

            foreach (var kvp in replacements)
            {
                template = template.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            return template;
        }
    }
}
