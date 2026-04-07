namespace Models.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> replacements);
    }
}
