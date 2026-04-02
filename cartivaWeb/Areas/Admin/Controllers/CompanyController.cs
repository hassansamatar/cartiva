using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using MyUtility;
using System;
using System.Linq;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: Company List
        public IActionResult Index()
        {
            var companies = _db.Companies.ToList();

            var companyUsers = _db.Users
                .Where(u => u.CompanyId != null)
                .ToList();

            var orders = _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .ToList();

            var companyList = companies.Select(company =>
            {
                var companyUser = companyUsers
                    .FirstOrDefault(u => u.CompanyId == company.Id);

                var companyOrders = orders
                    .Where(o => o.ApplicationUser.CompanyId == company.Id)
                    .ToList();

                string paymentStatus = "No Orders";

                if (companyOrders.Any())
                {
                    if (companyOrders.Any(o =>
                            o.PaymentStatus == SD.PaymentStatusDeferred &&
                            o.PaymentDueDate < DateOnly.FromDateTime(DateTime.Now)))
                    {
                        paymentStatus = "Overdue";
                    }
                    else if (companyOrders.Any(o =>
                            o.PaymentStatus == SD.PaymentStatusDeferred))
                    {
                        paymentStatus = "Pending";
                    }
                    else if (companyOrders.All(o =>
                            o.PaymentStatus == SD.PaymentStatusApproved))
                    {
                        paymentStatus = "Paid";
                    }
                }

                return new CompanyListVM
                {
                    Company = company,
                    ContactPerson = companyUser?.Name ?? "—",
                    PaymentStatus = paymentStatus
                };

            }).ToList();

            return View(companyList);
        }

        // GET: Upsert (Create/Edit)
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                // Create new company
                return View(new Company { IsActive = true }); // default active
            }

            // Edit existing company
            var companyObj = _db.Companies.FirstOrDefault(c => c.Id == id);
            if (companyObj == null) return NotFound();

            return View(companyObj);
        }

        // POST: Upsert (Create/Edit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company companyObj)
        {
            if (!ModelState.IsValid)
                return View(companyObj);

            if (companyObj.Id == 0)
            {
                // New company
                _db.Companies.Add(companyObj);
                TempData["success"] = "Company created successfully";
            }
            else
            {
                // Update existing company
                var existingCompany = _db.Companies.Find(companyObj.Id);
                if (existingCompany == null) return NotFound();

                existingCompany.Name = companyObj.Name;
                existingCompany.StreetAddress = companyObj.StreetAddress;
                existingCompany.City = companyObj.City;
                existingCompany.State = companyObj.State;
                existingCompany.PostalCode = companyObj.PostalCode;
                existingCompany.PhoneNumber = companyObj.PhoneNumber;
                existingCompany.IsActive = companyObj.IsActive;

                _db.Companies.Update(existingCompany);
                TempData["success"] = "Company updated successfully";
            }

            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Delete
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var companyObj = _db.Companies.FirstOrDefault(c => c.Id == id);
            if (companyObj == null) return NotFound();

            return View(companyObj);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            if (id == null) return NotFound();

            var company = _db.Companies.FirstOrDefault(c => c.Id == id);
            if (company == null) return NotFound();

            // Check if any user under this company has orders
            bool hasOrders = _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .Any(o => o.ApplicationUser.CompanyId == company.Id);

            // Check if any active user is assigned to this company
            bool hasActiveUsers = _db.Users
                .Any(u => u.CompanyId == company.Id && u.IsDeleted);

            if (hasOrders || hasActiveUsers)
            {
                // Cannot delete: mark inactive instead
                company.IsActive = false;
                _db.Companies.Update(company);
                _db.SaveChanges();

                TempData["error"] = "Company has order history or active users and cannot be deleted. It has been marked inactive instead.";
                return RedirectToAction("Delete", new { id = company.Id });
            }

            // Safe to delete
            _db.Companies.Remove(company);
            _db.SaveChanges();

            TempData["success"] = "Company deleted successfully.";
            return RedirectToAction("Index");
        }

        // Optional: Toggle Active/Inactive directly from Index
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var company = _db.Companies.Find(id);
            if (company == null) return NotFound();

            company.IsActive = !company.IsActive;
            _db.Companies.Update(company);
            _db.SaveChanges();

            TempData["success"] = $"Company status updated to {(company.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction("Index");
        }
    }
}