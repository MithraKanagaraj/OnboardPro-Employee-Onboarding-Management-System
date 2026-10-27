using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize]
public sealed class OnboardingTasksController(
    IOnboardingTaskService tasks,
    IEmployeeService employees,
    IBuddyService buddies) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.Hr))
            return View(await tasks.ListAsync(cancellationToken));

        var employee = await CurrentEmployeeAsync(cancellationToken);
        if (employee is null) return Forbid();
        if (User.IsInRole(AppRoles.NewJoiner))
            return View(await tasks.ListForEmployeeAsync(employee.Id, cancellationToken));

        if (User.IsInRole(AppRoles.Buddy))
        {
            var assignments = await buddies.ListForBuddyAsync(employee.Id, cancellationToken);
            return View(assignments.SelectMany(assignment => assignment.Employee.OnboardingTasks)
                .OrderBy(task => task.DueDate).ToList());
        }
        return Forbid();
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        if (task is null) return NotFound();
        if (!await CanViewEmployeeAsync(task.EmployeeId, cancellationToken)) return Forbid();
        return View(task);
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await PopulateEmployeesAsync(new OnboardingTaskFormViewModel(), cancellationToken));

    [HttpPost, Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OnboardingTaskFormViewModel model, CancellationToken cancellationToken)
    {
        if (await employees.GetAsync(model.EmployeeId, cancellationToken) is null)
            ModelState.AddModelError(nameof(model.EmployeeId), "Select an existing employee.");
        if (!Enum.IsDefined(model.Status))
            ModelState.AddModelError(nameof(model.Status), "Select a valid task status.");
        if (!ModelState.IsValid) return View(await PopulateEmployeesAsync(model, cancellationToken));
        try
        {
            await tasks.SaveAsync(Map(model), cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await PopulateEmployeesAsync(model, cancellationToken));
        }
        TempData["Success"] = "Onboarding task created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        return task is null ? NotFound() : View(await PopulateEmployeesAsync(Map(task), cancellationToken));
    }

    [HttpPost, Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OnboardingTaskFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (await employees.GetAsync(model.EmployeeId, cancellationToken) is null)
            ModelState.AddModelError(nameof(model.EmployeeId), "Select an existing employee.");
        if (!Enum.IsDefined(model.Status))
            ModelState.AddModelError(nameof(model.Status), "Select a valid task status.");
        if (!ModelState.IsValid) return View(await PopulateEmployeesAsync(model, cancellationToken));
        var task = await tasks.GetAsync(id, cancellationToken);
        if (task is null) return NotFound();
        task.EmployeeId = model.EmployeeId;
        task.TitleSnapshot = model.TitleSnapshot.Trim();
        task.DescriptionSnapshot = model.DescriptionSnapshot?.Trim();
        task.CategorySnapshot = model.CategorySnapshot.Trim();
        task.IsRequiredSnapshot = model.IsRequiredSnapshot;
        task.Status = model.Status;
        task.DueDate = model.DueDate;
        try
        {
            await tasks.SaveAsync(task, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await PopulateEmployeesAsync(model, cancellationToken));
        }
        TempData["Success"] = "Onboarding task updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        return task is null ? NotFound() : View(task);
    }

    [HttpPost, ActionName("Delete"), Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await tasks.DeleteAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "Onboarding task deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = AppRoles.NewJoiner), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OnboardingTaskStatus status, CancellationToken cancellationToken)
    {
        var employee = await CurrentEmployeeAsync(cancellationToken);
        if (employee is null) return Forbid();
        if (!Enum.IsDefined(status) || !await tasks.SetStatusAsync(id, employee.Id, status, cancellationToken))
            return NotFound();
        TempData["Success"] = "Task status updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanViewEmployeeAsync(int employeeId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.Hr)) return true;
        var current = await CurrentEmployeeAsync(cancellationToken);
        if (current is null) return false;
        if (User.IsInRole(AppRoles.NewJoiner)) return current.Id == employeeId;
        if (User.IsInRole(AppRoles.Buddy))
            return (await buddies.ListForBuddyAsync(current.Id, cancellationToken))
                .Any(assignment => assignment.EmployeeId == employeeId);
        return false;
    }

    private Task<Employee?> CurrentEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? Task.FromResult<Employee?>(null) : employees.GetForUserAsync(userId, cancellationToken);
    }

    private async Task<OnboardingTaskFormViewModel> PopulateEmployeesAsync(OnboardingTaskFormViewModel model, CancellationToken cancellationToken)
    {
        model.Employees = (await employees.ListAsync(cancellationToken))
            .Select(employee => new SelectListItem(employee.FullName, employee.Id.ToString(), employee.Id == model.EmployeeId));
        return model;
    }

    private static OnboardingTaskFormViewModel Map(OnboardingTask task) => new()
    {
        Id = task.Id,
        EmployeeId = task.EmployeeId,
        TitleSnapshot = task.TitleSnapshot,
        DescriptionSnapshot = task.DescriptionSnapshot,
        CategorySnapshot = task.CategorySnapshot,
        IsRequiredSnapshot = task.IsRequiredSnapshot,
        Status = task.Status,
        DueDate = task.DueDate
    };

    private static OnboardingTask Map(OnboardingTaskFormViewModel model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        TitleSnapshot = model.TitleSnapshot.Trim(),
        DescriptionSnapshot = model.DescriptionSnapshot?.Trim(),
        CategorySnapshot = model.CategorySnapshot.Trim(),
        IsRequiredSnapshot = model.IsRequiredSnapshot,
        Status = model.Status,
        DueDate = model.DueDate
    };
}
