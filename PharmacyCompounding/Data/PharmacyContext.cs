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
            mb.Entity<Prescription>().HasIndex(p => p.Number).IsUnique();
            mb.Entity<Doctor>().HasIndex(d => d.LicenseNumber).IsUnique();
            mb.Entity<Warehouse>().HasIndex(w => w.Name).IsUnique();

            // Order numbers come from a database sequence, so two orders created at the same time
            // never get the same number (Max + 1 in the application could give both the same one)
            mb.HasSequence<int>("order_numbers").StartsAt(1001);
            mb.Entity<Order>().Property(o => o.Number).HasDefaultValueSql("nextval('order_numbers')");

            // Optimistic concurrency: PostgreSQL changes the xmin system column on every UPDATE,
            // so EF Core adds "WHERE xmin = @original" and throws DbUpdateConcurrencyException on a conflict
            mb.Entity<Ingredient>().Property(i => i.Version).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
            mb.Entity<Order>().Property(o => o.Version).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

            // Consistency rules enforced by the database itself, whichever program writes the data
            mb.Entity<Ingredient>().ToTable(t =>
            {
                t.HasCheckConstraint("ck_ingredients_stock", "stock >= 0 AND min_stock >= 0");
                t.HasCheckConstraint("ck_ingredients_price", "price_per_unit >= 0");
            });
            mb.Entity<Order>().ToTable(t => t.HasCheckConstraint("ck_orders_total", "total >= 0"));
            mb.Entity<OrderItem>().ToTable(t => t.HasCheckConstraint("ck_order_items_quantity", "quantity > 0 AND work_cost >= 0"));
            mb.Entity<Component>().ToTable(t => t.HasCheckConstraint("ck_components_amount", "amount > 0"));
            mb.Entity<Payment>().ToTable(t => t.HasCheckConstraint("ck_payments_amount", "amount >= 0"));
            mb.Entity<Prescription>().ToTable(t => t.HasCheckConstraint("ck_prescriptions_validity", "validity_days BETWEEN 1 AND 365"));

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
