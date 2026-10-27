using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OnboardPro.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DateNotBeforeAttribute(string comparisonProperty) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var comparisonValue = validationContext.ObjectType
            .GetProperty(comparisonProperty, BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(validationContext.ObjectInstance);

        if (value is DateOnly valueDate && comparisonValue is DateOnly comparisonDate && valueDate < comparisonDate)
        {
            return new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} cannot be before {comparisonProperty}.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
