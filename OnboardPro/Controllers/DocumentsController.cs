using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnboardPro.Models;
using OnboardPro.Services;
using OnboardPro.ViewModels;

namespace OnboardPro.Controllers;

[Authorize]
public sealed class DocumentsController(
    IDocumentService documents,
    IEmployeeService employees,
    ILogger<DocumentsController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.Hr))
            return View(await documents.ListAsync(cancellationToken));
        if (!User.IsInRole(AppRoles.NewJoiner)) return Forbid();
        var employee = await CurrentEmployeeAsync(cancellationToken);
        return employee is null ? Forbid() : View(await documents.ListForEmployeeAsync(employee.Id, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AppRoles.Hr) && !User.IsInRole(AppRoles.NewJoiner)) return Forbid();
        var document = await documents.GetAsync(id, cancellationToken);
        if (document is null) return NotFound();
        if (!User.IsInRole(AppRoles.Hr))
        {
            var employee = await CurrentEmployeeAsync(cancellationToken);
            if (employee is null || employee.Id != document.EmployeeId) return Forbid();
        }
        return View(document);
    }

    [Authorize(Roles = AppRoles.NewJoiner)]
    public IActionResult Upload() => View(new DocumentUploadViewModel());

    [HttpPost, Authorize(Roles = AppRoles.NewJoiner), ValidateAntiForgeryToken, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var employee = await CurrentEmployeeAsync(cancellationToken);
        if (employee is null) return Forbid();
        try
        {
            await documents.UploadAsync(employee.Id, model.DocumentType, model.File!, cancellationToken);
            TempData["Success"] = "Document uploaded for HR review.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(nameof(model.File), exception.Message);
            logger.LogWarning("Document upload validation failed for employee {EmployeeId}: {Reason}", employee.Id, exception.Message);
            return View(model);
        }
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(id, cancellationToken);
        if (document is null) return NotFound();
        ViewBag.Document = document;
        return View(new DocumentReviewViewModel { Id = id, Status = document.Status, ReviewNote = document.ReviewNote });
    }

    [HttpPost, Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(DocumentReviewViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Document = await documents.GetAsync(model.Id, cancellationToken);
            return View(model);
        }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || !await documents.ReviewAsync(model.Id, model.Status, model.ReviewNote, userId, cancellationToken))
            return NotFound();
        TempData["Success"] = "Document review saved.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AppRoles.Hr) && !User.IsInRole(AppRoles.NewJoiner)) return Forbid();
        var document = await documents.GetAsync(id, cancellationToken);
        if (document is null) return NotFound();
        if (!User.IsInRole(AppRoles.Hr))
        {
            var employee = await CurrentEmployeeAsync(cancellationToken);
            if (employee is null || employee.Id != document.EmployeeId) return Forbid();
        }

        var path = documents.GetStoredFilePath(document);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, document.ContentType, document.OriginalFileName);
    }

    [Authorize(Roles = AppRoles.Hr)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(id, cancellationToken);
        return document is null ? NotFound() : View(document);
    }

    [HttpPost, ActionName("Delete"), Authorize(Roles = AppRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (!await documents.DeleteAsync(id, cancellationToken)) return NotFound();
        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Employee?> CurrentEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? Task.FromResult<Employee?>(null) : employees.GetForUserAsync(userId, cancellationToken);
    }
}
