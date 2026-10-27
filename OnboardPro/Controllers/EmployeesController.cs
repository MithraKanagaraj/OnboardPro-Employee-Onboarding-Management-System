using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize(Roles = AppRoles.Hr)]
public sealed class EmployeesController(IEmployeeService employees) : Controller
{
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var employeesList = await employees.ListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            employeesList = employeesList.Where(employee =>
                employee.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                employee.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                employee.Department.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewData["Search"] = search;
        return View(employeesList);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(id, cancellationToken);
        return employee is null ? NotFound() : View(employee);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await PopulateRolesAsync(new EmployeeFormViewModel(), cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model, CancellationToken cancellationToken)
    {
        if (await employees.EmailExistsAsync(model.Email, cancellationToken: cancellationToken))
            ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
        if (!(await employees.ListRolesAsync(cancellationToken)).Any(role => role.Id == model.RoleId))
            ModelState.AddModelError(nameof(model.RoleId), "Select a valid access role.");
        if (!ModelState.IsValid) return View(await PopulateRolesAsync(model, cancellationToken));

        await employees.SaveAsync(Map(model), cancellationToken);
        TempData["Success"] = "Employee created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(id, cancellationToken);
        if (employee is null) return NotFound();
        return View(await PopulateRolesAsync(Map(employee), cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var existing = await employees.GetAsync(id, cancellationToken);
        if (existing is null) return NotFound();
        if (existing.IdentityUserId is not null && existing.RoleId != model.RoleId)
            ModelState.AddModelError(nameof(model.RoleId), "An employee's access role cannot be changed after an account is linked.");
        if (existing.IdentityUserId is not null &&
            !string.Equals(existing.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.Email), "Remove the linked account before changing the employee email.");
        if (await employees.EmailExistsAsync(model.Email, id, cancellationToken))
            ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
        if (!(await employees.ListRolesAsync(cancellationToken)).Any(role => role.Id == model.RoleId))
            ModelState.AddModelError(nameof(model.RoleId), "Select a valid access role.");
        if (!ModelState.IsValid) return View(await PopulateRolesAsync(model, cancellationToken));

        existing.FirstName = model.FirstName.Trim();
        existing.LastName = model.LastName.Trim();
        existing.Email = model.Email;
        existing.JobTitle = model.JobTitle.Trim();
        existing.Department = model.Department.Trim();
        existing.StartDate = model.StartDate;
        existing.RoleId = model.RoleId;
        await employees.SaveAsync(existing, cancellationToken);
        TempData["Success"] = "Employee updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(id, cancellationToken);
        return employee is null ? NotFound() : View(employee);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await employees.DeleteAsync(id, cancellationToken))
            TempData["Error"] = "Employee could not be deleted. Remove linked accounts, documents, asset history, or buddy assignments first.";
        else TempData["Success"] = "Employee and their onboarding records were removed.";
        return RedirectToAction(nameof(Index));
    }

    [AcceptVerbs("Get", "Post")]
    public async Task<IActionResult> VerifyEmail(string email, int id, CancellationToken cancellationToken) =>
        Json(!await employees.EmailExistsAsync(email, id, cancellationToken));

    private async Task<EmployeeFormViewModel> PopulateRolesAsync(EmployeeFormViewModel model, CancellationToken cancellationToken)
    {
        model.Roles = (await employees.ListRolesAsync(cancellationToken))
            .Select(role => new SelectListItem(role.Name, role.Id.ToString(), role.Id == model.RoleId));
        return model;
    }

    private static EmployeeFormViewModel Map(Employee employee) => new()
    {
        Id = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Email = employee.Email,
        JobTitle = employee.JobTitle,
        Department = employee.Department,
        StartDate = employee.StartDate,
        RoleId = employee.RoleId
    };

    private static Employee Map(EmployeeFormViewModel model) => new()
    {
        Id = model.Id,
        FirstName = model.FirstName.Trim(),
        LastName = model.LastName.Trim(),
        Email = model.Email,
        JobTitle = model.JobTitle.Trim(),
        Department = model.Department.Trim(),
        StartDate = model.StartDate,
        RoleId = model.RoleId
    };
}
