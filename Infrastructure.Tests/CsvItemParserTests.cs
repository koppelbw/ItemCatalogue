using Domain.Enums;
using FluentValidation;
using Infrastructure.Storage;
using Shouldly;
using System.Text;

namespace Infrastructure.Tests;

// Pure parsing tests — no storage involved. The contract under test: cell-level failures become
// per-row errors keyed by the physical CSV line (header = 1), never exceptions; only whole-file
// problems throw.
public class CsvItemParserTests
{
    private static readonly CsvItemParser _parser = new();

    private static Task<Application.DTOs.CsvParseResult> ParseAsync(params string[] lines)
        => _parser.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines))));

    [Fact]
    public async Task ParseAsync_FullyPopulatedRow_MapsEveryColumn()
    {
        var result = await ParseAsync(
            "Name,Description,ItemTypes,PurchasePrice,CurrentValue,Brand,Model,SerialNumber,PurchasedFrom,Quantity,Condition,AcquisitionType,PurchaseDate,WarrantyExpiryDate,IsStored,IsShownInUI,RoomId,ContainerId,OwnerId,ReleaseDate,ValuationDate,AcquisitionReference",
            "Desk Lamp,LED lamp,Electronics;Books,19.99,12.50,Ikea,Tertial,SN-1,Ikea Store,2,Good,Purchased,2024-01-15,2026-01-15,true,1,7,11,3,2023-06-01,2025-01-01,INV-001");

        result.Errors.ShouldBeEmpty();
        var row = result.Rows.ShouldHaveSingleItem();
        row.RowNumber.ShouldBe(2);
        row.Name.ShouldBe("Desk Lamp");
        row.Description.ShouldBe("LED lamp");
        row.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
        row.PurchasePrice.ShouldBe(19.99m);
        row.CurrentValue.ShouldBe(12.50m);
        row.Brand.ShouldBe("Ikea");
        row.Model.ShouldBe("Tertial");
        row.SerialNumber.ShouldBe("SN-1");
        row.PurchasedFrom.ShouldBe("Ikea Store");
        row.Quantity.ShouldBe(2);
        row.Condition.ShouldBe(Condition.Good);
        row.AcquisitionType.ShouldBe(AcquisitionType.Purchased);
        row.PurchaseDate.ShouldBe(new DateTime(2024, 1, 15));
        row.WarrantyExpiryDate.ShouldBe(new DateTime(2026, 1, 15));
        row.IsStored.ShouldBeTrue();
        row.IsShownInUI.ShouldBeTrue();
        row.RoomId.ShouldBe(7);
        row.ContainerId.ShouldBe(11);
        row.OwnerId.ShouldBe(3);
        row.ReleaseDate.ShouldBe(new DateTime(2023, 6, 1));
        row.ValuationDate.ShouldBe(new DateTime(2025, 1, 1));
        row.AcquisitionReference.ShouldBe("INV-001");
    }

    [Fact]
    public async Task ParseAsync_MinimalHeader_AppliesDefaults()
    {
        var result = await ParseAsync("Name,ItemTypes", "Drill,Electronics");

        var row = result.Rows.ShouldHaveSingleItem();
        row.Quantity.ShouldBe(1);
        row.IsStored.ShouldBeFalse();
        row.IsShownInUI.ShouldBeFalse();
        row.Description.ShouldBeNull();
        row.PurchasePrice.ShouldBeNull();
        row.RoomId.ShouldBeNull();
    }

    [Fact]
    public async Task ParseAsync_BlankCells_BecomeNulls()
    {
        var result = await ParseAsync("Name,Description,PurchasePrice,RoomId", "Drill, , ,  ");

        var row = result.Rows.ShouldHaveSingleItem();
        row.Description.ShouldBeNull();
        row.PurchasePrice.ShouldBeNull();
        row.RoomId.ShouldBeNull();
    }

    [Fact]
    public async Task ParseAsync_NonNumericReferenceId_BecomesRowError()
    {
        var result = await ParseAsync("Name,ItemTypes,RoomId", "Drill,Electronics,Garage");

        result.Rows.ShouldBeEmpty();
        var error = result.Errors.ShouldHaveSingleItem();
        error.RowNumber.ShouldBe(2);
        error.Messages.ShouldHaveSingleItem().ShouldContain("not a valid RoomId");
    }

    [Fact]
    public async Task ParseAsync_BadCells_BecomeRowErrors_AndGoodRowsSurvive()
    {
        var result = await ParseAsync(
            "Name,ItemTypes,PurchasePrice,PurchaseDate",
            "Good Row,Electronics,10.00,2024-01-01",
            "Bad Row,Electronics,not-a-price,not-a-date",
            "Another Good,Books,5,2024-02-02");

        result.Rows.Count.ShouldBe(2);
        var error = result.Errors.ShouldHaveSingleItem();
        error.RowNumber.ShouldBe(3);
        error.Messages.Count.ShouldBe(2);
        error.Messages[0].ShouldContain("not-a-price");
        error.Messages[1].ShouldContain("not-a-date");
    }

    [Fact]
    public async Task ParseAsync_UnknownEnumValue_ErrorListsValidValues()
    {
        var result = await ParseAsync("Name,Condition", "Drill,Shiny");

        var error = result.Errors.ShouldHaveSingleItem();
        error.Messages.ShouldHaveSingleItem()
            .ShouldBe("'Shiny' is not a valid Condition. Valid values: New, LikeNew, Good, Fair, Poor, ForRepair, Broken.");
    }

    [Fact]
    public async Task ParseAsync_OutOfRangeNumericEnum_IsRejected()
    {
        // Enum.TryParse would happily accept "999"; the parser must not.
        var result = await ParseAsync("Name,AcquisitionType", "Drill,999");

        result.Errors.ShouldHaveSingleItem().Messages.ShouldHaveSingleItem().ShouldContain("not a valid AcquisitionType");
    }

    [Fact]
    public async Task ParseAsync_ItemTypes_AreCaseInsensitiveAndSemicolonDelimited()
    {
        var result = await ParseAsync("Name,ItemTypes", "Drill,electronics; BOOKS");

        result.Rows.ShouldHaveSingleItem().ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_ThrowsValidation()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() => ParseAsync());
        ex.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("The file is empty or has no header row.");
    }

    [Fact]
    public async Task ParseAsync_MissingNameColumn_ThrowsValidation()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() => ParseAsync("Description,PurchasePrice", "just a desc,5"));
        ex.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("The file is missing the required 'Name' column.");
    }

    [Fact]
    public async Task ParseAsync_HeaderOnly_ReturnsNoRowsAndNoErrors()
    {
        var result = await ParseAsync("Name,ItemTypes");

        result.Rows.ShouldBeEmpty();
        result.Errors.ShouldBeEmpty();
    }
}
