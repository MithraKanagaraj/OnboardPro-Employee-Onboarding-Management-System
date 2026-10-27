namespace OnboardPro.Models;

public sealed class ChecklistTemplateTask
{
    public int ChecklistTemplateId { get; set; }
    public int TaskDefinitionId { get; set; }
    public int SortOrder { get; set; }
    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public TaskDefinition TaskDefinition { get; set; } = null!;
}
