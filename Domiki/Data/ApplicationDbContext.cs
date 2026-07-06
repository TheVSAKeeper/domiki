using Domiki.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<Domik> Domiks { get; set; }
        public DbSet<Manufacture> Manufactures { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderResource> OrderResources { get; set; }
        public DbSet<Neighbor> Neighbors { get; set; }
        public DbSet<NeighborReputation> NeighborReputations { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceType> ResourceTypes { get; set; }
        public DbSet<ModificatorType> ModificatorTypes { get; set; }
        public DbSet<Trait> Traits { get; set; }
        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerSkill> WorkerSkills { get; set; }

        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptResource> ReceiptResources { get; set; }

        public DbSet<DomikType> DomikTypes { get; set; }
        public DbSet<DomikTypeLevel> DomikTypeLevels { get; set; }
        public DbSet<DomikTypeLevelResource> DomikTypeLevelResources { get; set; }
        public DbSet<DomikTypeLevelModificator> DomikTypeLevelModificators { get; set; }
        public DbSet<DomikTypeLevelReceipt> DomikTypeLevelRecepts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Resource>()
                .HasKey(p => new
                {
                    p.PlayerId,
                    p.TypeId
                });

            modelBuilder.Entity<Domik>()
                .HasKey(p => new
                {
                    p.PlayerId,
                    p.Id
                });

            modelBuilder.Entity<Player>()
                .HasIndex(u => u.AspNetUserId)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .HasIndex(u => u.VillageName)
                .IsUnique()
                .HasFilter("\"VillageName\" IS NOT NULL");

            modelBuilder.Entity<Player>()
                .Navigation(e => e.Resources)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Trait>().HasData(
                new Trait { Id = 1, Name = "Обычный", LogicName = "ordinary", DurationPercent = 0 },
                new Trait { Id = 2, Name = "Проворный", LogicName = "nimble", DurationPercent = -10 },
                new Trait { Id = 3, Name = "Работящий", LogicName = "diligent", DurationPercent = -20 });

            modelBuilder.Entity<Worker>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<Worker>()
                .HasOne(s => s.Trait)
                .WithMany()
                .HasForeignKey(e => e.TraitId);

            modelBuilder.Entity<Worker>()
                .HasOne(s => s.Manufacture)
                .WithMany()
                .HasForeignKey(e => e.ManufactureId);

            modelBuilder.Entity<WorkerSkill>()
                .HasKey(p => new
                {
                    p.WorkerId,
                    p.DomikTypeId,
                });

            modelBuilder.Entity<WorkerSkill>()
                .HasOne(s => s.Worker)
                .WithMany(x => x.Skills)
                .HasForeignKey(e => e.WorkerId);

            modelBuilder.Entity<Resource>()
                .Navigation(e => e.Player)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Manufacture>()
                .HasOne(s => s.Domik)
                .WithMany(x => x.Manufactures)
                .HasForeignKey(e => new { e.DomikPlayerId, e.DomikId});

            modelBuilder.Entity<Order>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<Order>()
                .HasOne(s => s.Neighbor)
                .WithMany()
                .HasForeignKey(e => e.NeighborId);

            modelBuilder.Entity<OrderResource>()
                .HasKey(p => new
                {
                    p.OrderId,
                    p.ResourceTypeId,
                });

            modelBuilder.Entity<OrderResource>()
                .Navigation(e => e.Order)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<OrderResource>()
                .Navigation(e => e.ResourceType)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<NeighborReputation>()
                .HasKey(p => new
                {
                    p.PlayerId,
                    p.NeighborId,
                });

            modelBuilder.Entity<NeighborReputation>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<NeighborReputation>()
                .HasOne(s => s.Neighbor)
                .WithMany()
                .HasForeignKey(e => e.NeighborId);

            modelBuilder.Entity<Neighbor>().HasData(
                new Neighbor { Id = 1, Name = "Заречье", LogicName = "zarechye", PrimaryResourceTypeId = 6 },
                new Neighbor { Id = 2, Name = "Боровое", LogicName = "borovoe", PrimaryResourceTypeId = 7 },
                new Neighbor { Id = 3, Name = "Каменка", LogicName = "kamenka", PrimaryResourceTypeId = 2 },
                new Neighbor { Id = 4, Name = "Глинищи", LogicName = "glinischi", PrimaryResourceTypeId = 4 },
                new Neighbor { Id = 5, Name = "Дубрава", LogicName = "dubrava", PrimaryResourceTypeId = 3 });


            modelBuilder.Entity<DomikTypeLevel>()
                .HasKey(p => new
                {
                    p.DomikTypeId,
                    p.Value,
                });

            modelBuilder.Entity<DomikTypeLevel>()
                .HasOne(s => s.DomikType)
                .WithMany(x => x.Levels)
                .HasForeignKey(e => e.DomikTypeId);

            modelBuilder.Entity<DomikTypeLevelModificator>()
                .HasKey(p => new
                {
                    p.DomikTypeLevelDomikTypeId,
                    p.DomikTypeLevelValue,
                    p.ModificatorTypeId,
                });

            modelBuilder.Entity<DomikTypeLevelModificator>()
                .Navigation(e => e.DomikTypeLevel)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<DomikTypeLevelModificator>()
                .Navigation(e => e.ModificatorType)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<DomikTypeLevelReceipt>()
                .HasKey(p => new
                {
                    p.DomikTypeLevelDomikTypeId,
                    p.DomikTypeLevelValue,
                    p.ReceiptId,
                });

            modelBuilder.Entity<DomikTypeLevelReceipt>()
                .Navigation(e => e.DomikTypeLevel)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<DomikTypeLevelReceipt>()
                .Navigation(e => e.Receipt)
                .UsePropertyAccessMode(PropertyAccessMode.Property);


            modelBuilder.Entity<DomikTypeLevelResource>()
                .HasKey(p => new
                {
                    p.DomikTypeLevelDomikTypeId,
                    p.DomikTypeLevelValue,
                    p.ResourceTypeId,
                });

            modelBuilder.Entity<DomikTypeLevelResource>()
                .Navigation(e => e.DomikTypeLevel)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<DomikTypeLevelResource>()
                .Navigation(e => e.ResourceType)
                .UsePropertyAccessMode(PropertyAccessMode.Property);


            modelBuilder.Entity<ReceiptResource>()
                .HasKey(p => new
                {
                    p.ReceiptId,
                    p.ResourceTypeId,
                    p.IsInput,
                });

            modelBuilder.Entity<ReceiptResource>()
                .Navigation(e => e.Receipt)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<ReceiptResource>()
                .Navigation(e => e.ResourceType)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
        }
    }
}