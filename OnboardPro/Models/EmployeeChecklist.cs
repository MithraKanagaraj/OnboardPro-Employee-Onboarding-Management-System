using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class EmployeeChecklist
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int ChecklistTemplateId { get; set; }

    [Required, StringLength(120)]
    public string TemplateNameSnapshot { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public Employee Employee { get; set; } = null!;
    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();
}
