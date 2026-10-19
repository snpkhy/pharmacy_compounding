using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Data
{
    // Data context: the bridge between the database and the model classes (Code First)
    public class PharmacyContext : DbContext
    {
        public PharmacyContext(DbContextOptions<PharmacyContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Pharmacist> Pharmacists => Set<Pharmacist>();
        public DbSet<PharmTechnologist> Technologists => Set<PharmTechnologist>();
        public DbSet<PharmacyManager> Managers => Set<PharmacyManager>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Prescription> Prescriptions => Set<Prescription>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Component> Components => Set<Component>();
        public DbSet<Ingredient> Ingredients => Set<Ingredient>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<ManufacturingAct> ManufacturingActs => Set<ManufacturingAct>();
        public DbSet<QualityCheck> QualityChecks => Set<QualityCheck>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // The hierarchy User → Client / Employee → Pharmacist / PharmTechnologist / PharmacyManager
            // is stored in a single Users table (Table-Per-Hierarchy)
            mb.Entity<Employee>();
            mb.Entity<User>()
              .HasDiscriminator<string>("UserType")
              .HasValue<Client>(Roles.Client)
              .HasValue<Pharmacist>(Roles.Pharmacist)
              .HasValue<PharmTechnologist>(Roles.Technologist)
              .HasValue<PharmacyManager>(Roles.Manager);
            mb.Entity<User>().Ignore(u => u.Role);
            mb.Entity<User>().HasIndex(u => u.Email).IsUnique();

            mb.Entity<Order>().HasIndex(o => o.Number).IsUnique();
            mb.Entity<Order>().Property(o => o.Status).HasConversion<string>();
            mb.Entity<Ingredient>().HasIndex(i => i.Code).IsUnique();

            // Order 1 — 1 Prescription
            mb.Entity<Order>()
              .HasOne(o => o.Prescription).WithOne(p => p.Order)
              .HasForeignKey<Order>(o => o.PrescriptionId)
              .OnDelete(DeleteBehavior.Restrict);

            // Composition: items and components are deleted with their owner
            mb.Entity<OrderItem>()
              .HasOne(i => i.Order).WithMany(o => o.Items)
              .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<Component>()
              .HasOne(c => c.OrderItem).WithMany(i => i.Components)
              .HasForeignKey(c => c.OrderItemId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<Component>()
              .HasOne(c => c.Ingredient).WithMany()
              .HasForeignKey(c => c.IngredientId).OnDelete(DeleteBehavior.Restrict);

            // Aggregation: Warehouse 1 — 0..* Ingredient
            mb.Entity<Ingredient>()
              .HasOne(i => i.Warehouse).WithMany(w => w.Ingredients)
              .HasForeignKey(i => i.WarehouseId).OnDelete(DeleteBehavior.Restrict);

            // Order 1 — 0..1 Payment
            mb.Entity<Payment>()
              .HasOne(p => p.Order).WithOne(o => o.Payment)
              .HasForeignKey<Payment>(p => p.OrderId);

            // ManufacturingAct 1 — 0..1 QualityCheck
            mb.Entity<QualityCheck>()
              .HasOne(q => q.ManufacturingAct).WithOne(a => a.QualityCheck)
              .HasForeignKey<QualityCheck>(q => q.ManufacturingActId);

            mb.Entity<ManufacturingAct>()
              .HasOne(a => a.Technologist).WithMany(t => t.ManufacturingActs)
              .HasForeignKey(a => a.TechnologistId).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<QualityCheck>()
              .HasOne(q => q.Pharmacist).WithMany(p => p.QualityChecks)
              .HasForeignKey(q => q.PharmacistId).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Order>()
              .HasOne(o => o.Client).WithMany(c => c.Orders)
              .HasForeignKey(o => o.ClientId).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Prescription>()
              .HasOne(p => p.Doctor).WithMany(d => d.Prescriptions)
              .HasForeignKey(p => p.DoctorId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
