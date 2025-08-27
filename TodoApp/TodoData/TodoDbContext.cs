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
                
                entity.Property(t => t.Item)
                    .IsRequired()
                    .HasColumnType("text");
                
                entity.Property(t => t.CprNr)
                    .IsRequired();
                
                entity.Property(t => t.IsDone)
                    .IsRequired();
                    
                entity.HasOne(t => t.Cpr)
                    .WithMany()
                    .HasForeignKey(t => t.CprNr)
                    .HasPrincipalKey(c => c.CprNr)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configure the table name
                entity.ToTable("Todos");
            });
                
            // Configure the Cpr entity
            modelBuilder.Entity<Cpr>(entity =>
            {
                entity.HasKey(c => c.UserId);
                
                entity.Property(c => c.CprNr)
                    .IsRequired()
                    .HasMaxLength(10);
                
                // Using the new ToTable method with HasCheckConstraint
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Cpr_CprNr_Format", "\"CprNr\" ~ '^[0-9]{10}$'"));
                
                entity.HasIndex(c => c.CprNr)
                    .IsUnique();
            });
        }
    }
}
