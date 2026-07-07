using System;
using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domiki.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260707110000_Stage3Blueprints")]
    partial class Stage3Blueprints
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Domiki.Models.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Domiki.Web.Data.Blueprint", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DomikTypeId")
                        .HasColumnType("integer");

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("NeighborId")
                        .HasColumnType("integer");

                    b.Property<int>("ReputationThreshold")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("DomikTypeId");

                    b.HasIndex("NeighborId");

                    b.ToTable("Blueprints");
                });

            modelBuilder.Entity("Domiki.Web.Data.Domik", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("Id")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("Level")
                        .HasColumnType("integer");

                    b.Property<int>("TypeId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("UpgradeCalculateDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<double?>("UpgradeSeconds")
                        .HasColumnType("double precision");

                    b.HasKey("PlayerId", "Id");

                    b.ToTable("Domiks");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<int>("MaxCount")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("UnlockLevel")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("DomikTypes");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevel", b =>
                {
                    b.Property<int>("DomikTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("Value")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("MaxManufactureCount")
                        .HasColumnType("integer");

                    b.Property<int>("UpgradeSeconds")
                        .HasColumnType("integer");

                    b.HasKey("DomikTypeId", "Value");

                    b.ToTable("DomikTypeLevels");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevelModificator", b =>
                {
                    b.Property<int>("DomikTypeLevelDomikTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("DomikTypeLevelValue")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("ModificatorTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(3);

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.HasKey("DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ModificatorTypeId");

                    b.HasIndex("ModificatorTypeId");

                    b.ToTable("DomikTypeLevelModificators");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevelReceipt", b =>
                {
                    b.Property<int>("DomikTypeLevelDomikTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("DomikTypeLevelValue")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("ReceiptId")
                        .HasColumnType("integer")
                        .HasColumnOrder(3);

                    b.HasKey("DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ReceiptId");

                    b.HasIndex("ReceiptId");

                    b.ToTable("DomikTypeLevelReceipts");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevelResource", b =>
                {
                    b.Property<int>("DomikTypeLevelDomikTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("DomikTypeLevelValue")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("ResourceTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(3);

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.HasKey("DomikTypeLevelDomikTypeId", "DomikTypeLevelValue", "ResourceTypeId");

                    b.HasIndex("ResourceTypeId");

                    b.ToTable("DomikTypeLevelResources");
                });

            modelBuilder.Entity("Domiki.Web.Data.Manufacture", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DomikId")
                        .HasColumnType("integer");

                    b.Property<int>("DomikPlayerId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("FinishDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("OutputPercent")
                        .HasColumnType("integer");

                    b.Property<int>("PlodderCount")
                        .HasColumnType("integer");

                    b.Property<int>("ReceiptId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("DomikPlayerId", "DomikId");

                    b.ToTable("Manufactures");
                });

            modelBuilder.Entity("Domiki.Web.Data.ModificatorType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ModificatorTypes");
                });

            modelBuilder.Entity("Domiki.Web.Data.Neighbor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("PrimaryResourceTypeId")
                        .HasColumnType("integer");

                    b.Property<int>("UnlockLevel")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Neighbors");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            LogicName = "zarechye",
                            Name = "Заречье",
                            PrimaryResourceTypeId = 6,
                            UnlockLevel = 8
                        },
                        new
                        {
                            Id = 2,
                            LogicName = "borovoe",
                            Name = "Боровое",
                            PrimaryResourceTypeId = 7,
                            UnlockLevel = 8
                        },
                        new
                        {
                            Id = 3,
                            LogicName = "kamenka",
                            Name = "Каменка",
                            PrimaryResourceTypeId = 2,
                            UnlockLevel = 3
                        },
                        new
                        {
                            Id = 4,
                            LogicName = "glinischi",
                            Name = "Глинищи",
                            PrimaryResourceTypeId = 4,
                            UnlockLevel = 0
                        },
                        new
                        {
                            Id = 5,
                            LogicName = "dubrava",
                            Name = "Дубрава",
                            PrimaryResourceTypeId = 3,
                            UnlockLevel = 0
                        });
                });

            modelBuilder.Entity("Domiki.Web.Data.NeighborReputation", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("NeighborId")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("Points")
                        .HasColumnType("integer");

                    b.HasKey("PlayerId", "NeighborId");

                    b.HasIndex("NeighborId");

                    b.ToTable("NeighborReputations");
                });

            modelBuilder.Entity("Domiki.Web.Data.Order", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("ExpireDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("NeighborId")
                        .HasColumnType("integer");

                    b.Property<int>("PlayerId")
                        .HasColumnType("integer");

                    b.Property<int>("RewardCoins")
                        .HasColumnType("integer");

                    b.Property<int>("RewardGold")
                        .HasColumnType("integer");

                    b.Property<int>("RewardReputation")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("NeighborId");

                    b.HasIndex("PlayerId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("Domiki.Web.Data.OrderResource", b =>
                {
                    b.Property<int>("OrderId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("ResourceTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.HasKey("OrderId", "ResourceTypeId");

                    b.HasIndex("ResourceTypeId");

                    b.ToTable("OrderResources");
                });

            modelBuilder.Entity("Domiki.Web.Data.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AspNetUserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("character varying(450)");

                    b.Property<int>("CrestColor")
                        .HasColumnType("integer");

                    b.Property<int>("CrestIcon")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<string>("VillageName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.HasIndex("AspNetUserId")
                        .IsUnique();

                    b.HasIndex("VillageName")
                        .IsUnique()
                        .HasFilter("\"VillageName\" IS NOT NULL");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("Domiki.Web.Data.PlayerBlueprint", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("BlueprintId")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.HasKey("PlayerId", "BlueprintId");

                    b.HasIndex("BlueprintId");

                    b.ToTable("PlayerBlueprints");
                });

            modelBuilder.Entity("Domiki.Web.Data.Receipt", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DurationSeconds")
                        .HasColumnType("integer");

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("PlodderCount")
                        .HasColumnType("integer");

                    b.Property<int>("SpeedupPercent")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Receipts");
                });

            modelBuilder.Entity("Domiki.Web.Data.ReceiptResource", b =>
                {
                    b.Property<int>("ReceiptId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("ResourceTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<bool>("IsInput")
                        .HasColumnType("boolean")
                        .HasColumnOrder(3);

                    b.Property<bool>("IsOptional")
                        .HasColumnType("boolean");

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.HasKey("ReceiptId", "ResourceTypeId", "IsInput");

                    b.HasIndex("ResourceTypeId");

                    b.ToTable("ReceiptResources");
                });

            modelBuilder.Entity("Domiki.Web.Data.Resource", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("TypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.HasKey("PlayerId", "TypeId");

                    b.ToTable("Resources");
                });

            modelBuilder.Entity("Domiki.Web.Data.ResourceType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ResourceTypes");
                });

            modelBuilder.Entity("Domiki.Web.Data.Trait", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DurationPercent")
                        .HasColumnType("integer");

                    b.Property<string>("LogicName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("NoFatigue")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("Traits");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            DurationPercent = 0,
                            LogicName = "ordinary",
                            Name = "Обычный",
                            NoFatigue = false
                        },
                        new
                        {
                            Id = 2,
                            DurationPercent = -10,
                            LogicName = "nimble",
                            Name = "Проворный",
                            NoFatigue = false
                        },
                        new
                        {
                            Id = 3,
                            DurationPercent = -20,
                            LogicName = "diligent",
                            Name = "Работящий",
                            NoFatigue = false
                        },
                        new
                        {
                            Id = 4,
                            DurationPercent = 15,
                            LogicName = "sonya",
                            Name = "Соня",
                            NoFatigue = true
                        });
                });

            modelBuilder.Entity("Domiki.Web.Data.WeatherPeriod", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("WeatherTypeId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("WeatherTypeId");

                    b.ToTable("WeatherPeriods");
                });

            modelBuilder.Entity("Domiki.Web.Data.WeatherType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("LogicName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("RotationWeight")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("WeatherTypes");
                });

            modelBuilder.Entity("Domiki.Web.Data.WeatherTypeEffect", b =>
                {
                    b.Property<int>("WeatherTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(1);

                    b.Property<int>("DomikTypeId")
                        .HasColumnType("integer")
                        .HasColumnOrder(2);

                    b.Property<int>("OutputPercent")
                        .HasColumnType("integer");

                    b.HasKey("WeatherTypeId", "DomikTypeId");

                    b.ToTable("WeatherTypeEffects");
                });

            modelBuilder.Entity("Domiki.Web.Data.Worker", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("ManufactureId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("PlayerId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("RestUntil")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("TraitId")
                        .HasColumnType("integer");

                    b.Property<int>("WorkedSeconds")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ManufactureId");

                    b.HasIndex("PlayerId");

                    b.HasIndex("TraitId");

                    b.ToTable("Workers");
                });

            modelBuilder.Entity("Domiki.Web.Data.WorkerSkill", b =>
                {
                    b.Property<int>("WorkerId")
                        .HasColumnType("integer");

                    b.Property<int>("DomikTypeId")
                        .HasColumnType("integer");

                    b.Property<int>("Uses")
                        .HasColumnType("integer");

                    b.HasKey("WorkerId", "DomikTypeId");

                    b.ToTable("WorkerSkills");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .HasColumnType("text");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("Domiki.Web.Data.Blueprint", b =>
                {
                    b.HasOne("Domiki.Web.Data.DomikType", "DomikType")
                        .WithMany()
                        .HasForeignKey("DomikTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.Neighbor", "Neighbor")
                        .WithMany()
                        .HasForeignKey("NeighborId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DomikType");

                    b.Navigation("Neighbor");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevel", b =>
                {
                    b.HasOne("Domiki.Web.Data.DomikType", "DomikType")
                        .WithMany("Levels")
                        .HasForeignKey("DomikTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DomikType");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevelModificator", b =>
                {
                    b.HasOne("Domiki.Web.Data.ModificatorType", "ModificatorType")
                        .WithMany()
                        .HasForeignKey("ModificatorTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.DomikTypeLevel", "DomikTypeLevel")
                        .WithMany()
                        .HasForeignKey("DomikTypeLevelDomikTypeId", "DomikTypeLevelValue")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DomikTypeLevel");

                    b.Navigation("ModificatorType");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevelReceipt", b =>
                {
                    b.HasOne("Domiki.Web.Data.Receipt", "Receipt")
                        .WithMany()
                        .HasForeignKey("ReceiptId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.DomikTypeLevel", "DomikTypeLevel")
                        .WithMany()
                        .HasForeignKey("DomikTypeLevelDomikTypeId", "DomikTypeLevelValue")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DomikTypeLevel");

                    b.Navigation("Receipt");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikTypeLevelResource", b =>
                {
                    b.HasOne("Domiki.Web.Data.ResourceType", "ResourceType")
                        .WithMany()
                        .HasForeignKey("ResourceTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.DomikTypeLevel", "DomikTypeLevel")
                        .WithMany()
                        .HasForeignKey("DomikTypeLevelDomikTypeId", "DomikTypeLevelValue")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DomikTypeLevel");

                    b.Navigation("ResourceType");
                });

            modelBuilder.Entity("Domiki.Web.Data.Manufacture", b =>
                {
                    b.HasOne("Domiki.Web.Data.Domik", "Domik")
                        .WithMany("Manufactures")
                        .HasForeignKey("DomikPlayerId", "DomikId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Domik");
                });

            modelBuilder.Entity("Domiki.Web.Data.NeighborReputation", b =>
                {
                    b.HasOne("Domiki.Web.Data.Neighbor", "Neighbor")
                        .WithMany()
                        .HasForeignKey("NeighborId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Neighbor");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Domiki.Web.Data.Order", b =>
                {
                    b.HasOne("Domiki.Web.Data.Neighbor", "Neighbor")
                        .WithMany()
                        .HasForeignKey("NeighborId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Neighbor");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Domiki.Web.Data.OrderResource", b =>
                {
                    b.HasOne("Domiki.Web.Data.Order", "Order")
                        .WithMany("Resources")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.ResourceType", "ResourceType")
                        .WithMany()
                        .HasForeignKey("ResourceTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Order");

                    b.Navigation("ResourceType");
                });

            modelBuilder.Entity("Domiki.Web.Data.PlayerBlueprint", b =>
                {
                    b.HasOne("Domiki.Web.Data.Blueprint", "Blueprint")
                        .WithMany()
                        .HasForeignKey("BlueprintId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Blueprint");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Domiki.Web.Data.ReceiptResource", b =>
                {
                    b.HasOne("Domiki.Web.Data.Receipt", "Receipt")
                        .WithMany("Resources")
                        .HasForeignKey("ReceiptId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.ResourceType", "ResourceType")
                        .WithMany()
                        .HasForeignKey("ResourceTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Receipt");

                    b.Navigation("ResourceType");
                });

            modelBuilder.Entity("Domiki.Web.Data.Resource", b =>
                {
                    b.HasOne("Domiki.Web.Data.Player", "Player")
                        .WithMany("Resources")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Domiki.Web.Data.WeatherPeriod", b =>
                {
                    b.HasOne("Domiki.Web.Data.WeatherType", "WeatherType")
                        .WithMany()
                        .HasForeignKey("WeatherTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("WeatherType");
                });

            modelBuilder.Entity("Domiki.Web.Data.WeatherTypeEffect", b =>
                {
                    b.HasOne("Domiki.Web.Data.WeatherType", "WeatherType")
                        .WithMany()
                        .HasForeignKey("WeatherTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("WeatherType");
                });

            modelBuilder.Entity("Domiki.Web.Data.Worker", b =>
                {
                    b.HasOne("Domiki.Web.Data.Manufacture", "Manufacture")
                        .WithMany()
                        .HasForeignKey("ManufactureId");

                    b.HasOne("Domiki.Web.Data.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Web.Data.Trait", "Trait")
                        .WithMany()
                        .HasForeignKey("TraitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manufacture");

                    b.Navigation("Player");

                    b.Navigation("Trait");
                });

            modelBuilder.Entity("Domiki.Web.Data.WorkerSkill", b =>
                {
                    b.HasOne("Domiki.Web.Data.Worker", "Worker")
                        .WithMany("Skills")
                        .HasForeignKey("WorkerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Worker");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Domiki.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Domiki.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domiki.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Domiki.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domiki.Web.Data.Domik", b =>
                {
                    b.Navigation("Manufactures");
                });

            modelBuilder.Entity("Domiki.Web.Data.DomikType", b =>
                {
                    b.Navigation("Levels");
                });

            modelBuilder.Entity("Domiki.Web.Data.Order", b =>
                {
                    b.Navigation("Resources");
                });

            modelBuilder.Entity("Domiki.Web.Data.Player", b =>
                {
                    b.Navigation("Resources");
                });

            modelBuilder.Entity("Domiki.Web.Data.Receipt", b =>
                {
                    b.Navigation("Resources");
                });

            modelBuilder.Entity("Domiki.Web.Data.Worker", b =>
                {
                    b.Navigation("Skills");
                });
#pragma warning restore 612, 618
        }
    }
}
