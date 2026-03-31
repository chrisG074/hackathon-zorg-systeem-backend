using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SoftZorg.Models;

namespace SoftZorg.Data
{
    // BELANGRIJK: We erven nu van IdentityDbContext zodat de login-tabellen (users/roles) werken!
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Jouw eigen tabellen
        public DbSet<Melding> Meldingen { get; set; }

        // Mocht je later relaties willen toevoegen, dan doe je dat hier:
        // protected override void OnModelCreating(ModelBuilder builder)
        // {
        //     base.OnModelCreating(builder);
        // }
    }
}