using Microsoft.EntityFrameworkCore;

namespace TodoApp.TodoData
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Todo> Todos { get; set; }
        public DbSet<Cpr> Cprs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure the Todo entity
            modelBuilder.Entity<Todo>(entity =>
            {
                entity.HasKey(t => t.Id);
                
                entity.Property(t => t.Id)
                    .HasDefaultValueSql("gen_random_uuid()");
                
                entity.Property(t => t.CprNr)
                    .IsRequired();
                
                entity.Property(t => t.EncryptedItem)
                    .IsRequired()
                    .HasColumnType("bytea");
                
                entity.Ignore(t => t.Item);
                
                entity.Property(t => t.IsDone)
                    .IsRequired();
                    
                entity.HasOne(t => t.Cpr)
                    .WithMany()
                    .HasForeignKey(t => t.CprNr)
                    .HasPrincipalKey(c => c.CprPbkdf2)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configure the table name
                entity.ToTable("Todos");
            });
                
            // Configure the Cpr entity
            modelBuilder.Entity<Cpr>(entity =>
            {
                entity.HasKey(c => c.UserId);
                
                entity.Property(c => c.CprPbkdf2)
                    .IsRequired();
                entity.Property(c => c.CprBcrypt)
                    .IsRequired();
                entity.Property(c => c.CprKey)
                    .IsRequired();
                entity.HasAlternateKey(c => c.CprPbkdf2);
                entity.HasIndex(c => c.CprKey).IsUnique();
            });
        }
    }
}
