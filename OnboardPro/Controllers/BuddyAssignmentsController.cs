using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize(Roles = AppRoles.Hr + "," + AppRoles.Buddy)]
public sealed class BuddyAssignmentsController(
    IBuddyService buddies,
    IEmployeeService employees) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.Hr))
            return View(await buddies.ListAsync(cancellationToken));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var employee = userId is null ? null : await employees.GetForUserAsync(userId, cancellationToken);
        if (employee is null) return Forbid();
        return View(await buddies.ListForBuddyAsync(employee.Id, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var assignment = await buddies.GetAsync(id, cancellationToken);
        if (assignment is null) return NotFound();
        if (!User.IsInRole(AppRoles.Hr))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = userId is null ? null : await employees.GetForUserAsync(userId, cancellationToken);
            if (employee?.Id != assignment.BuddyEmployeeId) return Forbid();
        }
        return View(assignment);
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await PopulateEmployeesAsync(new BuddyAssignmentFormViewModel(), cancellationToken));

    [HttpPost, Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BuddyAssignmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(await PopulateEmployeesAsync(model, cancellationToken));
        try
        {
            await buddies.SaveAsync(Map(model), cancellationToken);
            TempData["Success"] = "Buddy assigned.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await PopulateEmployeesAsync(model, cancellationToken));
        }
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var assignment = await buddies.GetAsync(id, cancellationToken);
        return assignment is null ? NotFound() : View(await PopulateEmployeesAsync(Map(assignment), cancellationToken));
    }

    [HttpPost, Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BuddyAssignmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(await PopulateEmployeesAsync(model, cancellationToken));
        try
        {
            var assignment = await buddies.GetAsync(id, cancellationToken);
            if (assignment is null) return NotFound();
            assignment.EmployeeId = model.EmployeeId;
            assignment.BuddyEmployeeId = model.BuddyEmployeeId;
            assignment.AssignedOn = model.AssignedOn;
            assignment.EndedOn = model.EndedOn;
            await buddies.SaveAsync(assignment, cancellationToken);
            TempData["Success"] = "Buddy assignment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await PopulateEmployeesAsync(model, cancellationToken));
        }
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var assignment = await buddies.GetAsync(id, cancellationToken);
        return assignment is null ? NotFound() : View(assignment);
    }

    [HttpPost, ActionName("Delete"), Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await buddies.DeleteAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "Buddy assignment deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<BuddyAssignmentFormViewModel> PopulateEmployeesAsync(BuddyAssignmentFormViewModel model, CancellationToken cancellationToken)
    {
        var allEmployees = await employees.ListAsync(cancellationToken);
        model.Joiners = allEmployees.Where(employee => employee.Role.Name == AppRoles.NewJoiner)
            .Select(employee => new SelectListItem(employee.FullName, employee.Id.ToString(), employee.Id == model.EmployeeId));
        model.BuddyEmployees = allEmployees.Where(employee => employee.Role.Name == AppRoles.Buddy)
            .Select(employee => new SelectListItem(employee.FullName, employee.Id.ToString(), employee.Id == model.BuddyEmployeeId));
        return model;
    }

    private static BuddyAssignmentFormViewModel Map(BuddyAssignment assignment) => new()
    {
        Id = assignment.Id,
        EmployeeId = assignment.EmployeeId,
        BuddyEmployeeId = assignment.BuddyEmployeeId,
        AssignedOn = assignment.AssignedOn,
        EndedOn = assignment.EndedOn
    };

    private static BuddyAssignment Map(BuddyAssignmentFormViewModel model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        BuddyEmployeeId = model.BuddyEmployeeId,
        AssignedOn = model.AssignedOn,
        EndedOn = model.EndedOn
    };
}
