using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Security;

namespace DigitalSignage.CMS.Pages.Account;

public class ChangePasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public ChangePasswordModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool Success { get; set; }

    public bool IsRequired { get; set; }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        IsRequired = User.HasClaim(c => c.Type == SentinelClaims.MustChangePassword);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsRequired = User.HasClaim(c => c.Type == SentinelClaims.MustChangePassword);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        var mustChangeClaim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == SentinelClaims.MustChangePassword);
        if (mustChangeClaim is not null)
        {
            await _userManager.RemoveClaimAsync(user, mustChangeClaim);
        }

        await _signInManager.RefreshSignInAsync(user);
        Success = true;
        IsRequired = false;
        Input = new InputModel();
        return Page();
    }
}
