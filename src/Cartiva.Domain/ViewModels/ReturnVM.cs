using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cartiva.Domain.ViewModels
{
    public class ReturnVm
    {
        public int OrderDetailId { get; set; }

        public OrderDetail? OrderDetail { get; set; }

        public int DaysRemaining { get; set; }

        public IEnumerable<SelectListItem> Reasons { get; set; } = new List<SelectListItem>();
    }
}