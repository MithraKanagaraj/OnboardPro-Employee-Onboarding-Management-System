using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize(Roles = AppRoles.ItAdmin)]
public sealed class AssetsController(IAssetService assets, IEmployeeService employees) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await assets.ListAsync(cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var asset = await assets.GetAsync(id, cancellationToken);
        return asset is null ? NotFound() : View(asset);
    }

    public IActionResult Create() => View(new AssetFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        await assets.SaveAsync(Map(model), cancellationToken);
        TempData["Success"] = "Asset added to inventory.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var asset = await assets.GetAsync(id, cancellationToken);
        return asset is null ? NotFound() : View(Map(asset));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var existing = await assets.GetAsync(id, cancellationToken);
        if (existing is null) return NotFound();
        existing.Name = model.Name.Trim();
        existing.Category = model.Category.Trim();
        existing.SerialNumber = model.SerialNumber.Trim();
        existing.Notes = model.Notes?.Trim();
        existing.IsRetired = model.IsRetired;
        await assets.SaveAsync(existing, cancellationToken);
        TempData["Success"] = "Asset updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var asset = await assets.GetAsync(id, cancellationToken);
        return asset is null ? NotFound() : View(asset);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await assets.DeleteAsync(id, cancellationToken))
            TempData["Error"] = "Asset could not be deleted because it has an allocation history.";
        else TempData["Success"] = "Asset deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Allocations(CancellationToken cancellationToken) =>
        View(await assets.ListAllocationsAsync(cancellationToken));

    public async Task<IActionResult> Allocate(CancellationToken cancellationToken) =>
        View(await PopulateAllocationAsync(new AssetAllocationFormViewModel(), cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Allocate(AssetAllocationFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(await PopulateAllocationAsync(model, cancellationToken));
        try
        {
            await assets.SaveAllocationAsync(Map(model), cancellationToken);
            TempData["Success"] = "Asset allocation saved.";
            return RedirectToAction(nameof(Allocations));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await PopulateAllocationAsync(model, cancellationToken));
        }
    }

    public async Task<IActionResult> EditAllocation(int id, CancellationToken cancellationToken)
    {
        var allocation = await assets.GetAllocationAsync(id, cancellationToken);
        if (allocation is null) return NotFound();
        return View(await PopulateAllocationAsync(Map(allocation), cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAllocation(int id, AssetAllocationFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(await PopulateAllocationAsync(model, cancellationToken));
        try
        {
            var existing = await assets.GetAllocationAsync(id, cancellationToken);
            if (existing is null) return NotFound();
            existing.EmployeeId = model.EmployeeId;
            existing.AssetId = model.AssetId;
            existing.AllocatedOn = model.AllocatedOn;
            existing.ReturnedOn = model.ReturnedOn;
            existing.Notes = model.Notes?.Trim();
            await assets.SaveAllocationAsync(existing, cancellationToken);
            TempData["Success"] = "Asset allocation updated.";
            return RedirectToAction(nameof(Allocations));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await PopulateAllocationAsync(model, cancellationToken));
        }
    }

    public async Task<IActionResult> DeleteAllocation(int id, CancellationToken cancellationToken)
    {
        var allocation = await assets.GetAllocationAsync(id, cancellationToken);
        return allocation is null ? NotFound() : View(allocation);
    }

    [HttpPost, ActionName("DeleteAllocation"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllocationConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await assets.DeleteAllocationAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "Allocation record deleted.";
        return RedirectToAction(nameof(Allocations));
    }

    private async Task<AssetAllocationFormViewModel> PopulateAllocationAsync(AssetAllocationFormViewModel model, CancellationToken cancellationToken)
    {
        model.Employees = (await employees.ListAsync(cancellationToken))
            .Select(employee => new SelectListItem(employee.FullName, employee.Id.ToString(), employee.Id == model.EmployeeId));
        model.Assets = (await assets.ListAsync(cancellationToken))
            .Where(asset => !asset.IsRetired &&
                (asset.Allocations.All(allocation => allocation.ReturnedOn is not null) || asset.Id == model.AssetId))
            .Select(asset => new SelectListItem($"{asset.Name} ({asset.SerialNumber})", asset.Id.ToString(), asset.Id == model.AssetId));
        return model;
    }

    private static AssetFormViewModel Map(Asset asset) => new()
    {
        Id = asset.Id,
        Name = asset.Name,
        Category = asset.Category,
        SerialNumber = asset.SerialNumber,
        Notes = asset.Notes,
        IsRetired = asset.IsRetired
    };

    private static Asset Map(AssetFormViewModel model) => new()
    {
        Id = model.Id,
        Name = model.Name.Trim(),
        Category = model.Category.Trim(),
        SerialNumber = model.SerialNumber.Trim(),
        Notes = model.Notes?.Trim(),
        IsRetired = model.IsRetired
    };

    private static AssetAllocationFormViewModel Map(AssetAllocation allocation) => new()
    {
        Id = allocation.Id,
        EmployeeId = allocation.EmployeeId,
        AssetId = allocation.AssetId,
        AllocatedOn = allocation.AllocatedOn,
        ReturnedOn = allocation.ReturnedOn,
        Notes = allocation.Notes
    };

    private static AssetAllocation Map(AssetAllocationFormViewModel model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        AssetId = model.AssetId,
        AllocatedOn = model.AllocatedOn,
        ReturnedOn = model.ReturnedOn,
        Notes = model.Notes?.Trim()
    };
}
