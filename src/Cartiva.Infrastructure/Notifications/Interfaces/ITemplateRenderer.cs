namespace Cartiva.Infrastructure.Notifications.Interfaces;

public interface ITemplateRenderer
{
    Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken cancellationToken = default);
}
