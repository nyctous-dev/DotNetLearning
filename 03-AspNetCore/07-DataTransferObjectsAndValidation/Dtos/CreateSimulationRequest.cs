using System.ComponentModel.DataAnnotations;

namespace DataTransferObjectsAndValidation.Dtos;

public sealed class CreateSimulationRequest
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 500)]
    public int InitialBoidCount { get; init; }
}