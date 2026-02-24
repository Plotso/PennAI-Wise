using System.ComponentModel.DataAnnotations;

namespace PennaiWise.Api.Models;

/// <summary>
/// Represents a supported currency (e.g. EUR, USD, GBP).
/// The <see cref="Code"/> is the ISO 4217 three-letter code and serves as the primary key.
/// </summary>
public class Currency
{
    [Key]
    [MaxLength(3)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    public string Symbol { get; set; } = string.Empty;
}
