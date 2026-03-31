using Microsoft.EntityFrameworkCore;
using SoftZorg.Models; // Zorg dat deze import bovenaan staat!

namespace SoftZorg.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // VOEG DEZE REGEL TOE:
        public DbSet<Melding> Meldingen { get; set; }

        // Laat de rest van het bestand (bijv. OnModelCreating als je dat hebt) intact
    }
}