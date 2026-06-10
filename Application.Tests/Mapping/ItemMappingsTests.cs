using Application.DTOs;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using Shouldly;

namespace Application.Tests.Mapping;

public class ItemMappingsTests
{
    [Fact]
    public void ToEntity_CopiesWritableFields()
    {
        var request = new CreateItemRequest(
            Name: "Lamp",
            Description: "desc",
            ItemTypes: [ItemType.Electronics, ItemType.Books],
            Price: 12.50m,
            IsStored: true,
            LocationId: 7,
            OwnerId: 3);

        var item = request.ToEntity();

        item.Name.ShouldBe("Lamp");
        item.Description.ShouldBe("desc");
        item.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
        item.Price.ShouldBe(12.50m);
        item.IsStored.ShouldBeTrue();
        item.LocationId.ShouldBe(7);
        item.OwnerId.ShouldBe(3);
    }

    [Fact]
    public void ToEntity_DoesNotSetServerOwnedFields()
    {
        var item = new CreateItemRequest("Lamp", null, [ItemType.Electronics], null, false, null, null).ToEntity();

        item.Id.ShouldBe(0);
        item.IsDeleted.ShouldBeFalse();
        item.ReasonForDeletion.ShouldBeNull();
    }

    [Fact]
    public void ApplyTo_OverwritesEditableFieldsAndCarriesRowVersion()
    {
        var existing = new Item
        {
            Id = 5,
            Name = "Old",
            Description = "old",
            ItemTypes = [ItemType.Books],
            Price = 1m,
            IsStored = false,
            RowVersion = [9, 9, 9],
        };

        var request = new UpdateItemRequest(
            Id: 5,
            Name: "New",
            Description: "new",
            ItemTypes: [ItemType.Electronics],
            Price: 99.99m,
            IsStored: true,
            LocationId: 2,
            OwnerId: 4,
            RowVersion: [1, 2, 3]);

        request.ApplyTo(existing);

        existing.Name.ShouldBe("New");
        existing.Description.ShouldBe("new");
        existing.ItemTypes.ShouldBe([ItemType.Electronics]);
        existing.Price.ShouldBe(99.99m);
        existing.IsStored.ShouldBeTrue();
        existing.LocationId.ShouldBe(2);
        existing.OwnerId.ShouldBe(4);
        existing.RowVersion.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ApplyTo_DoesNotChangeIdentity()
    {
        var existing = new Item { Id = 5, Name = "Old", ItemTypes = [ItemType.Books] };

        new UpdateItemRequest(5, "New", null, [ItemType.Books], null, false, null, null, [1])
            .ApplyTo(existing);

        existing.Id.ShouldBe(5);
    }

    [Fact]
    public void ToResponse_ProjectsAllFields()
    {
        var item = new Item
        {
            Id = 8,
            Name = "Lamp",
            Description = "desc",
            ItemTypes = [ItemType.Electronics],
            Price = 5m,
            IsStored = true,
            IsDeleted = true,
            ReasonForDeletion = DeletedReason.Broken,
            LocationId = 1,
            OwnerId = 2,
            CreatedDate = new DateTime(2026, 1, 1),
            LastModifiedDate = new DateTime(2026, 2, 2),
            RowVersion = [4, 5, 6],
        };

        var response = item.ToResponse();

        response.Id.ShouldBe(8);
        response.Name.ShouldBe("Lamp");
        response.IsDeleted.ShouldBeTrue();
        response.ReasonForDeletion.ShouldBe(DeletedReason.Broken);
        response.CreatedDate.ShouldBe(new DateTime(2026, 1, 1));
        response.LastModifiedDate.ShouldBe(new DateTime(2026, 2, 2));
        response.RowVersion.ShouldBe([4, 5, 6]);
    }
}
