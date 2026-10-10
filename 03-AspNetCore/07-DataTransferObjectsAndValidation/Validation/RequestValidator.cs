using System.ComponentModel.DataAnnotations;

namespace DataTransferObjectsAndValidation.Validation;

public static class RequestValidator
{
    public static Dictionary<string, string[]> Validate<T>(T request)
        where T : notnull
    {
        List<ValidationResult> results = [];

        bool isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        if (isValid)
        {
            return [];
        }

        return results
            .SelectMany(result =>
                result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, member) => new
                {
                    Member = member,
                    Message = result.ErrorMessage ?? "Invalid value."
                })
            .GroupBy(item => item.Member, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Message).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}