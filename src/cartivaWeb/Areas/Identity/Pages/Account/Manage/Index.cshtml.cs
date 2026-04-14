// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Cartiva.Domain; // adjust namespace
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace CartivaWeb.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }
        public string Email { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Name is required.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
            [RegularExpression(@"^[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens and apostrophes.")]
            [Display(Name = "Full Name")]
            public string Name { get; set; }

            [RegularExpression(@"^\+?\d[\d\s\-]{6,18}\d$", ErrorMessage = "Please enter a valid phone number (e.g. +47 12345678).")]
            [StringLength(20)]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [StringLength(100)]
            [Display(Name = "Street Address")]
            public string StreetAddress { get; set; }

            [StringLength(50)]
            [Display(Name = "City")]
            public string City { get; set; }

            [StringLength(50)]
            [Display(Name = "State / Region")]
            public string? State { get; set; }

            [StringLength(10)]
            [RegularExpression(@"^\d{4,10}$", ErrorMessage = "Postal code must be 4-10 digits.")]
            [Display(Name = "Postal Code")]
            public string PostalCode { get; set; }

            [StringLength(50)]
            [Display(Name = "Country")]
            public string Country { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user);
            Email = await _userManager.GetEmailAsync(user);

            Input = new InputModel
            {
                Name = user.Name,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                StreetAddress = user.StreetAddress,
                City = user.City,
                State = user.State,
                PostalCode = user.PostalCode,
                Country = user.Country
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update custom fields
            user.Name = Input.Name;
            user.StreetAddress = Input.StreetAddress;
            user.City = Input.City;
            user.State = Input.State;
            user.PostalCode = Input.PostalCode;
            user.Country = Input.Country;

            // Update phone number if changed
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Unexpected error when updating profile.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}