using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnboardPro.Models;

namespace OnboardPro.ViewModels;

public sealed class DashboardViewModel
{
    public int EmployeeCount { get; set; }
    public int IncompleteTaskCount { get; set; }
    public int PendingDocumentCount { get; set; }
    public int AvailableAssetCount { get; set; }
    public IReadOnlyList<EmployeeProgressViewModel> Employees { get; set; } = [];
}

public sealed class EmployeeProgressViewModel
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int ProgressPercent => TotalTasks == 0 ? 0 : (int)Math.Round(CompletedTasks * 100d / TotalTasks);
}

public sealed class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Remote(action: "VerifyEmail", controller: "Employees", AdditionalFields = nameof(Id))]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Job title")]
    public string JobTitle { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, Display(Name = "Access role")]
    public int RoleId { get; set; }

    public IEnumerable<SelectListItem> Roles { get; set; } = [];
}

public sealed class ChecklistTemplateFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Role")]
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    [Display(Name = "Task definitions")]
    public List<int> SelectedTaskDefinitionIds { get; set; } = [];
    public IEnumerable<SelectListItem> Roles { get; set; } = [];
    public IEnumerable<SelectListItem> TaskDefinitions { get; set; } = [];
}

public sealed class TaskDefinitionFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Description { get; set; }
    [Range(0, 365)]
    [Display(Name = "Due (days after start date)")]
    public int DueOffsetDays { get; set; }
    [Required, StringLength(60)]
    public string Category { get; set; } = "General";
    [Display(Name = "Required task")]
    public bool IsRequired { get; set; } = true;
}

public sealed class OnboardingTaskFormViewModel
{
    public int Id { get; set; }
    [Required]
    public int EmployeeId { get; set; }
    [Required, StringLength(160)]
    public string TitleSnapshot { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? DescriptionSnapshot { get; set; }
    [Required, StringLength(60)]
    public string CategorySnapshot { get; set; } = "General";
    public bool IsRequiredSnapshot { get; set; }
    public OnboardingTaskStatus Status { get; set; }
    [DataType(DataType.Date)]
    public DateOnly? DueDate { get; set; }
    public IEnumerable<SelectListItem> Employees { get; set; } = [];
}

public sealed class DocumentUploadViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Document type")]
    public string DocumentType { get; set; } = string.Empty;

    [Required, DataType(DataType.Upload)]
    public IFormFile? File { get; set; }
}

public sealed class DocumentReviewViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required]
    public DocumentStatus Status { get; set; }
    [StringLength(500)]
    [Display(Name = "Review note")]
    public string? ReviewNote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Status))
            yield return new ValidationResult("Choose an approval or rejection decision.", new[] { nameof(Status) });
        if (Status == DocumentStatus.PendingReview)
            yield return new ValidationResult("Choose an approval or rejection decision.", new[] { nameof(Status) });
    }
}

public sealed class AssetFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(60)]
    public string Category { get; set; } = string.Empty;
    [Required, StringLength(100)]
    [Display(Name = "Serial number")]
    public string SerialNumber { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Notes { get; set; }
    [Display(Name = "Retired")]
    public bool IsRetired { get; set; }
}

public sealed class AssetAllocationFormViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required]
    public int EmployeeId { get; set; }
    [Required]
    public int AssetId { get; set; }
    [Required, DataType(DataType.Date)]
    public DateOnly AllocatedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [DataType(DataType.Date)]
    public DateOnly? ReturnedOn { get; set; }
    [StringLength(300)]
    public string? Notes { get; set; }
    public IEnumerable<SelectListItem> Employees { get; set; } = [];
    public IEnumerable<SelectListItem> Assets { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ReturnedOn.HasValue && ReturnedOn.Value < AllocatedOn)
            yield return new ValidationResult("The return date cannot be before the allocation date.", new[] { nameof(ReturnedOn) });
    }
}

public sealed class BuddyAssignmentFormViewModel
{
    public int Id { get; set; }
    [Required, Display(Name = "New joiner")]
    public int EmployeeId { get; set; }
    [Required, Display(Name = "Buddy")]
    public int BuddyEmployeeId { get; set; }
    [Required, DataType(DataType.Date)]
    [Display(Name = "Assigned on")]
    public DateOnly AssignedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [DataType(DataType.Date)]
    [OnboardPro.Validation.DateNotBefore(nameof(AssignedOn), ErrorMessage = "The assignment end date cannot be before its start date.")]
    [Display(Name = "Assignment ended on")]
    public DateOnly? EndedOn { get; set; }
    public IEnumerable<SelectListItem> Joiners { get; set; } = [];
    public IEnumerable<SelectListItem> BuddyEmployees { get; set; } = [];
}

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class EmployeeAccountViewModel
{
    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }
    [Required, StringLength(100, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    public IEnumerable<SelectListItem> Employees { get; set; } = [];
}

public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 12)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;
    [DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
