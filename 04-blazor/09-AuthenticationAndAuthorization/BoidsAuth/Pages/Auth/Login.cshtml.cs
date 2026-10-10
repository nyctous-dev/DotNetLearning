using System.Security.Claims;
using BoidsAuth.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoidsAuth.Pages.Auth;

public sealed class LoginModel : PageModel
{
    private readonly LocalUserStore _users;

    public LoginModel(LocalUserStore users)
    {
        _users = users;
    }

    [BindProperty]
    public string Username { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrEmpty(Password))
        {
            ModelState.AddModelError(
                string.Empty,
                "Enter a username and password.");

            return Page();
        }

        LocalUser? user = _users.ValidateCredentials(
            Username.Trim(),
            Password);

        if (user is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid username or password.");

            return Page();
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        ];

        ClaimsIdentity identity = new(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true
            });

        return LocalRedirect("/");
    }
}