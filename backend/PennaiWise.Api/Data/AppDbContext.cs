using Microsoft.EntityFrameworkCore;

namespace PennaiWise.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Add DbSet<T> properties here as entities are created
}
