using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnboardPro.Data;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize(Roles = AppRoles.Hr)]
public sealed class EmployeeAccountsController(
    OnboardProDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<EmployeeAccountsController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await db.Employees.AsNoTracking().Include(employee => employee.Role)
            .OrderBy(employee => employee.LastName).ToListAsync(cancellationToken));

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await PopulateEmployeesAsync(new EmployeeAccountViewModel(), cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeAccountViewModel model, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Id == model.EmployeeId, cancellationToken);
        if (employee is null || employee.IdentityUserId is not null)
            ModelState.AddModelError(nameof(model.EmployeeId), "Select an employee without a linked account.");
        if (!ModelState.IsValid) return View(await PopulateEmployeesAsync(model, cancellationToken));

        var account = new ApplicationUser
        {
            UserName = employee!.Email,
            Email = employee.Email,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(account, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(await PopulateEmployeesAsync(model, cancellationToken));
        }

        var roleResult = await userManager.AddToRoleAsync(account, employee.Role.Name);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(account);
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(await PopulateEmployeesAsync(model, cancellationToken));
        }

        employee.IdentityUserId = account.Id;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Provisioned a {Role} account for employee {EmployeeId}.", employee.Role.Name, employee.Id);
        TempData["Success"] = "Account created. Share the temporary password with the employee using a secure channel.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.FindAsync([id], cancellationToken);
        if (employee is null) return NotFound();
        if (employee.IdentityUserId is null)
        {
            TempData["Error"] = "This employee has no linked account.";
            return RedirectToAction(nameof(Index));
        }

        var account = await userManager.FindByIdAsync(employee.IdentityUserId);
        if (account is not null)
        {
            if (account.Id == userManager.GetUserId(User))
            {
                TempData["Error"] = "You cannot remove your own account.";
                return RedirectToAction(nameof(Index));
            }
            if (await userManager.IsInRoleAsync(account, AppRoles.Hr) &&
                (await userManager.GetUsersInRoleAsync(AppRoles.Hr)).Count <= 1)
            {
                TempData["Error"] = "At least one HR account must remain active.";
                return RedirectToAction(nameof(Index));
            }
            var result = await userManager.DeleteAsync(account);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }
        }
        employee.IdentityUserId = null;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Employee account removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<EmployeeAccountViewModel> PopulateEmployeesAsync(EmployeeAccountViewModel model, CancellationToken cancellationToken)
    {
        model.Employees = await db.Employees.AsNoTracking()
            .Where(employee => employee.IdentityUserId == null)
            .OrderBy(employee => employee.LastName)
            .Select(employee => new SelectListItem(
                employee.FirstName + " " + employee.LastName + " (" + employee.Email + ", " + employee.Role.Name + ")",
                employee.Id.ToString(),
                employee.Id == model.EmployeeId))
            .ToListAsync(cancellationToken);
        return model;
    }
}
