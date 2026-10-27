using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class Employee
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required]
    public int RoleId { get; set; }

    public string? IdentityUserId { get; set; }
    public Role Role { get; set; } = null!;
    public ApplicationUser? IdentityUser { get; set; }
    public ICollection<EmployeeChecklist> Checklists { get; set; } = new List<EmployeeChecklist>();
    public ICollection<OnboardingTask> OnboardingTasks { get; set; } = new List<OnboardingTask>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<AssetAllocation> AssetAllocations { get; set; } = new List<AssetAllocation>();
    public ICollection<BuddyAssignment> BuddyAssignments { get; set; } = new List<BuddyAssignment>();
    public ICollection<BuddyAssignment> BuddyForAssignments { get; set; } = new List<BuddyAssignment>();

    public string FullName => $"{FirstName} {LastName}";
}
