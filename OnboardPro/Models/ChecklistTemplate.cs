using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class ChecklistTemplate
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Role Role { get; set; } = null!;
    public ICollection<ChecklistTemplateTask> TemplateTasks { get; set; } = new List<ChecklistTemplateTask>();
    public ICollection<EmployeeChecklist> EmployeeChecklists { get; set; } = new List<EmployeeChecklist>();
}
