using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class BuddyAssignment
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int BuddyEmployeeId { get; set; }

    public DateOnly AssignedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EndedOn { get; set; }
    public Employee Employee { get; set; } = null!;
    public Employee BuddyEmployee { get; set; } = null!;
}
