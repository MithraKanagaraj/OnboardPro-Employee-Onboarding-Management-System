using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize(Roles = AppRoles.Hr)]
public sealed class ChecklistTemplatesController(
    IChecklistService checklists,
    IEmployeeService employees,
    ITaskDefinitionService taskDefinitions) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await checklists.ListAsync(cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var template = await checklists.GetAsync(id, cancellationToken);
        return template is null ? NotFound() : View(template);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await PopulateAsync(new ChecklistTemplateFormViewModel(), cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChecklistTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!(await employees.ListRolesAsync(cancellationToken)).Any(role => role.Id == model.RoleId))
            ModelState.AddModelError(nameof(model.RoleId), "Select a valid role.");
        if (!ModelState.IsValid) return View(await PopulateAsync(model, cancellationToken));
        try
        {
            await checklists.SaveAsync(Map(model), model.SelectedTaskDefinitionIds, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.SelectedTaskDefinitionIds), exception.Message);
            return View(await PopulateAsync(model, cancellationToken));
        }
        TempData["Success"] = "Checklist template created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var template = await checklists.GetAsync(id, cancellationToken);
        if (template is null) return NotFound();
        return View(await PopulateAsync(Map(template), cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ChecklistTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!(await employees.ListRolesAsync(cancellationToken)).Any(role => role.Id == model.RoleId))
            ModelState.AddModelError(nameof(model.RoleId), "Select a valid role.");
        if (!ModelState.IsValid) return View(await PopulateAsync(model, cancellationToken));
        if (await checklists.GetAsync(id, cancellationToken) is null) return NotFound();
        try
        {
            await checklists.SaveAsync(Map(model), model.SelectedTaskDefinitionIds, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.SelectedTaskDefinitionIds), exception.Message);
            return View(await PopulateAsync(model, cancellationToken));
        }
        TempData["Success"] = "Template updated. Previously assigned onboarding tasks retain their snapshots.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Assign(CancellationToken cancellationToken)
    {
        ViewBag.Employees = new SelectList(await employees.ListAsync(cancellationToken), "Id", "FullName");
        ViewBag.Templates = new SelectList((await checklists.ListAsync(cancellationToken)).Where(template => template.IsActive), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int employeeId, int templateId, CancellationToken cancellationToken)
    {
        if (!await checklists.AssignAsync(employeeId, templateId, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "The employee or active template was not found, or this template is already assigned.");
            ViewBag.Employees = new SelectList(await employees.ListAsync(cancellationToken), "Id", "FullName", employeeId);
            ViewBag.Templates = new SelectList((await checklists.ListAsync(cancellationToken)).Where(template => template.IsActive), "Id", "Name", templateId);
            return View();
        }
        TempData["Success"] = "Checklist assigned and employee task instances generated.";
        return RedirectToAction("Index", "OnboardingTasks");
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var template = await checklists.GetAsync(id, cancellationToken);
        return template is null ? NotFound() : View(template);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await checklists.DeleteAsync(id, cancellationToken))
            TempData["Error"] = "Template could not be deleted because it is missing or has already been assigned.";
        else TempData["Success"] = "Checklist template deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ChecklistTemplateFormViewModel> PopulateAsync(ChecklistTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        model.Roles = (await employees.ListRolesAsync(cancellationToken))
            .Select(role => new SelectListItem(role.Name, role.Id.ToString(), role.Id == model.RoleId));
        model.TaskDefinitions = (await taskDefinitions.ListAsync(cancellationToken))
            .Select(task => new SelectListItem($"{task.Category}: {task.Title}", task.Id.ToString(),
                model.SelectedTaskDefinitionIds.Contains(task.Id)));
        return model;
    }

    private static ChecklistTemplateFormViewModel Map(ChecklistTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Description = template.Description,
        RoleId = template.RoleId,
        IsActive = template.IsActive,
        SelectedTaskDefinitionIds = template.TemplateTasks.Select(link => link.TaskDefinitionId).ToList()
    };

    private static ChecklistTemplate Map(ChecklistTemplateFormViewModel model) => new()
    {
        Id = model.Id,
        Name = model.Name.Trim(),
        Description = model.Description?.Trim(),
        RoleId = model.RoleId,
        IsActive = model.IsActive
    };
}
