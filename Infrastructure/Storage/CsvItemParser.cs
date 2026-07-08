using Application.DTOs;
using Application.StoragePorts;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.Enums;
using FluentValidation.Results;
using System.Globalization;

// CsvHelper declares its own ValidationException; the API's ValidationExceptionHandler catches FluentValidation's, so disambiguate explicitly.
using ValidationException = FluentValidation.ValidationException;

namespace Infrastructure.Storage;

public sealed class CsvItemParser : ICsvItemParser
{
    private static readonly CsvConfiguration _configuration = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
        TrimOptions = TrimOptions.Trim,
    };

    public async Task<CsvParseResult> ParseAsync(Stream csv, CancellationToken cancellationToken = default)
    {
        var rows = new List<CsvItemRow>();
        var errors = new List<ImportRowError>();

        using var reader = new StreamReader(csv, leaveOpen: true);
        using var csvReader = new CsvReader(reader, _configuration);

        if (!await csvReader.ReadAsync() || !csvReader.ReadHeader())
        {
            throw new ValidationException([new ValidationFailure("File", "The file is empty or has no header row.")]);
        }
        if (!csvReader.HeaderRecord!.Contains("Name", StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException([new ValidationFailure("File", "The file is missing the required 'Name' column.")]);
        }

        while (await csvReader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Physical line number (header = 1), so errors point at the user's spreadsheet row.
            var rowNumber = csvReader.Parser.RawRow;
            var rowErrors = new List<string>();

            var row = new CsvItemRow(
                RowNumber: rowNumber,
                Name: Text(csvReader, "Name") ?? string.Empty,
                Description: Text(csvReader, "Description"),
                ItemTypes: ParseItemTypes(csvReader, rowErrors),
                PurchasePrice: ParseDecimal(csvReader, "PurchasePrice", rowErrors),
                CurrentValue: ParseDecimal(csvReader, "CurrentValue", rowErrors),
                Brand: Text(csvReader, "Brand"),
                Model: Text(csvReader, "Model"),
                SerialNumber: Text(csvReader, "SerialNumber"),
                PurchasedFrom: Text(csvReader, "PurchasedFrom"),
                Quantity: ParseInt(csvReader, "Quantity", rowErrors) ?? 1,
                Condition: ParseEnum<Condition>(csvReader, "Condition", rowErrors),
                AcquisitionType: ParseEnum<AcquisitionType>(csvReader, "AcquisitionType", rowErrors),
                PurchaseDate: ParseDate(csvReader, "PurchaseDate", rowErrors),
                WarrantyExpiryDate: ParseDate(csvReader, "WarrantyExpiryDate", rowErrors),
                IsStored: ParseBool(csvReader, "IsStored", defaultValue: false, rowErrors),
                IsShownInUI: ParseBool(csvReader, "IsShownInUI", defaultValue: false, rowErrors),
                // References are given by database id; a non-numeric cell becomes a per-row error
                // via ParseInt, and a numeric-but-nonexistent id is caught at chunk time by
                // ItemBulkPreparer's FK check.
                RoomId: ParseInt(csvReader, "RoomId", rowErrors),
                ContainerId: ParseInt(csvReader, "ContainerId", rowErrors),
                OwnerId: ParseInt(csvReader, "OwnerId", rowErrors),
                ReleaseDate: ParseDate(csvReader, "ReleaseDate", rowErrors),
                ValuationDate: ParseDate(csvReader, "ValuationDate", rowErrors),
                AcquisitionReference: Text(csvReader, "AcquisitionReference"));

            if (rowErrors.Count > 0)
            {
                errors.Add(new ImportRowError(rowNumber, rowErrors));
            }
            else
            {
                rows.Add(row);
            }
        }

        return new CsvParseResult(rows, errors);
    }

    // Blank cells (and absent optional columns) normalize to null.
    private static string? Text(CsvReader csv, string column)
    {
        var cell = csv.GetField(column);
        return string.IsNullOrWhiteSpace(cell) ? null : cell.Trim();
    }

    private static decimal? ParseDecimal(CsvReader csv, string column, List<string> errors)
    {
        var cell = Text(csv, column);
        if (cell is null) return null;
        if (decimal.TryParse(cell, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) return value;
        errors.Add($"'{cell}' is not a valid {column}; use a plain number like 19.99.");
        return null;
    }

    private static int? ParseInt(CsvReader csv, string column, List<string> errors)
    {
        var cell = Text(csv, column);
        if (cell is null) return null;
        if (int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return value;
        errors.Add($"'{cell}' is not a valid {column}; use a whole number.");
        return null;
    }

    private static DateTime? ParseDate(CsvReader csv, string column, List<string> errors)
    {
        var cell = Text(csv, column);
        if (cell is null) return null;
        if (DateTime.TryParse(cell, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)) return value;
        errors.Add($"'{cell}' is not a valid {column}; use yyyy-MM-dd.");
        return null;
    }

    private static bool ParseBool(CsvReader csv, string column, bool defaultValue, List<string> errors)
    {
        var cell = Text(csv, column);
        if (cell is null) return defaultValue;
        if (bool.TryParse(cell, out var value)) return value;
        if (cell == "1") return true;
        if (cell == "0") return false;
        errors.Add($"'{cell}' is not a valid {column}; use true or false.");
        return defaultValue;
    }

    // Enum.TryParse alone would accept out-of-range numerics ("999"); IsDefined guards that.
    private static TEnum? ParseEnum<TEnum>(CsvReader csv, string column, List<string> errors)
        where TEnum : struct, Enum
    {
        var cell = Text(csv, column);
        if (cell is null) return null;
        if (Enum.TryParse<TEnum>(cell, ignoreCase: true, out var value) && Enum.IsDefined(value)) return value;
        errors.Add($"'{cell}' is not a valid {column}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        return null;
    }

    // Multi-valued: "Electronics;Books". An empty list is legal here — the required-non-empty rule
    // is business validation, enforced per chunk by CreateItemRequestValidator.
    private static List<ItemType> ParseItemTypes(CsvReader csv, List<string> errors)
    {
        var cell = Text(csv, "ItemTypes");
        if (cell is null) return [];

        var types = new List<ItemType>();
        foreach (var part in cell.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<ItemType>(part, ignoreCase: true, out var type) && Enum.IsDefined(type))
            {
                types.Add(type);
            }
            else
            {
                errors.Add($"'{part}' is not a valid ItemType. Valid values: {string.Join(", ", Enum.GetNames<ItemType>())}.");
            }
        }

        return types;
    }
}
