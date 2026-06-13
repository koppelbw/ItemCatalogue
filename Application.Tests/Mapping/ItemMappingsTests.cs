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
            PurchasePrice: 12.50m,
            CurrentValue: 9.00m,
            Brand: "Acme",
            Model: "L-100",
            SerialNumber: "SN-123",
            PurchasedFrom: "Hardware Store",
            Quantity: 3,
            Condition: Condition.Good,
            AcquisitionType: AcquisitionType.Purchased,
            PurchaseDate: new DateTime(2025, 5, 1),
            WarrantyExpiryDate: new DateTime(2027, 5, 1),
            IsStored: true,
            RoomId: 7,
            ContainerId: null,
            OwnerId: 3);

        var item = request.ToEntity();

        item.Name.ShouldBe("Lamp");
        item.Description.ShouldBe("desc");
        item.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
        item.PurchasePrice.ShouldBe(12.50m);
        item.CurrentValue.ShouldBe(9.00m);
        item.Brand.ShouldBe("Acme");
        item.Model.ShouldBe("L-100");
        item.SerialNumber.ShouldBe("SN-123");
        item.PurchasedFrom.ShouldBe("Hardware Store");
        item.Quantity.ShouldBe(3);
        item.Condition.ShouldBe(Condition.Good);
        item.AcquisitionType.ShouldBe(AcquisitionType.Purchased);
        item.PurchaseDate.ShouldBe(new DateTime(2025, 5, 1));
        item.WarrantyExpiryDate.ShouldBe(new DateTime(2027, 5, 1));
        item.IsStored.ShouldBeTrue();
        item.RoomId.ShouldBe(7);
        item.OwnerId.ShouldBe(3);
    }

    [Fact]
    public void ToEntity_DoesNotSetServerOwnedFields()
    {
        var item = new CreateItemRequest("Lamp", null, [ItemType.Electronics], null, null, null, null, null, null, 1, null, null, null, null, false, null, null, null).ToEntity();

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
            PurchasePrice = 1m,
            CurrentValue = 1m,
            IsStored = false,
            RowVersion = [9, 9, 9],
        };

        var request = new UpdateItemRequest(
            Id: 5,
            Name: "New",
            Description: "new",
            ItemTypes: [ItemType.Electronics],
            PurchasePrice: 99.99m,
            CurrentValue: 80.00m,
            Brand: "Acme",
            Model: "L-200",
            SerialNumber: "SN-999",
            PurchasedFrom: "Online",
            Quantity: 2,
            Condition: Condition.LikeNew,
            AcquisitionType: AcquisitionType.Gift,
            PurchaseDate: new DateTime(2025, 6, 1),
            WarrantyExpiryDate: new DateTime(2026, 6, 1),
            IsStored: true,
            RoomId: 2,
            ContainerId: null,
            OwnerId: 4,
            RowVersion: [1, 2, 3]);

        request.ApplyTo(existing);

        existing.Name.ShouldBe("New");
        existing.Description.ShouldBe("new");
        existing.ItemTypes.ShouldBe([ItemType.Electronics]);
        existing.PurchasePrice.ShouldBe(99.99m);
        existing.CurrentValue.ShouldBe(80.00m);
        existing.Brand.ShouldBe("Acme");
        existing.Model.ShouldBe("L-200");
        existing.SerialNumber.ShouldBe("SN-999");
        existing.PurchasedFrom.ShouldBe("Online");
        existing.Quantity.ShouldBe(2);
        existing.Condition.ShouldBe(Condition.LikeNew);
        existing.AcquisitionType.ShouldBe(AcquisitionType.Gift);
        existing.PurchaseDate.ShouldBe(new DateTime(2025, 6, 1));
        existing.WarrantyExpiryDate.ShouldBe(new DateTime(2026, 6, 1));
        existing.IsStored.ShouldBeTrue();
        existing.RoomId.ShouldBe(2);
        existing.OwnerId.ShouldBe(4);
        existing.RowVersion.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ApplyTo_DoesNotChangeIdentity()
    {
        var existing = new Item { Id = 5, Name = "Old", ItemTypes = [ItemType.Books] };

        new UpdateItemRequest(5, "New", null, [ItemType.Books], null, null, null, null, null, null, 1, null, null, null, null, false, null, null, null, [1])
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
            PurchasePrice = 5m,
            CurrentValue = 3m,
            Brand = "Acme",
            Model = "L-300",
            SerialNumber = "SN-555",
            PurchasedFrom = "Store",
            Quantity = 4,
            Condition = Condition.Fair,
            AcquisitionType = AcquisitionType.Inherited,
            PurchaseDate = new DateTime(2025, 3, 3),
            WarrantyExpiryDate = new DateTime(2026, 3, 3),
            IsStored = true,
            IsDeleted = true,
            ReasonForDeletion = DeletedReason.Broken,
            ContainerId = 3,
            OwnerId = 2,
            CreatedDate = new DateTime(2026, 1, 1),
            LastModifiedDate = new DateTime(2026, 2, 2),
            RowVersion = [4, 5, 6],
        };

        var response = item.ToResponse();

        response.Id.ShouldBe(8);
        response.Name.ShouldBe("Lamp");
        response.PurchasePrice.ShouldBe(5m);
        response.CurrentValue.ShouldBe(3m);
        response.Brand.ShouldBe("Acme");
        response.Model.ShouldBe("L-300");
        response.SerialNumber.ShouldBe("SN-555");
        response.PurchasedFrom.ShouldBe("Store");
        response.Quantity.ShouldBe(4);
        response.Condition.ShouldBe(Condition.Fair);
        response.AcquisitionType.ShouldBe(AcquisitionType.Inherited);
        response.PurchaseDate.ShouldBe(new DateTime(2025, 3, 3));
        response.WarrantyExpiryDate.ShouldBe(new DateTime(2026, 3, 3));
        response.ContainerId.ShouldBe(3);
        response.IsDeleted.ShouldBeTrue();
        response.ReasonForDeletion.ShouldBe(DeletedReason.Broken);
        response.CreatedDate.ShouldBe(new DateTime(2026, 1, 1));
        response.LastModifiedDate.ShouldBe(new DateTime(2026, 2, 2));
        response.RowVersion.ShouldBe([4, 5, 6]);
    }
}
