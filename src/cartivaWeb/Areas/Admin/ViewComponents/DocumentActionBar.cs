using Microsoft.AspNetCore.Mvc;

namespace cartivaWeb.Areas.Admin.ViewComponents
{
    public class DocumentActionBar : ViewComponent
    {
        public IViewComponentResult Invoke(DocumentActionBarViewModel model)
        {
            return View(model);
        }
    }
}
