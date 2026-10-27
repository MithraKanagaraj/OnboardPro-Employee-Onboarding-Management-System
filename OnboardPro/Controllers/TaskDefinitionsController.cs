using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize(Roles = AppRoles.Hr)]
public sealed class TaskDefinitionsController(ITaskDefinitionService tasks) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await tasks.ListAsync(cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        return task is null ? NotFound() : View(task);
    }

    public IActionResult Create() => View(new TaskDefinitionFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskDefinitionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        await tasks.SaveAsync(Map(model), cancellationToken);
        TempData["Success"] = "Task definition created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        return task is null ? NotFound() : View(Map(task));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskDefinitionFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var existing = await tasks.GetAsync(id, cancellationToken);
        if (existing is null) return NotFound();
        existing.Title = model.Title.Trim();
        existing.Description = model.Description?.Trim();
        existing.DueOffsetDays = model.DueOffsetDays;
        existing.Category = model.Category.Trim();
        existing.IsRequired = model.IsRequired;
        await tasks.SaveAsync(existing, cancellationToken);
        TempData["Success"] = "Task definition updated. Assigned task instances are unchanged.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetAsync(id, cancellationToken);
        return task is null ? NotFound() : View(task);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await tasks.DeleteAsync(id, cancellationToken))
            TempData["Error"] = "Task definition could not be deleted because it is missing or is used by a checklist template.";
        else TempData["Success"] = "Task definition deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static TaskDefinition Map(TaskDefinitionFormViewModel model) => new()
    {
        Id = model.Id,
        Title = model.Title.Trim(),
        Description = model.Description?.Trim(),
        DueOffsetDays = model.DueOffsetDays,
        Category = model.Category.Trim(),
        IsRequired = model.IsRequired
    };

    private static TaskDefinitionFormViewModel Map(TaskDefinition task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        DueOffsetDays = task.DueOffsetDays,
        Category = task.Category,
        IsRequired = task.IsRequired
    };
}
