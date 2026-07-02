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
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Container> Containers { get; set; }
    public DbSet<Door> Doors { get; set; }
    public DbSet<Stair> Stairs { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<ItemTag> ItemTags { get; set; }
    public DbSet<CollectionItem> CollectionItems { get; set; }
    public DbSet<Picture> Pictures { get; set; }

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
            builder.ToTable("Item");

                       
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                  .IsRequired()
                  .UseIdentityColumn(seed: 1, increment: 1);


            builder.Property(e => e.Name)
              .HasColumnType("nvarchar(255)")
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


            builder.Property(i => i.RowVersion)
                .IsRowVersion();

            builder.HasOne(i => i.Room)
                .WithMany()
                .HasForeignKey(i => i.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
                       
            builder.HasOne(i => i.Container)
                .WithMany()
                .HasForeignKey(i => i.ContainerId)
                .OnDelete(DeleteBehavior.SetNull);

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

            builder.Property(r => r.RoomType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(r => r.OriginXInches).HasColumnType("decimal(9,2)");
            builder.Property(r => r.OriginYInches).HasColumnType("decimal(9,2)");
            builder.Property(r => r.WidthInches).HasColumnType("decimal(9,2)");
            builder.Property(r => r.DepthInches).HasColumnType("decimal(9,2)");
            builder.Property(r => r.HeightInches).HasColumnType("decimal(9,2)");
            builder.Property(r => r.Rotation).HasColumnType("decimal(6,2)");

            builder.Property(r => r.WallColor).HasMaxLength(9);
            builder.Property(r => r.FloorColor).HasMaxLength(9);
            builder.Property(r => r.CeilingColor).HasMaxLength(9);
                        
            builder.HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
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

            builder.Property(c => c.ContainerType)
                .HasConversion<string>()
                .HasMaxLength(50);
                        
            builder.Property(c => c.PositionXInches).HasColumnType("decimal(9,2)");
            builder.Property(c => c.PositionYInches).HasColumnType("decimal(9,2)");
            builder.Property(c => c.PositionZInches).HasColumnType("decimal(9,2)");
            builder.Property(c => c.Rotation).HasColumnType("decimal(6,2)");
            builder.Property(c => c.WidthInches).HasColumnType("decimal(9,2)");
            builder.Property(c => c.DepthInches).HasColumnType("decimal(9,2)");
            builder.Property(c => c.HeightInches).HasColumnType("decimal(9,2)");

            builder.Property(c => c.Color).HasMaxLength(9);

            builder.HasOne(c => c.Room)
                .WithMany(r => r.Containers)
                .HasForeignKey(c => c.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.ParentContainer)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentContainerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(c => c.RowVersion)
                .IsRowVersion();
        });

        // Configure Floor entity
        modelBuilder.Entity<Floor>(builder =>
        {
            builder.ToTable("Floor");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Id)
                .ValueGeneratedOnAdd();

            builder.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(f => f.ElevationInches).HasColumnType("decimal(9,2)");
            builder.Property(f => f.CeilingHeightInches).HasColumnType("decimal(9,2)");
                        
            builder.HasOne(f => f.Location)
                .WithMany(l => l.Floors)
                .HasForeignKey(f => f.LocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasIndex(f => new { f.LocationId, f.LevelIndex })
                .IsUnique();

            builder.Property(f => f.RowVersion)
                .IsRowVersion();
        });

        // Configure Door entity
        modelBuilder.Entity<Door>(builder =>
        {
            builder.ToTable("Door");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                .ValueGeneratedOnAdd();

            builder.Property(d => d.Name)
                .HasMaxLength(100);

            builder.Property(d => d.Kind)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(d => d.Wall)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(d => d.OffsetInches).HasColumnType("decimal(9,2)");
            builder.Property(d => d.WidthInches).HasColumnType("decimal(9,2)");
            builder.Property(d => d.HeightInches).HasColumnType("decimal(9,2)");

            builder.Property(d => d.HingeSide)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(d => d.Swing)
                .HasConversion<string>()
                .HasMaxLength(50);
                        
            builder.HasOne(d => d.FromRoom)
                .WithMany(r => r.Doors)
                .HasForeignKey(d => d.FromRoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(d => d.ToRoom)
                .WithMany()
                .HasForeignKey(d => d.ToRoomId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(d => d.RowVersion)
                .IsRowVersion();
        });

        // Configure Stair entity
        modelBuilder.Entity<Stair>(builder =>
        {
            builder.ToTable("Stair");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .ValueGeneratedOnAdd();

            builder.Property(s => s.Name)
                .HasMaxLength(100);

            builder.Property(s => s.Shape)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.PositionXInches).HasColumnType("decimal(9,2)");
            builder.Property(s => s.PositionYInches).HasColumnType("decimal(9,2)");
            builder.Property(s => s.Rotation).HasColumnType("decimal(6,2)");
            builder.Property(s => s.RunInches).HasColumnType("decimal(9,2)");
            builder.Property(s => s.WidthInches).HasColumnType("decimal(9,2)");
            builder.Property(s => s.RiseInches).HasColumnType("decimal(9,2)");

            // The lower room the stair sits in. Restrict so a Room with stairs in it cannot be deleted.
            builder.HasOne(s => s.FromRoom)
                .WithMany(r => r.Stairs)
                .HasForeignKey(s => s.FromRoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // The upper room (NULL = leads to an exterior level). SetNull + Restrict (above) keep the
            // two FKs to Room free of a multiple-cascade-path.
            builder.HasOne(s => s.ToRoom)
                .WithMany()
                .HasForeignKey(s => s.ToRoomId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(s => s.RowVersion)
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

        modelBuilder.Entity<Picture>(builder =>
        {
            builder.ToTable("Picture");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.BlobName)
                .IsRequired()
                .HasMaxLength(400);

            builder.Property(p => p.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.SizeBytes)
                .IsRequired();

            builder.Property(p => p.OriginalFileName)
                .HasMaxLength(255);

            builder.Property(p => p.Caption)
                .HasMaxLength(500);

            builder.Property(p => p.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasOne(p => p.Location)
                .WithMany()
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Room)
                .WithMany()
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Container)
                .WithMany()
                .HasForeignKey(p => p.ContainerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Item)
                .WithMany()
                .HasForeignKey(p => p.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.RowVersion)
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
