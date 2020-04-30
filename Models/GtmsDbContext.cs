using Microsoft.EntityFrameworkCore;

namespace messageapi.Models
{
    public class GtmsDbContext : DbContext
    {
        public GtmsDbContext(DbContextOptions<GtmsDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder){
            base.OnModelCreating(builder);
            builder.Entity<ReceivedMessage>().ToTable("ReceivedMessage");
        }        

        public DbSet<ReceivedMessage> ReceivedMessages { get; set; }
    }
}