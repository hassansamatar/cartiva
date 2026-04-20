using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Domain;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CompanyController : Controller
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        // GET: Company List
        public async Task<IActionResult> Index()
        {
            var companyList = await _companyService.GetAllCompaniesWithStatsAsync();
            return View(companyList);
        }

        // GET: Upsert (Create/Edit)
        public async Task<IActionResult> Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new Company { IsActive = true });
            }

            var companyObj = await _companyService.GetCompanyByIdAsync(id.Value);
            if (companyObj == null) return NotFound();

            return View(companyObj);
        }

        // POST: Upsert (Create/Edit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Company companyObj)
        {
            if (!ModelState.IsValid)
                return View(companyObj);

            CompanyOperationResult result;

            if (companyObj.Id == 0)
            {
                result = await _companyService.CreateCompanyAsync(companyObj);
            }
            else
            {
                bool canChangeActiveStatus = User.IsInRole(SD.Role_Admin);
                result = await _companyService.UpdateCompanyAsync(companyObj, canChangeActiveStatus);
            }

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction("Index");
        }

        // GET: Delete
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var companyObj = await _companyService.GetCompanyByIdAsync(id.Value);
            if (companyObj == null) return NotFound();

            // Check if company can be deleted
            var hasOrders = await _companyService.HasOrdersAsync(id.Value);
            var hasActiveUsers = await _companyService.HasActiveUsersAsync(id.Value);
            var companyUsers = await _companyService.GetCompanyUsersAsync(id.Value);

            ViewBag.HasOrderHistory = hasOrders;
            ViewBag.HasActiveUsers = hasActiveUsers;
            ViewBag.CanDelete = !hasOrders && !hasActiveUsers;
            ViewBag.CompanyUsers = companyUsers;
            ViewBag.UserCount = companyUsers.Count;
            ViewBag.ActiveUserCount = companyUsers.Count(u => u.IsActive);

            return View(companyObj);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (id == null) return NotFound();

            var result = await _companyService.DeleteCompanyAsync(id.Value);

            if (result.Success)
            {
                TempData["success"] = result.Message;
                return RedirectToAction("Index");
            }

            if (result.WasDeactivatedInstead)
            {
                TempData["error"] = result.Message;
                return RedirectToAction("Delete", new { id = result.EntityId });
            }

            TempData["error"] = result.Message;
            return RedirectToAction("Index");
        }

        // Optional: Toggle Active/Inactive directly from Index
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _companyService.ToggleStatusAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction("Index");
        }
    }
}