using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Attributes;

public class NoWhiteSpaceOnlyAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var str = value as string;
        if (string.IsNullOrWhiteSpace(str))
        {
            return new ValidationResult("Field cannot be empty or whitespace.");
        }
        return ValidationResult.Success;
    }
}