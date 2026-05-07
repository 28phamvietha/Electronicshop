using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace ElectronicsShop.Models
{
    public partial class Db_ElectronicsShop : DbContext
    {
        public Db_ElectronicsShop()
            : base("name=DefaultConnection")
        {
        }

        public virtual DbSet<Cart> Carts { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Order_Items> Order_Items { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Product_Images> Product_Images { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Shipment> Shipments { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Product_Specification> Product_Specifications { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
                .HasMany(e => e.Products)
                .WithRequired(e => e.Category)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Order_Items>()
                .Property(e => e.price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(e => e.total_amount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(e => e.order_note)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<Order>()
                .HasMany(e => e.Order_Items)
                .WithRequired(e => e.Order)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product_Images>()
                .Property(e => e.image_url)
                .IsUnicode(false);

            modelBuilder.Entity<Product>()
                .Property(e => e.price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .Property(e => e.discount_price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.Carts)
                .WithRequired(e => e.Product)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.Order_Items)
                .WithRequired(e => e.Product)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.Product_Images)
                .WithRequired(e => e.Product)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.Wishlists)
                .WithRequired(e => e.Product)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Shipment>()
                .HasMany(e => e.Orders)
                .WithRequired(e => e.Shipment)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .Property(e => e.email)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.password)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.phone)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Carts)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Orders)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Shipments)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Wishlists)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);
        }
    }
}
