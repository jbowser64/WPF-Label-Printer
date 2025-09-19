using Microsoft.EntityFrameworkCore;
namespace DesktopLabelPrinter.Data
{
    public partial class PartsAndLocationsContext : DbContext
    {
        public PartsAndLocationsContext()
        {
        }

        public PartsAndLocationsContext(DbContextOptions<PartsAndLocationsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PartsAndLocation> PartsAndLocations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer("Data Source=localhost\\SQLEXPRESS;Initial Catalog=PartsAndLocations;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;");
        // try Data Source= /PartsandLocations.db
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PartsAndLocation>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.Abc).HasMaxLength(50).HasColumnName("ABC");
                entity.Property(e => e.Bin).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(50);
                entity.Property(e => e.LastCount).HasColumnName("LastCount");
                entity.Property(e => e.Material).HasMaxLength(50);
                entity.Property(e => e.Plant).HasMaxLength(50);
                entity.Property(e => e.Qty).HasColumnName("QTY");
                entity.Property(e => e.Sloc).HasMaxLength(50).HasColumnName("SLOC");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}




