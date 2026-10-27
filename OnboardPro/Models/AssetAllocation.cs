using System.ComponentModel.DataAnnotations;
using OnboardPro.Validation;

namespace OnboardPro.Models;

public sealed class AssetAllocation : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AssetId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly AllocatedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    [DateNotBefore(nameof(AllocatedOn), ErrorMessage = "The return date cannot be before the allocation date.")]
    public DateOnly? ReturnedOn { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
    public Asset Asset { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ReturnedOn.HasValue && ReturnedOn.Value < AllocatedOn)
        {
            yield return new ValidationResult(
                "The return date cannot be before the allocation date.",
                new[] { nameof(ReturnedOn) });
        }
    }
}
