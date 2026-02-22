using System.ComponentModel.DataAnnotations;

namespace PennaiWise.Api.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string? Color { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }
    public ICollection<Expense> Expenses { get; set; } = [];
}
