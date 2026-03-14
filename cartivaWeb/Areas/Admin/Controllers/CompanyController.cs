using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Linq;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CompanyController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Company List
        public IActionResult Index()
        {
            var objCompanyList = _db.Companies.ToList();
            
            return View(objCompanyList);
        }

        // GET: Upsert (Create/Edit)
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                // Create new company
                return View(new Company());
            }
            else
            {
                // Edit existing company
                Company companyObj = _db.Companies.FirstOrDefault(u => u.Id == id);
                if (companyObj == null)
                    return NotFound();
                return View(companyObj);
            }
        }

        // POST: Upsert (Create/Edit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company companyObj)
        {
            if (ModelState.IsValid)
            {
                if (companyObj.Id == 0)
                {
                    // Create new company
                    _db.Companies.Add(companyObj);
                    TempData["success"] = "Company created successfully";
                }
                else
                {
                    // Update existing company
                    _db.Companies.Update(companyObj);
                    TempData["success"] = "Company updated successfully";
                }

                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            // If ModelState is invalid, return the same view with validation messages
            return View(companyObj);
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
            var obj = _db.Companies.Find(id);
            if (obj == null) return NotFound();

            _db.Companies.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Company deleted successfully";

            return RedirectToAction("Index");
        }
    }
}