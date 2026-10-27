using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using OnboardPro.Models;
using OnboardPro.Services;

namespace OnboardPro.Controllers;

public sealed class HomeController(
    ILogger<HomeController> logger,
    IDashboardService dashboardService) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var isHrOrIt = User.IsInRole(AppRoles.Hr) || User.IsInRole(AppRoles.ItAdmin);
        var model = await dashboardService.GetAsync(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            !isHrOrIt && User.IsInRole(AppRoles.Buddy),
            !isHrOrIt && User.IsInRole(AppRoles.NewJoiner),
            cancellationToken);
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        logger.LogError(exception, "An unhandled exception reached the error endpoint. Request {RequestId}", HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
