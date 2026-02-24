using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PennaiWise.Api.Models;

/// <summary>
/// A user-defined exchange rate for a currency pair, effective from a given date.
/// When converting an expense, the system picks the rate with the latest
/// <see cref="EffectiveDate"/> that is on or before the expense date.
/// </summary>
public class ExchangeRate
{
    public int Id { get; set; }

    [Required]
    [MaxLength(3)]
    public string FromCurrencyCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(3)]
    public string ToCurrencyCode { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal Rate { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Currency FromCurrency { get; set; } = null!;
    public Currency ToCurrency { get; set; } = null!;
    public User User { get; set; } = null!;
}
