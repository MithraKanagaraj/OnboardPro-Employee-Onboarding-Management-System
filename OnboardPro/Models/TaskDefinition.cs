using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class TaskDefinition
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 365)]
    public int DueOffsetDays { get; set; }

    [Required, StringLength(60)]
    public string Category { get; set; } = "General";

    public bool IsRequired { get; set; } = true;
    public ICollection<ChecklistTemplateTask> TemplateTasks { get; set; } = new List<ChecklistTemplateTask>();
}
