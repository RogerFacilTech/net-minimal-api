using Microsoft.EntityFrameworkCore;

namespace Pix.MockServer.Infrastructure;

public class PixDbContext : DbContext
{
    public PixDbContext(DbContextOptions<PixDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurações de entidades do Pix
    }
}