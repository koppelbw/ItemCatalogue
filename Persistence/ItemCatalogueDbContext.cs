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
    public DbSet<ItemEvent> ItemEvents { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Container> Containers { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<ItemTag> ItemTags { get; set; }
    public DbSet<CollectionItem> CollectionItems { get; set; }

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
                  .HasColumnType("nvarchar(max)");

            builder.Property(e => e.PurchasePrice)
                  .HasColumnType("decimal(18,2)");

            builder.Property(e => e.CurrentValue)
                  .HasColumnType("decimal(18,2)");

            builder.Property(e => e.Brand)
                  .HasMaxLength(100);

            builder.Property(e => e.Model)
                  .HasMaxLength(100);

            builder.Property(e => e.SerialNumber)
                  .HasMaxLength(100);

            builder.Property(e => e.PurchasedFrom)
                  .HasMaxLength(150);

            builder.Property(e => e.AcquisitionReference)
                  .HasMaxLength(100);

            builder.Property(i => i.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(e => e.Condition)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(e => e.AcquisitionType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasIndex(i => i.SerialNumber);

            builder.Property(i => i.IsStored)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(i => i.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.ReasonForDeletion)
              .HasConversion<string>()
              .HasMaxLength(255);


            // Maps RowVersion to a SQL Server rowversion column and registers it as a
            // concurrency token, so every UPDATE/DELETE carries "AND RowVersion = @original".
            builder.Property(i => i.RowVersion)
                .IsRowVersion();

            // Foreign key to Room. An item's location is derived through its room.
            builder.HasOne(i => i.Room)
                .WithMany()
                .HasForeignKey(i => i.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Container (an item may live inside a container instead of directly in a room).
            builder.HasOne(i => i.Container)
                .WithMany()
                .HasForeignKey(i => i.ContainerId)
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
                // Mirror the SSDT DEFAULT '[]' so the model knows the column is store-generated on
                // insert (matches the database for out-of-app writes; SchemaDriftTests enforces this).
                .HasDefaultValueSql("'[]'")
                .IsRequired();
        });

        modelBuilder.Entity<ItemEvent>(builder =>
        {
            builder.ToTable("ItemEvent");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.EventType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.OccurredAt)
                .IsRequired();

            builder.Property(e => e.OldValue)
                .HasMaxLength(500);

            builder.Property(e => e.NewValue)
                .HasMaxLength(500);

            builder.Property(e => e.Notes)
                .HasMaxLength(500);

            builder.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
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

            // Foreign key to the owning Location (Required). Restrict so a Location that still
            // has Rooms cannot be deleted; SQL Server error 547 surfaces as EntityInUseException.
            builder.HasOne(r => r.Location)
                .WithMany(l => l.Rooms)
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.Property(r => r.RowVersion)
                .IsRowVersion();
        });

        // Configure Container entity
        modelBuilder.Entity<Container>(builder =>
        {
            builder.ToTable("Container");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            // A top-level container is owned by a Room. Restrict so a Room that still has containers
            // cannot be deleted; SQL Server error 547 surfaces as EntityInUseException. Nullable
            // because a nested container references a parent container instead (see below).
            builder.HasOne(c => c.Room)
                .WithMany(r => r.Containers)
                .HasForeignKey(c => c.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-reference: a nested container is owned by a parent container. Restrict (SQL Server
            // forbids cascade on a self-referencing FK), so a parent with children cannot be deleted.
            builder.HasOne(c => c.ParentContainer)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentContainerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(c => c.RowVersion)
                .IsRowVersion();
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

            // The Location -> Rooms one-to-many is configured from the Room side (Room.LocationId).

            builder.Property(l => l.RowVersion)
                .IsRowVersion();
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

            builder.Property(p => p.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<Tag>(builder =>
        {
            builder.ToTable("Tag");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.HasIndex(t => t.Name)
                .IsUnique();

            builder.Property(t => t.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<Collection>(builder =>
        {
            builder.ToTable("Collection");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.HasIndex(c => c.Name)
                .IsUnique();

            builder.Property(c => c.RowVersion)
                .IsRowVersion();
        });

        // Item <-> Tag: a plain many-to-many through the ItemTag join entity. The skip-navigations
        // (Item.Tags / Tag.Items) give a clean read surface; both FKs cascade so deleting an Item or a
        // Tag removes its join rows. ItemId leads the composite key, so EF emits a single index on
        // TagId — both mirrored by Database/dbo/tables/ItemTag.sql.
        modelBuilder.Entity<Item>()
            .HasMany(i => i.Tags)
            .WithMany(t => t.Items)
            .UsingEntity<ItemTag>(
                right => right
                    .HasOne(it => it.Tag)
                    .WithMany()
                    .HasForeignKey(it => it.TagId)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne(it => it.Item)
                    .WithMany()
                    .HasForeignKey(it => it.ItemId)
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("ItemTag");
                    join.HasKey(it => new { it.ItemId, it.TagId });
                });

        // Collection <-> Item: a *rich* many-to-many modelled explicitly (not a skip-navigation)
        // because the join carries payload. CollectionItem is a dependent of both Collection and Item;
        // both FKs cascade. Quantity/SortOrder defaults mirror Database/dbo/tables/CollectionItem.sql.
        modelBuilder.Entity<CollectionItem>(builder =>
        {
            builder.ToTable("CollectionItem");

            builder.HasKey(ci => new { ci.CollectionId, ci.ItemId });

            builder.Property(ci => ci.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(ci => ci.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(ci => ci.Role)
                .HasMaxLength(100);

            builder.HasOne(ci => ci.Collection)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ci => ci.Item)
                .WithMany(i => i.CollectionMemberships)
                .HasForeignKey(ci => ci.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Audit columns are uniform across every IAuditable entity, so configure them once here
        // rather than repeating the mapping in each entity block. CreatedDate keeps a GETUTCDATE()
        // default as a fallback for out-of-app inserts (e.g. post-deployment seed scripts); for
        // app writes the AuditingSaveChangesInterceptor supplies an explicit value that wins.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IAuditable).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IAuditable.CreatedDate))
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
