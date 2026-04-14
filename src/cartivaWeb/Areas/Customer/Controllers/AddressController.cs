using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Infrastructure.AddressService;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [ApiController]
    [Route("api/address")]
    public class AddressController : ControllerBase
    {
        private readonly AddressLookupService _service;

        public AddressController(AddressLookupService service)
        {
            _service = service;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest();

            var result = await _service.SearchAsync(q);

            return Content(result, "application/json");
        }
    }
}
