using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using urbanBackend.Models;

namespace urbanBackend.Data
{
    public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            Database.SetCommandTimeout(300);
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public virtual DbSet<Cart> Carts { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<OrderItem> OrderItems { get; set; }

        public virtual DbSet<Payment> Payments { get; set; }

        public virtual DbSet<Product> Products { get; set; }

       

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD7B71A6551CF");

                entity.ToTable("Cart");

                entity.Property(e => e.CreateDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                    .HasForeignKey(d => d.ProductId)
                    .HasConstraintName("FK_Cart_Products");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFD2E81E9C");

                entity.Property(e => e.DeliveryDate).HasColumnType("datetime");
                entity.Property(e => e.ModifyDate).HasColumnType("datetime");
                entity.Property(e => e.OrderDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.OrderStatus).HasMaxLength(50);
                entity.Property(e => e.PaymentStatus).HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.UserId).HasMaxLength(450);

                
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED068144334A21");

                entity.Property(e => e.PriceAtPurchase).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__OrderItem__Order__6754599E");

                entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__OrderItem__Produ__68487DD7");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38928E1350");

                entity.Property(e => e.PaymentAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PaymentDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.PaymentMethod).HasMaxLength(100);
                entity.Property(e => e.PaymentStatus).HasMaxLength(50);

                entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Payments__OrderI__6C190EBB");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD5752FD40");

                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.CreateDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.ModifyDate).HasColumnType("datetime");
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.VendorId).HasMaxLength(450);

                
            });

            base.OnModelCreating(modelBuilder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
