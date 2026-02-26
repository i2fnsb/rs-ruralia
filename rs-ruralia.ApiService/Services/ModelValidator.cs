using System.ComponentModel.DataAnnotations;

namespace rs_ruralia.ApiService.Services;

public class ModelValidator
{
    public static void Validate<T>(T model) where T : class
    {
        if (model == null)
        {
            throw new ValidationException("Model cannot be null");
        }

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(
            model, 
            validationContext, 
            validationResults, 
            validateAllProperties: true);

        if (!isValid)
        {
            var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }
    }

    public static bool TryValidate<T>(T model, out List<ValidationResult> validationResults) where T : class
    {
        validationResults = new List<ValidationResult>();

        if (model == null)
        {
            validationResults.Add(new ValidationResult("Model cannot be null"));
            return false;
        }

        var validationContext = new ValidationContext(model);
        return Validator.TryValidateObject(
            model, 
            validationContext, 
            validationResults, 
            validateAllProperties: true);
    }
}