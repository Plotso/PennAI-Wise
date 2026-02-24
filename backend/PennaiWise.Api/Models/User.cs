using System.ComponentModel.DataAnnotations;

namespace PennaiWise.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(3)]
    public string? DefaultCurrencyCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Currency? DefaultCurrency { get; set; }
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<ExchangeRate> ExchangeRates { get; set; } = [];
}
