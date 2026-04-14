namespace Cartiva.Infrastructure.EmailServices
{
    public interface IEmailTemplateService
    {
        Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> replacements);
    }
}
