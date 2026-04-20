using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Domain.ViewModels;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: /Admin/User/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            var userRoles = await _userService.GetUserRolesAsync(users);
            var companyUsers = _userService.GetUsersByCompany(users);

            ViewBag.UserRoles = userRoles;
            ViewBag.CompanyUsers = companyUsers;
            return View(users);
        }

        // POST: /Admin/User/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Deactivate(string id)
        {
            var result = await _userService.DeactivateUserAsync(id, User.Identity?.Name ?? "");

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/User/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Activate(string id)
        {
            var result = await _userService.ActivateUserAsync(id);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/User/EditRoles/5
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> EditRoles(string id)
        {
            var model = await _userService.GetEditRolesViewModelAsync(id);
            if (model == null) return NotFound();

            return View(model);
        }

        // POST: /Admin/User/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> EditRoles(EditRolesViewModel model)
        {
            var result = await _userService.UpdateUserRolesAsync(model.UserId, model.SelectedRole, model.CompanyId);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}