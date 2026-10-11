
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoidsSimulation.Web.Areas.Identity.Pages.Account;

public sealed class LogoutModel(
    SignInManager<IdentityUser> signInManager)
    : PageModel
{
    [BindProperty]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = GetSafeReturnUrl(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();

        return LocalRedirect(GetSafeReturnUrl(ReturnUrl));
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
            && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/simulation";
    }
}
