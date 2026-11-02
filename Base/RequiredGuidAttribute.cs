using System.ComponentModel.DataAnnotations;

namespace Zuhid.Base;

public class RequiredGuidAttribute : ValidationAttribute
{
  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    return value == null || value is not Guid || (Guid)value == Guid.Empty
        ? new ValidationResult($"{validationContext.MemberName} must be a valid Guid.")
        : ValidationResult.Success;
  }
}
