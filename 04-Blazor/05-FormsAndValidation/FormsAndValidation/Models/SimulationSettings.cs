using System.ComponentModel.DataAnnotations;

namespace FormsAndValidation.Models;

public sealed class SimulationSettings : IValidatableObject
{
    [Required(ErrorMessage = "A simulation name is required.")]
    [StringLength(
        40,
        MinimumLength = 3,
        ErrorMessage = "The name must contain between 3 and 40 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(
        1,
        500,
        ErrorMessage = "Boid count must be between 1 and 500.")]
    public int BoidCount { get; set; } = 100;

    [Range(
        0.1,
        10.0,
        ErrorMessage = "Movement speed must be between 0.1 and 10.0.")]
    public double MovementSpeed { get; set; } = 2.0;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Name) &&
            !Name.Any(char.IsLetterOrDigit))
        {
            yield return new ValidationResult(
                "The name must contain at least one letter or digit.",
                [nameof(Name)]);
        }

        if (!double.IsFinite(MovementSpeed))
        {
            yield return new ValidationResult(
                "Movement speed must be a finite number.",
                [nameof(MovementSpeed)]);
        }
    }
}