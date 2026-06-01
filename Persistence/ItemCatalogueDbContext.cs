using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Persistence;

// EF Core has a feature called lazy-loading proxies (UseLazyLoadingProxies) that works by generating a subclass of your DbContext/entities at runtime.
// If you ever enabled that, a sealed context would break it.
public sealed class ItemCatalogueDbContext(DbContextOptions<ItemCatalogueDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Person> People { get; set; }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // A value converter alone is not enough for a mutable collection: EF Core needs a ValueComparer
    // to snapshot and compare the value. Without it, the snapshot is the live list reference compared
    // by reference equality, so in-place mutations (e.g. item.ItemTypes.Add(...)) go undetected and
    // are never persisted. This comparer snapshots a copy and compares by element sequence.
    private static readonly ValueComparer<List<ItemType>> _itemTypesComparer = new(
        (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
        v => v == null ? 0 : v.Aggregate(0, (acc, x) => HashCode.Combine(acc, x.GetHashCode())),
        v => v == null ? new List<ItemType>() : v.ToList());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Item>(builder =>
        {
            // We are using Singular naming convention for tables, so we specify "Item" instead of "Items".
            // EF Core will use the DbSet property name as the table name, so since you declared: Items, it will default to "Items".
            // If you want to specify a different table name, you can do so here:
            builder.ToTable("Item");


            //  EF Core will infer IDENTITY(1,1) automatically for an int primary key named Id by convention,
            //  so it won't cause a bug without it. But it's good practice to be explicit
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                  .IsRequired()
                  .UseIdentityColumn(seed: 1, increment: 1);


            builder.Property(e => e.Name)
              .HasColumnType("nvarchar(255)")   // Optional: explicitly set the column type to nvarchar(255). EF Core will infer this from the string property and max length, but being explicit can help avoid issues.
              .IsRequired()
              .HasMaxLength(255);

            builder.Property(e => e.Description)
                  .HasMaxLength(4000);

            builder.Property(e => e.Price)
                  .HasColumnType("decimal(18,2)");

            builder.Property(i => i.IsStored)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(i => i.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.ReasonForDeletion)
              .HasConversion<string>();

            builder.Property(i => i.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(i => i.LastModifiedDate);

            // Foreign key to Location
            builder.HasOne(i => i.Location)
                .WithMany()
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Person (Owner)
            builder.HasOne(i => i.Owner)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Navigation property for ItemTypes (many-to-many or one-to-many relationship)
            // Switching to JsonStringEnumConverter stores names instead (["Electronics","Books"]), making the data resilient to enum reordering and human-readable in the database.
            builder.Property(i => i.ItemTypes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => string.IsNullOrEmpty(v)
                        ? new List<ItemType>()
                        : JsonSerializer.Deserialize<List<ItemType>>(v, _jsonOptions) ?? new List<ItemType>(), _itemTypesComparer)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
        });

        // Configure Room entity
        modelBuilder.Entity<Room>(builder =>
        {
            builder.ToTable("Room");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.Description)
                .HasMaxLength(500);
        });

        // Configure Location entity
        modelBuilder.Entity<Location>(builder =>
        {
            builder.ToTable("Location");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                .ValueGeneratedOnAdd();

            builder.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.Description)
                .HasMaxLength(500);

            // Foreign key to Room (Required)
            builder.HasOne(l => l.Room)
                .WithMany()
                .HasForeignKey(l => l.RoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        // Configure Person entity
        modelBuilder.Entity<Person>(builder =>
        {
            builder.ToTable("Person");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
        });
    }
}
