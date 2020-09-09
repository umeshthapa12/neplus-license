using Microsoft.EntityFrameworkCore;

namespace NeplusLicense.Entities
{
    public class LicenseDbContext : DbContext
    {
        public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ref link: https://docs.microsoft.com/en-us/ef/core/modeling/relationships?tabs=fluent-api%2Cfluent-api-simple-key%2Csimple-key#many-to-many
            modelBuilder.Entity<RefreshToken>()
                .HasOne<User>()
                .WithMany(p => p.RefreshTokens)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        }

        public virtual DbSet<User>             User             { get; set; }
        public virtual DbSet<RefreshToken>     RefreshToken     { get; set; }
        public virtual DbSet<RequestedVendors> RequestedVendors { get; set; }
        public virtual DbSet<Vendors>          Vendors          { get; set; }
    }
}