using Microsoft.AspNetCore.Identity;

namespace OnboardPro.Models;

public sealed class ApplicationUser : IdentityUser
{
    public Employee? Employee { get; set; }
}
