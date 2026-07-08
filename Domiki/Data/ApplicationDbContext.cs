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
        public DbSet<Blueprint> Blueprints { get; set; }
        public DbSet<PlayerBlueprint> PlayerBlueprints { get; set; }
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

        public DbSet<WeatherType> WeatherTypes { get; set; }
        public DbSet<WeatherTypeEffect> WeatherTypeEffects { get; set; }
        public DbSet<WeatherPeriod> WeatherPeriods { get; set; }

        public DbSet<ExpeditionType> ExpeditionTypes { get; set; }
        public DbSet<ExpeditionLoot> ExpeditionLoot { get; set; }
        public DbSet<Expedition> Expeditions { get; set; }

        public DbSet<DecorType> DecorTypes { get; set; }
        public DbSet<DecorCost> DecorCosts { get; set; }
        public DbSet<PlayerDecor> PlayerDecors { get; set; }

        public DbSet<TolokaType> TolokaTypes { get; set; }
        public DbSet<Toloka> Tolokas { get; set; }
        public DbSet<TolokaContribution> TolokaContributions { get; set; }
        public DbSet<TradeLot> TradeLots { get; set; }

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
                new Trait { Id = 1, Name = "Обычный", LogicName = "ordinary", DurationPercent = 0, NoFatigue = false },
                new Trait { Id = 2, Name = "Проворный", LogicName = "nimble", DurationPercent = -10, NoFatigue = false },
                new Trait { Id = 3, Name = "Работящий", LogicName = "diligent", DurationPercent = -20, NoFatigue = false },
                new Trait { Id = 4, Name = "Соня", LogicName = "sonya", DurationPercent = 15, NoFatigue = true });

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

            modelBuilder.Entity<Worker>()
                .HasOne(s => s.Expedition)
                .WithMany()
                .HasForeignKey(e => e.ExpeditionId);

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

            modelBuilder.Entity<Blueprint>()
                .HasOne(s => s.DomikType)
                .WithMany()
                .HasForeignKey(e => e.DomikTypeId);

            modelBuilder.Entity<Blueprint>()
                .HasOne(s => s.Neighbor)
                .WithMany()
                .HasForeignKey(e => e.NeighborId);

            modelBuilder.Entity<PlayerBlueprint>()
                .HasKey(p => new
                {
                    p.PlayerId,
                    p.BlueprintId,
                });

            modelBuilder.Entity<PlayerBlueprint>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<PlayerBlueprint>()
                .HasOne(s => s.Blueprint)
                .WithMany()
                .HasForeignKey(e => e.BlueprintId);

            modelBuilder.Entity<Neighbor>().HasData(
                new Neighbor { Id = 1, Name = "Заречье", LogicName = "zarechye", PrimaryResourceTypeId = 6, UnlockLevel = 8 },
                new Neighbor { Id = 2, Name = "Боровое", LogicName = "borovoe", PrimaryResourceTypeId = 7, UnlockLevel = 8 },
                new Neighbor { Id = 3, Name = "Каменка", LogicName = "kamenka", PrimaryResourceTypeId = 2, UnlockLevel = 3 },
                new Neighbor { Id = 4, Name = "Глинищи", LogicName = "glinischi", PrimaryResourceTypeId = 4, UnlockLevel = 0 },
                new Neighbor { Id = 5, Name = "Дубрава", LogicName = "dubrava", PrimaryResourceTypeId = 3, UnlockLevel = 0 });


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

            modelBuilder.Entity<WeatherTypeEffect>()
                .HasKey(p => new
                {
                    p.WeatherTypeId,
                    p.DomikTypeId,
                });

            modelBuilder.Entity<WeatherTypeEffect>()
                .HasOne(s => s.WeatherType)
                .WithMany()
                .HasForeignKey(e => e.WeatherTypeId);

            modelBuilder.Entity<WeatherPeriod>()
                .HasOne(s => s.WeatherType)
                .WithMany()
                .HasForeignKey(e => e.WeatherTypeId);

            modelBuilder.Entity<ExpeditionLoot>()
                .HasKey(p => new
                {
                    p.ExpeditionTypeId,
                    p.ResourceTypeId,
                });

            modelBuilder.Entity<ExpeditionLoot>()
                .HasOne(s => s.ExpeditionType)
                .WithMany()
                .HasForeignKey(e => e.ExpeditionTypeId);

            modelBuilder.Entity<Expedition>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<Expedition>()
                .HasOne(s => s.ExpeditionType)
                .WithMany()
                .HasForeignKey(e => e.ExpeditionTypeId);

            modelBuilder.Entity<DecorCost>()
                .HasKey(p => new
                {
                    p.DecorTypeId,
                    p.ResourceTypeId,
                });

            modelBuilder.Entity<DecorCost>()
                .HasOne(s => s.DecorType)
                .WithMany()
                .HasForeignKey(e => e.DecorTypeId);

            modelBuilder.Entity<DecorCost>()
                .HasOne(s => s.ResourceType)
                .WithMany()
                .HasForeignKey(e => e.ResourceTypeId);

            modelBuilder.Entity<PlayerDecor>()
                .HasKey(p => new
                {
                    p.PlayerId,
                    p.DecorTypeId,
                });

            modelBuilder.Entity<PlayerDecor>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<PlayerDecor>()
                .HasOne(s => s.DecorType)
                .WithMany()
                .HasForeignKey(e => e.DecorTypeId);

            modelBuilder.Entity<TolokaType>()
                .HasOne(s => s.ResourceType)
                .WithMany()
                .HasForeignKey(e => e.ResourceTypeId);

            modelBuilder.Entity<Toloka>()
                .HasOne(s => s.TolokaType)
                .WithMany()
                .HasForeignKey(e => e.TolokaTypeId);

            modelBuilder.Entity<TolokaContribution>()
                .HasKey(p => new
                {
                    p.TolokaId,
                    p.PlayerId,
                });

            modelBuilder.Entity<TolokaContribution>()
                .HasOne(s => s.Toloka)
                .WithMany()
                .HasForeignKey(e => e.TolokaId);

            modelBuilder.Entity<TolokaContribution>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId);

            modelBuilder.Entity<TradeLot>()
                .HasOne(s => s.Seller)
                .WithMany()
                .HasForeignKey(e => e.SellerId);
        }
    }
}