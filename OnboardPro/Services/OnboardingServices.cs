using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnboardPro.Data;
using OnboardPro.Models;
using OnboardPro.ViewModels;

namespace OnboardPro.Services;

public interface IEmployeeService
{
    Task<List<Employee>> ListAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, int exceptId = 0, CancellationToken cancellationToken = default);
    Task SaveAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
}

public interface ITaskDefinitionService
{
    Task<List<TaskDefinition>> ListAsync(CancellationToken cancellationToken = default);
    Task<TaskDefinition?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(TaskDefinition task, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IChecklistService
{
    Task<List<ChecklistTemplate>> ListAsync(CancellationToken cancellationToken = default);
    Task<ChecklistTemplate?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(ChecklistTemplate template, IReadOnlyCollection<int> taskDefinitionIds, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AssignAsync(int employeeId, int templateId, CancellationToken cancellationToken = default);
    Task<List<EmployeeChecklist>> ListAssignmentsAsync(CancellationToken cancellationToken = default);
}

public interface IOnboardingTaskService
{
    Task<List<OnboardingTask>> ListAsync(CancellationToken cancellationToken = default);
    Task<List<OnboardingTask>> ListForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<OnboardingTask?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SetStatusAsync(int taskId, int employeeId, OnboardingTaskStatus status, CancellationToken cancellationToken = default);
    Task SaveAsync(OnboardingTask task, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IDocumentService
{
    Task<List<Document>> ListAsync(CancellationToken cancellationToken = default);
    Task<List<Document>> ListForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<Document?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Document> UploadAsync(int employeeId, string documentType, IFormFile file, CancellationToken cancellationToken = default);
    Task<bool> ReviewAsync(int id, DocumentStatus status, string? note, string reviewerUserId, CancellationToken cancellationToken = default);
    string GetStoredFilePath(Document document);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAssetService
{
    Task<List<Asset>> ListAsync(CancellationToken cancellationToken = default);
    Task<Asset?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<AssetAllocation>> ListAllocationsAsync(CancellationToken cancellationToken = default);
    Task<AssetAllocation?> GetAllocationAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAllocationAsync(AssetAllocation allocation, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllocationAsync(int id, CancellationToken cancellationToken = default);
}

public interface IBuddyService
{
    Task<List<BuddyAssignment>> ListAsync(CancellationToken cancellationToken = default);
    Task<List<BuddyAssignment>> ListForBuddyAsync(int buddyEmployeeId, CancellationToken cancellationToken = default);
    Task<BuddyAssignment?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(BuddyAssignment assignment, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardViewModel> GetAsync(string? userId, bool isBuddy, bool isJoiner, CancellationToken cancellationToken = default);
}

public sealed class EmployeeService(OnboardProDbContext db) : IEmployeeService
{
    public Task<List<Employee>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Employees.AsNoTracking().Include(employee => employee.Role).OrderBy(employee => employee.LastName).ToListAsync(cancellationToken);

    public Task<Employee?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Employees.Include(employee => employee.Role).SingleOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, int exceptId = 0, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return db.Employees.AnyAsync(employee => employee.Email == normalizedEmail && employee.Id != exceptId, cancellationToken);
    }

    public async Task SaveAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        employee.Email = employee.Email.Trim().ToLowerInvariant();
        if (employee.Id == 0)
        {
            db.Employees.Add(employee);
        }
        else
        {
            db.Entry(employee).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees.FindAsync([id], cancellationToken);
        if (employee is null) return false;
        if (employee.IdentityUserId is not null ||
            await db.Documents.AnyAsync(document => document.EmployeeId == id, cancellationToken) ||
            await db.AssetAllocations.AnyAsync(allocation => allocation.EmployeeId == id, cancellationToken) ||
            await db.BuddyAssignments.AnyAsync(assignment =>
                assignment.EmployeeId == id || assignment.BuddyEmployeeId == id, cancellationToken))
            return false;
        db.Employees.Remove(employee);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        db.OnboardingRoles.AsNoTracking().OrderBy(role => role.Name).ToListAsync(cancellationToken);

    public Task<Employee?> GetForUserAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Employees.AsNoTracking().SingleOrDefaultAsync(employee => employee.IdentityUserId == userId, cancellationToken);
}

public sealed class TaskDefinitionService(
    OnboardProDbContext db,
    OnboardPro.Repositories.IGenericRepository<TaskDefinition> repository) : ITaskDefinitionService
{
    public async Task<List<TaskDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListAsync(cancellationToken)).OrderBy(task => task.Category).ThenBy(task => task.Title).ToList();

    public Task<TaskDefinition?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task SaveAsync(TaskDefinition task, CancellationToken cancellationToken = default)
    {
        if (task.Id == 0) await repository.AddAsync(task, cancellationToken);
        else repository.Update(task);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetByIdAsync(id, cancellationToken);
        if (task is null) return false;
        if (await db.ChecklistTemplateTasks.AnyAsync(link => link.TaskDefinitionId == id, cancellationToken)) return false;
        repository.Remove(task);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ChecklistService(OnboardProDbContext db) : IChecklistService
{
    public Task<List<ChecklistTemplate>> ListAsync(CancellationToken cancellationToken = default) =>
        db.ChecklistTemplates.AsNoTracking().Include(template => template.Role)
            .Include(template => template.TemplateTasks).ThenInclude(link => link.TaskDefinition)
            .OrderBy(template => template.Name).ToListAsync(cancellationToken);

    public Task<ChecklistTemplate?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.ChecklistTemplates.Include(template => template.Role)
            .Include(template => template.TemplateTasks).ThenInclude(link => link.TaskDefinition)
            .SingleOrDefaultAsync(template => template.Id == id, cancellationToken);

    public async Task SaveAsync(ChecklistTemplate template, IReadOnlyCollection<int> taskDefinitionIds, CancellationToken cancellationToken = default)
    {
        if (!await db.OnboardingRoles.AnyAsync(role => role.Id == template.RoleId, cancellationToken))
            throw new InvalidOperationException("The selected checklist role does not exist.");
        var selectedIds = taskDefinitionIds.Distinct().ToArray();
        if (selectedIds.Length > 0 &&
            await db.TaskDefinitions.CountAsync(task => selectedIds.Contains(task.Id), cancellationToken) != selectedIds.Length)
            throw new InvalidOperationException("The selected checklist contains an unknown task definition.");
        var existingTemplate = template.Id == 0
            ? null
            : await db.ChecklistTemplates.Include(item => item.TemplateTasks)
                .SingleOrDefaultAsync(item => item.Id == template.Id, cancellationToken);

        if (template.Id != 0 && existingTemplate is null) return;

        if (existingTemplate is null)
        {
            existingTemplate = template;
            db.ChecklistTemplates.Add(existingTemplate);
        }
        else
        {
            existingTemplate.Name = template.Name;
            existingTemplate.Description = template.Description;
            existingTemplate.RoleId = template.RoleId;
            existingTemplate.IsActive = template.IsActive;
            db.ChecklistTemplateTasks.RemoveRange(existingTemplate.TemplateTasks);
        }

        for (var index = 0; index < selectedIds.Length; index++)
        {
            existingTemplate.TemplateTasks.Add(new ChecklistTemplateTask
            {
                ChecklistTemplate = existingTemplate,
                TaskDefinitionId = selectedIds[index],
                SortOrder = index + 1
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await db.ChecklistTemplates.FindAsync([id], cancellationToken);
        if (template is null) return false;
        if (await db.EmployeeChecklists.AnyAsync(item => item.ChecklistTemplateId == id, cancellationToken)) return false;
        db.ChecklistTemplates.Remove(template);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AssignAsync(int employeeId, int templateId, CancellationToken cancellationToken = default)
    {
        var template = await db.ChecklistTemplates.Include(item => item.TemplateTasks)
            .ThenInclude(link => link.TaskDefinition)
            .SingleOrDefaultAsync(item => item.Id == templateId && item.IsActive, cancellationToken);
        var employee = await db.Employees.Include(item => item.Checklists)
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);

        if (template is null || employee is null || employee.RoleId != template.RoleId ||
            employee.Checklists.Any(link => link.ChecklistTemplateId == templateId))
            return false;

        var assignedAt = DateTime.UtcNow;
        var checklist = new EmployeeChecklist
        {
            Employee = employee,
            ChecklistTemplate = template,
            TemplateNameSnapshot = template.Name,
            AssignedAtUtc = assignedAt
        };
        foreach (var link in template.TemplateTasks.OrderBy(link => link.SortOrder))
        {
            var definition = link.TaskDefinition;
            checklist.Tasks.Add(new OnboardingTask
            {
                Employee = employee,
                EmployeeChecklist = checklist,
                TaskDefinition = definition,
                TitleSnapshot = definition.Title,
                DescriptionSnapshot = definition.Description,
                CategorySnapshot = definition.Category,
                IsRequiredSnapshot = definition.IsRequired,
                AssignedAtUtc = assignedAt,
                DueDate = employee.StartDate.AddDays(definition.DueOffsetDays)
            });
        }
        db.EmployeeChecklists.Add(checklist);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<EmployeeChecklist>> ListAssignmentsAsync(CancellationToken cancellationToken = default) =>
        db.EmployeeChecklists.AsNoTracking().Include(item => item.Employee).Include(item => item.ChecklistTemplate)
            .Include(item => item.Tasks).OrderByDescending(item => item.AssignedAtUtc).ToListAsync(cancellationToken);
}

public sealed class OnboardingTaskService(OnboardProDbContext db) : IOnboardingTaskService
{
    public Task<List<OnboardingTask>> ListAsync(CancellationToken cancellationToken = default) =>
        db.OnboardingTasks.AsNoTracking().Include(task => task.Employee).OrderBy(task => task.DueDate).ToListAsync(cancellationToken);

    public Task<List<OnboardingTask>> ListForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default) =>
        db.OnboardingTasks.AsNoTracking().Where(task => task.EmployeeId == employeeId)
            .OrderBy(task => task.DueDate).ThenBy(task => task.TitleSnapshot).ToListAsync(cancellationToken);

    public Task<OnboardingTask?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.OnboardingTasks.Include(task => task.Employee)
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<bool> SetStatusAsync(int taskId, int employeeId, OnboardingTaskStatus status, CancellationToken cancellationToken = default)
    {
        var task = await db.OnboardingTasks.SingleOrDefaultAsync(
            item => item.Id == taskId && item.EmployeeId == employeeId, cancellationToken);
        if (task is null) return false;
        task.Status = status;
        task.CompletedAtUtc = status == OnboardingTaskStatus.Completed ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SaveAsync(OnboardingTask task, CancellationToken cancellationToken = default)
    {
        if (!await db.Employees.AnyAsync(employee => employee.Id == task.EmployeeId, cancellationToken))
            throw new InvalidOperationException("The selected employee does not exist.");
        task.CompletedAtUtc = task.Status == OnboardingTaskStatus.Completed ? DateTime.UtcNow : null;
        if (task.Id == 0) db.OnboardingTasks.Add(task);
        else db.Entry(task).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await db.OnboardingTasks.FindAsync([id], cancellationToken);
        if (task is null) return false;
        db.OnboardingTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class DocumentService(
    OnboardProDbContext db,
    IWebHostEnvironment environment,
    ILogger<DocumentService> logger) : IDocumentService
{
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg"
    };

    public Task<List<Document>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Documents.AsNoTracking().Include(document => document.Employee)
            .OrderBy(document => document.Status).ThenByDescending(document => document.UploadedAtUtc).ToListAsync(cancellationToken);

    public Task<List<Document>> ListForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default) =>
        db.Documents.AsNoTracking().Where(document => document.EmployeeId == employeeId)
            .OrderByDescending(document => document.UploadedAtUtc).ToListAsync(cancellationToken);

    public Task<Document?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Documents.Include(document => document.Employee).SingleOrDefaultAsync(document => document.Id == id, cancellationToken);

    public async Task<Document> UploadAsync(int employeeId, string documentType, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > MaximumFileSize)
            throw new InvalidDataException("Document files must be between 1 byte and 10 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
            throw new InvalidDataException("Only PDF, PNG, and JPEG documents are accepted.");

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "documents");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, storedFileName);

        await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var document = new Document
        {
            EmployeeId = employeeId,
            DocumentType = documentType.Trim(),
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = contentType,
            SizeBytes = file.Length
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Stored document {DocumentId} for employee {EmployeeId}", document.Id, employeeId);
        return document;
    }

    public async Task<bool> ReviewAsync(int id, DocumentStatus status, string? note, string reviewerUserId, CancellationToken cancellationToken = default)
    {
        if (status == DocumentStatus.PendingReview) return false;
        var document = await db.Documents.FindAsync([id], cancellationToken);
        if (document is null) return false;
        document.Status = status;
        document.ReviewNote = note?.Trim();
        document.ReviewedAtUtc = DateTime.UtcNow;
        document.ReviewedByUserId = reviewerUserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public string GetStoredFilePath(Document document)
    {
        var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "documents", document.StoredFileName));
        var directory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "documents")) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The stored document path is invalid.");
        return path;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var document = await db.Documents.FindAsync([id], cancellationToken);
        if (document is null) return false;
        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        var path = GetStoredFilePath(document);
        if (File.Exists(path)) File.Delete(path);
        return true;
    }
}

public sealed class AssetService(OnboardProDbContext db) : IAssetService
{
    public Task<List<Asset>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Assets.AsNoTracking().Include(asset => asset.Allocations).ThenInclude(allocation => allocation.Employee)
            .OrderBy(asset => asset.Category).ThenBy(asset => asset.Name).ToListAsync(cancellationToken);

    public Task<Asset?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Assets.Include(asset => asset.Allocations).ThenInclude(allocation => allocation.Employee)
            .SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public async Task SaveAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        if (asset.Id == 0) db.Assets.Add(asset);
        else db.Entry(asset).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var asset = await db.Assets.FindAsync([id], cancellationToken);
        if (asset is null) return false;
        if (await db.AssetAllocations.AnyAsync(allocation => allocation.AssetId == id, cancellationToken)) return false;
        db.Assets.Remove(asset);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<AssetAllocation>> ListAllocationsAsync(CancellationToken cancellationToken = default) =>
        db.AssetAllocations.AsNoTracking().Include(allocation => allocation.Employee).Include(allocation => allocation.Asset)
            .OrderByDescending(allocation => allocation.AllocatedOn).ToListAsync(cancellationToken);

    public Task<AssetAllocation?> GetAllocationAsync(int id, CancellationToken cancellationToken = default) =>
        db.AssetAllocations.Include(allocation => allocation.Employee).Include(allocation => allocation.Asset)
            .SingleOrDefaultAsync(allocation => allocation.Id == id, cancellationToken);

    public async Task SaveAllocationAsync(AssetAllocation allocation, CancellationToken cancellationToken = default)
    {
        if (!await db.Employees.AnyAsync(employee => employee.Id == allocation.EmployeeId, cancellationToken))
            throw new InvalidOperationException("The selected employee does not exist.");
        if (!await db.Assets.AnyAsync(asset => asset.Id == allocation.AssetId, cancellationToken))
            throw new InvalidOperationException("The selected asset does not exist.");
        if (allocation.Id == 0 && allocation.ReturnedOn.HasValue)
            throw new InvalidOperationException("Create a new allocation first, then record its return date when the asset is returned.");

        var isAvailable = await db.Assets.AnyAsync(asset => asset.Id == allocation.AssetId && !asset.IsRetired &&
            !db.AssetAllocations.Any(active => active.AssetId == asset.Id && active.ReturnedOn == null && active.Id != allocation.Id),
            cancellationToken);
        if (!isAvailable && allocation.ReturnedOn is null)
            throw new InvalidOperationException("The selected asset is retired or already allocated.");

        if (allocation.Id == 0) db.AssetAllocations.Add(allocation);
        else db.Entry(allocation).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAllocationAsync(int id, CancellationToken cancellationToken = default)
    {
        var allocation = await db.AssetAllocations.FindAsync([id], cancellationToken);
        if (allocation is null) return false;
        db.AssetAllocations.Remove(allocation);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class BuddyService(OnboardProDbContext db) : IBuddyService
{
    public Task<List<BuddyAssignment>> ListAsync(CancellationToken cancellationToken = default) =>
        db.BuddyAssignments.AsNoTracking().Include(assignment => assignment.Employee)
            .ThenInclude(employee => employee.OnboardingTasks)
            .Include(assignment => assignment.BuddyEmployee)
            .OrderBy(assignment => assignment.EndedOn).ThenBy(assignment => assignment.Employee.LastName)
            .ToListAsync(cancellationToken);

    public Task<List<BuddyAssignment>> ListForBuddyAsync(int buddyEmployeeId, CancellationToken cancellationToken = default) =>
        db.BuddyAssignments.AsNoTracking().Where(assignment => assignment.BuddyEmployeeId == buddyEmployeeId && assignment.EndedOn == null)
            .Include(assignment => assignment.Employee).Include(assignment => assignment.Employee.OnboardingTasks)
            .OrderBy(assignment => assignment.Employee.LastName).ToListAsync(cancellationToken);

    public Task<BuddyAssignment?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.BuddyAssignments.Include(assignment => assignment.Employee)
            .ThenInclude(employee => employee.OnboardingTasks)
            .Include(assignment => assignment.BuddyEmployee)
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public async Task SaveAsync(BuddyAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (assignment.EmployeeId == assignment.BuddyEmployeeId)
            throw new InvalidOperationException("An employee cannot be assigned as their own buddy.");
        if (assignment.EndedOn.HasValue && assignment.EndedOn.Value < assignment.AssignedOn)
            throw new InvalidOperationException("An assignment end date cannot be before its start date.");
        var employeeRoles = await db.Employees.Where(employee =>
                employee.Id == assignment.EmployeeId || employee.Id == assignment.BuddyEmployeeId)
            .Select(employee => new { employee.Id, RoleName = employee.Role.Name })
            .ToListAsync(cancellationToken);
        if (employeeRoles.SingleOrDefault(employee => employee.Id == assignment.EmployeeId)?.RoleName != AppRoles.NewJoiner ||
            employeeRoles.SingleOrDefault(employee => employee.Id == assignment.BuddyEmployeeId)?.RoleName != AppRoles.Buddy)
            throw new InvalidOperationException("Select a New Joiner and an employee assigned the Buddy role.");
        if (assignment.EndedOn is null &&
            await db.BuddyAssignments.AnyAsync(item => item.EmployeeId == assignment.EmployeeId &&
                item.EndedOn == null && item.Id != assignment.Id, cancellationToken))
            throw new InvalidOperationException("The employee already has an active buddy assignment.");
        if (assignment.Id == 0) db.BuddyAssignments.Add(assignment);
        else db.Entry(assignment).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await db.BuddyAssignments.FindAsync([id], cancellationToken);
        if (assignment is null) return false;
        db.BuddyAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class DashboardService(OnboardProDbContext db, IEmployeeService employees) : IDashboardService
{
    public async Task<DashboardViewModel> GetAsync(string? userId, bool isBuddy, bool isJoiner, CancellationToken cancellationToken = default)
    {
        var employeeId = 0;
        if (isBuddy || isJoiner)
        {
            var employee = userId is null ? null : await employees.GetForUserAsync(userId, cancellationToken);
            if (employee is null) return new DashboardViewModel();
            employeeId = employee.Id;
        }

        var employeeQuery = db.Employees.AsNoTracking().AsQueryable();
        if (isJoiner) employeeQuery = employeeQuery.Where(employee => employee.Id == employeeId);
        if (isBuddy)
        {
            var joinerIds = db.BuddyAssignments.Where(assignment => assignment.BuddyEmployeeId == employeeId && assignment.EndedOn == null)
                .Select(assignment => assignment.EmployeeId);
            employeeQuery = employeeQuery.Where(employee => joinerIds.Contains(employee.Id));
        }

        var employeesInScope = await employeeQuery
            .Select(employee => new EmployeeProgressViewModel
            {
                EmployeeId = employee.Id,
                Name = employee.FirstName + " " + employee.LastName,
                JobTitle = employee.JobTitle,
                StartDate = employee.StartDate,
                TotalTasks = employee.OnboardingTasks.Count,
                CompletedTasks = employee.OnboardingTasks.Count(task => task.Status == OnboardingTaskStatus.Completed)
            })
            .OrderBy(employee => employee.StartDate)
            .ToListAsync(cancellationToken);

        var scopedEmployeeIds = employeesInScope.Select(employee => employee.EmployeeId).ToArray();
        return new DashboardViewModel
        {
            EmployeeCount = employeesInScope.Count,
            Employees = employeesInScope,
            IncompleteTaskCount = await db.OnboardingTasks.CountAsync(task =>
                scopedEmployeeIds.Contains(task.EmployeeId) && task.Status != OnboardingTaskStatus.Completed, cancellationToken),
            PendingDocumentCount = isBuddy || isJoiner ? 0 : await db.Documents.CountAsync(document =>
                scopedEmployeeIds.Contains(document.EmployeeId) && document.Status == DocumentStatus.PendingReview, cancellationToken),
            AvailableAssetCount = isJoiner || isBuddy ? 0 : await db.Assets.CountAsync(asset => !asset.IsRetired &&
                !db.AssetAllocations.Any(allocation => allocation.AssetId == asset.Id && allocation.ReturnedOn == null), cancellationToken)
        };
    }
}
