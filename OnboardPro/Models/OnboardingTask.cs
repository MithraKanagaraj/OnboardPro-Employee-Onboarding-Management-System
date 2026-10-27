using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public enum OnboardingTaskStatus
{
    NotStarted,
    InProgress,
    Completed
}

public sealed class OnboardingTask
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? EmployeeChecklistId { get; set; }
    public int? TaskDefinitionId { get; set; }

    [Required, StringLength(160)]
    public string TitleSnapshot { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? DescriptionSnapshot { get; set; }

    [Required, StringLength(60)]
    public string CategorySnapshot { get; set; } = "General";

    public bool IsRequiredSnapshot { get; set; }
    public OnboardingTaskStatus Status { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Employee Employee { get; set; } = null!;
    public EmployeeChecklist? EmployeeChecklist { get; set; }
    public TaskDefinition? TaskDefinition { get; set; }
}
