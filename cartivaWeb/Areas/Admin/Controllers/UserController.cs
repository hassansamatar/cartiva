using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.ViewModels;
using ApplicationUtility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserController> _logger;

        public UserController(UserManager<ApplicationUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              ApplicationDbContext db,
                              ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _logger = logger;
        }

        // GET: /Admin/User/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();

            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "None";
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // POST: /Admin/User/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.UserName == User.Identity.Name)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsInactive = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                TempData["Success"] = $"User {user.Email} has been deactivated.";
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to deactivate user {Email}: {Errors}", user.Email, errors);
                TempData["Error"] = $"Failed to deactivate user: {errors}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/User/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsInactive = false;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                TempData["Success"] = $"User {user.Email} has been activated.";
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to activate user {Email}: {Errors}", user.Email, errors);
                TempData["Error"] = $"Failed to activate user: {errors}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/User/EditRoles/5
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var companies = await _db.Companies.ToListAsync();

            var model = new EditRolesViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.Name ?? user.Email,
                SelectedRole = currentRoles.FirstOrDefault() ?? "None",
                AvailableRoles = allRoles,
                Companies = companies,
                CompanyId = user.CompanyId
            };

            return View(model);
        }

        // POST: /Admin/User/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Remove existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Assign new role
            if (!string.IsNullOrEmpty(model.SelectedRole) && model.SelectedRole != "None")
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

                // If role is Company, assign selected company
                if (model.SelectedRole == SD.Role_Company)
                {
                    user.CompanyId = model.CompanyId;
                }
                else
                {
                    user.CompanyId = null; // Remove company if role changed to non-company
                }

                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = $"User {user.Email} role updated to {model.SelectedRole ?? "None"}";
            return RedirectToAction(nameof(Index));
        }
    }
}