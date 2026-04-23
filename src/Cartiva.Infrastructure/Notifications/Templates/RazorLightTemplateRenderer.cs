using Cartiva.Infrastructure.Notifications.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace Cartiva.Infrastructure.Notifications.Templates;

public class RazorLightTemplateRenderer : ITemplateRenderer
{
    private readonly RazorLightEngine _engine;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RazorLightTemplateRenderer> _logger;
    private readonly string _templatesPath;

    public RazorLightTemplateRenderer(
        IMemoryCache cache,
        ILogger<RazorLightTemplateRenderer> logger)
    {
        _cache = cache;
        _logger = logger;

        // Templates path relative to Infrastructure project
        _templatesPath = Path.Combine(AppContext.BaseDirectory, "Templates");

        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templatesPath)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"template_{templateName}_{typeof(TModel).Name}";

            // Try to get cached compiled template
            if (_cache.TryGetValue<string>(cacheKey, out var cachedTemplate) && cachedTemplate != null)
            {
                _logger.LogDebug("Using cached template for {TemplateName}", templateName);
            }

            // Render the template
            var templatePath = $"{templateName}.cshtml";
            var result = await _engine.CompileRenderAsync(templatePath, model);

            // Cache the result for 1 hour
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", templateName);

            // Return a fallback generic template
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Notification</title>
</head>
<body>
    <h1>Notification</h1>
    <p>Error rendering template: {templateName}</p>
</body>
</html>";
        }
    }
}
