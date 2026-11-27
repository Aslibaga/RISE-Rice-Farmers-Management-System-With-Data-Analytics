using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Models;
using RISE.API.Services;

namespace RISE.API.Controllers;

[ApiController]
[Authorize(Policy = "AnyAuthenticated")]
[Route("api/excel")]
public sealed class ExcelController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] FarmerHeaders =
    {
        "Last Name", "First Name", "Middle Name", "Full Name",
        "Contact Number", "Barangay", "Municipality", "Province"
    };

    private static readonly string[] SupplyHeaders =
    {
        "Supply Name", "Category Name", "Unit of Measure",
        "Unit Cost", "Stock Quantity", "Expiration Date"
    };

    private int OwnerId => OwnerScope.UserId(User);

    private readonly RiseDbContext _context;

    public ExcelController(RiseDbContext context) => _context = context;

    private FileResult Xlsx(byte[] bytes, string fileName) => File(bytes, ExcelContentType, fileName);

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static string? Get(IReadOnlyDictionary<string, string> row, params string[] names)
    {
        foreach (var name in names)
            if (row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        return null;
    }

    private static bool TryDecimal(string? value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value)) return true;
        return decimal.TryParse(value.Replace(",", ""), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    private static bool TryDate(string? value, out DateOnly result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result)) return true;
        if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dateTime))
        {
            result = DateOnly.FromDateTime(dateTime);
            return true;
        }
        return false;
    }

    private static string StockStatus(decimal quantity) => quantity <= 0 ? "Out of stock" : "In stock";

    // ------------------------------------------------------------------
    // Farmer records
    // ------------------------------------------------------------------

    [HttpGet("farmers/template")]
    public IActionResult DownloadFarmerTemplate()
    {
        var columns = FarmerHeaders.Select(header => new XlsxService.Column { Header = header, Width = header.Length > 14 ? 24 : 18 }).ToList();
        var sheet = new XlsxService.Sheet
        {
            Name = "Farmer Records",
            Columns = columns,
            Rows = Array.Empty<object?[]>(),
            Summary = "RISE import template · Last Name and Contact Number are required."
        };
        var instructions = new XlsxService.Sheet
        {
            Name = "Instructions",
            Columns = new[] { new XlsxService.Column { Header = "Field", Width = 24 }, new XlsxService.Column { Header = "Rule", Width = 90 } },
            Rows = new object?[][]
            {
                new object?[] { "Last Name", "Required, maximum 60 characters." },
                new object?[] { "Contact Number", "Required, minimum 7 and maximum 30 characters." },
                new object?[] { "Full Name", "Optional; generated from the name fields when blank." },
                new object?[] { "Duplicate check", "Active farmers with the same full name and contact number are skipped." }
            }
        };
        return Xlsx(XlsxService.Write(new[] { sheet, instructions }, "RISE Farmer Import Template", "Farmer Records"), "Farmer_Import_Template.xlsx");
    }

    [HttpGet("farmers/export")]
    public async Task<IActionResult> ExportFarmers([FromQuery] string? search, [FromQuery] string? sort, CancellationToken cancellationToken)
    {
        var query = _context.Farmers.AsNoTracking().Where(farmer => farmer.IsActive && farmer.OwnerUserId == OwnerId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(farmer =>
                farmer.FullName.Contains(search) ||
                farmer.FarmerId.ToString() == search ||
                (farmer.Barangay != null && farmer.Barangay.Contains(search)) ||
                (farmer.Municipality != null && farmer.Municipality.Contains(search)));
        }
        query = sort switch
        {
            "newest" => query.OrderByDescending(farmer => farmer.FarmerId),
            "oldest" => query.OrderBy(farmer => farmer.FarmerId),
            _ => query.OrderBy(farmer => farmer.FullName)
        };
        var farmers = await query.Select(farmer => new
        {
            farmer.FullName, farmer.FirstName, farmer.LastName, farmer.MiddleName,
            farmer.ContactNumber, farmer.Barangay, farmer.Municipality, farmer.Province,
            farmer.IsActive
        }).ToListAsync(cancellationToken);

        var columns = new[]
        {
            new XlsxService.Column { Header = "No.", Kind = XlsxService.CellKind.Integer, Width = 8 },
            new XlsxService.Column { Header = "Last Name", Width = 18 },
            new XlsxService.Column { Header = "First Name", Width = 18 },
            new XlsxService.Column { Header = "Middle Name", Width = 18 },
            new XlsxService.Column { Header = "Full Name", Width = 28 },
            new XlsxService.Column { Header = "Display Name", Width = 28 },
            new XlsxService.Column { Header = "Contact Number", Width = 18 },
            new XlsxService.Column { Header = "Barangay", Width = 20 },
            new XlsxService.Column { Header = "Municipality", Width = 20 },
            new XlsxService.Column { Header = "Province", Width = 20 },
            new XlsxService.Column { Header = "Status", Width = 12 }
        };
        var rows = farmers.Select((farmer, index) => new object?[]
        {
            index + 1, farmer.LastName, farmer.FirstName, farmer.MiddleName, farmer.FullName,
            Farmer.BuildDisplayName(farmer.FirstName, farmer.LastName, farmer.MiddleName, farmer.FullName),
            farmer.ContactNumber, farmer.Barangay, farmer.Municipality, farmer.Province,
            farmer.IsActive ? "Active" : "Archived"
        }).ToArray();

        var sheets = new[]
        {
            new XlsxService.Sheet { Name = "Farmer Records", Columns = columns, Rows = rows }
        };
        return Xlsx(XlsxService.Write(sheets, "RISE Farmer Records"), "Farmer_Records.xlsx");
    }

    [HttpPost("farmers/preview")]
    public async Task<IActionResult> PreviewFarmers([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var parsed = await ParseRows(file, "Farmer Records", cancellationToken);
        if (parsed.Error is not null) return BadRequest(parsed.Error);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<object>();
        var validCount = 0;
        var invalidCount = 0;
        var duplicateCount = 0;
        foreach (var (row, index) in parsed.Rows.Select((row, index) => (row, index)))
        {
            var errors = new List<string>();
            var last = Clean(Get(row, "Last Name", "LastName"));
            var first = Clean(Get(row, "First Name", "FirstName"));
            var middle = Clean(Get(row, "Middle Name", "MiddleName"));
            var suppliedFull = Clean(Get(row, "Full Name", "FullName"));
            var contact = Clean(Get(row, "Contact Number", "ContactNumber"));
            var barangay = Clean(Get(row, "Barangay"));
            var municipality = Clean(Get(row, "Municipality"));
            var province = Clean(Get(row, "Province"));

            if (last.Length == 0) errors.Add("Last Name is required.");
            else if (last.Length > 60) errors.Add("Last Name exceeds 60 characters.");
            if (first.Length > 60) errors.Add("First Name exceeds 60 characters.");
            if (middle.Length > 60) errors.Add("Middle Name exceeds 60 characters.");
            if (suppliedFull.Length > 150) errors.Add("Full Name exceeds 150 characters.");
            if (contact.Length == 0) errors.Add("Contact Number is required.");
            else if (contact.Length < 7 || contact.Length > 30) errors.Add("Contact Number must be 7 to 30 characters.");
            if (barangay.Length > 100 || municipality.Length > 100 || province.Length > 100)
                errors.Add("Barangay, Municipality and Province must not exceed 100 characters.");

            var fullName = Farmer.BuildDisplayName(first, last, middle, suppliedFull);
            var duplicate = false;
            if (errors.Count == 0 && fullName.Length > 0 && contact.Length > 0)
            {
                duplicate = await _context.Farmers.AsNoTracking().AnyAsync(
                    farmer => farmer.OwnerUserId == OwnerId && farmer.IsActive && farmer.FullName == fullName && farmer.ContactNumber == contact, cancellationToken);
                if (!duplicate) duplicate = !seen.Add($"{fullName}||{contact}");
            }
            var valid = errors.Count == 0 && !duplicate;
            if (valid) validCount++; else if (duplicate) duplicateCount++; else invalidCount++;
            result.Add(new
            {
                rowNumber = index + 2,
                values = row,
                valid,
                duplicate,
                errors = duplicate ? new[] { "Possible duplicate of an active farmer; this row will be skipped." } : errors.ToArray()
            });
        }
        return Ok(new
        {
            fileName = file.FileName,
            headers = FarmerHeaders,
            totalRows = result.Count,
            validCount,
            invalidCount,
            duplicateCount,
            canImport = validCount > 0,
            rows = result
        });
    }

    [HttpPost("farmers/import")]
    public async Task<IActionResult> ImportFarmers([FromForm] IFormFile file, [FromForm] bool confirm, CancellationToken cancellationToken)
    {
        if (!confirm) return BadRequest("Confirmation is required before importing records.");
        var parsed = await ParseRows(file, "Farmer Records", cancellationToken);
        if (parsed.Error is not null) return BadRequest(parsed.Error);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inserted = 0;
        var skipped = 0;
        var errors = new List<string>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var (row, index) in parsed.Rows.Select((row, index) => (row, index)))
        {
            var last = Clean(Get(row, "Last Name", "LastName"));
            var first = Clean(Get(row, "First Name", "FirstName"));
            var middle = Clean(Get(row, "Middle Name", "MiddleName"));
            var suppliedFull = Clean(Get(row, "Full Name", "FullName"));
            var contact = Clean(Get(row, "Contact Number", "ContactNumber"));
            var fullName = Farmer.BuildDisplayName(first, last, middle, suppliedFull);
            var rowErrors = new List<string>();
            if (last.Length == 0 || last.Length > 60) rowErrors.Add("Invalid Last Name.");
            if (contact.Length < 7 || contact.Length > 30) rowErrors.Add("Invalid Contact Number.");
            if (suppliedFull.Length > 150 || first.Length > 60 || middle.Length > 60) rowErrors.Add("Name exceeds the allowed length.");
            if (rowErrors.Count > 0) { skipped++; errors.Add($"Row {index + 2}: {string.Join(" ", rowErrors)}"); continue; }

            var duplicate = await _context.Farmers.AsNoTracking().AnyAsync(
                farmer => farmer.OwnerUserId == OwnerId && farmer.IsActive && farmer.FullName == fullName && farmer.ContactNumber == contact, cancellationToken);
            if (duplicate || !seen.Add($"{fullName}||{contact}")) { skipped++; continue; }

            _context.Farmers.Add(new Farmer
            {
                FullName = fullName,
                FirstName = Null(first),
                LastName = last,
                MiddleName = Null(middle),
                ContactNumber = contact,
                Barangay = Null(Get(row, "Barangay")),
                Municipality = Null(Get(row, "Municipality")),
                Province = Null(Get(row, "Province")),
                IsActive = true,
                OwnerUserId = OwnerId
            });
            inserted++;
        }
        if (inserted > 0) await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { inserted, skipped, errors });
    }

    private static string? Null(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ------------------------------------------------------------------
    // Supply records
    // ------------------------------------------------------------------

    [HttpGet("supplies/template")]
    public IActionResult DownloadSupplyTemplate()
    {
        var sheet = new XlsxService.Sheet
        {
            Name = "Supply Inventory",
            Columns = SupplyHeaders.Select(header => new XlsxService.Column { Header = header, Width = header.Length > 13 ? 24 : 18 }).ToArray(),
            Rows = Array.Empty<object?[]>(),
            Summary = "RISE import template · Supply Name, Category Name and Unit of Measure are required."
        };
        var instructions = new XlsxService.Sheet
        {
            Name = "Instructions",
            Columns = new[] { new XlsxService.Column { Header = "Field", Width = 24 }, new XlsxService.Column { Header = "Rule", Width = 90 } },
            Rows = new object?[][]
            {
                new object?[] { "Supply Name", "Required, 2 to 200 characters." },
                new object?[] { "Category Name", "Required; an existing category is reused, a new one is created when needed." },
                new object?[] { "Unit of Measure", "Required, for example bag, sack, kg or pc." },
                new object?[] { "Unit Cost / Stock Quantity", "Numbers only, zero or greater. Stock is initial inventory, not an adjustment." },
                new object?[] { "Duplicate check", "Active supplies with the same name, category and unit are skipped; existing records are never overwritten." }
            }
        };
        return Xlsx(XlsxService.Write(new[] { sheet, instructions }, "RISE Supply Import Template", "Supply Records"), "Supply_Import_Template.xlsx");
    }

    [HttpGet("supplies/export")]
    public async Task<IActionResult> ExportSupplies([FromQuery] string? search, [FromQuery] string? sort, CancellationToken cancellationToken)
    {
        var query = _context.Supplies.AsNoTracking().Where(supply => supply.IsActive && supply.OwnerUserId == OwnerId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(supply => supply.Name.Contains(search) || supply.Category.Name.Contains(search));
        var supplies = await query.Select(supply => new
        {
            supply.SupplyId, supply.Name, CategoryName = supply.Category.Name, supply.UnitOfMeasure, supply.UnitCost,
            supply.StockQuantity, supply.ExpirationDate, supply.CreatedAtUtc, supply.IsActive
        }).ToListAsync(cancellationToken);
        supplies = (sort switch
        {
            "stock-desc" => supplies.OrderByDescending(supply => supply.StockQuantity).ThenBy(supply => supply.Name),
            "stock-asc" => supplies.OrderBy(supply => supply.StockQuantity).ThenBy(supply => supply.Name),
            _ => supplies.OrderBy(supply => supply.Name)
        }).ToList();

        var columns = new[]
        {
            new XlsxService.Column { Header = "No.", Kind = XlsxService.CellKind.Integer, Width = 8 },
            new XlsxService.Column { Header = "Supply Name", Width = 28 },
            new XlsxService.Column { Header = "Category", Width = 20 },
            new XlsxService.Column { Header = "Unit of Measure", Width = 18 },
            new XlsxService.Column { Header = "Unit Cost", Kind = XlsxService.CellKind.Number, Width = 14 },
            new XlsxService.Column { Header = "Stock Quantity", Kind = XlsxService.CellKind.Number, Width = 18 },
            new XlsxService.Column { Header = "Stock Status", Width = 16 },
            new XlsxService.Column { Header = "Expiration Date", Kind = XlsxService.CellKind.Date, Width = 18 },
            new XlsxService.Column { Header = "Created At", Kind = XlsxService.CellKind.Date, Width = 20 },
            new XlsxService.Column { Header = "Status", Width = 12 }
        };
        var rows = supplies.Select((supply, index) => new object?[]
        {
            index + 1, supply.Name, supply.CategoryName, supply.UnitOfMeasure,
            supply.UnitCost, supply.StockQuantity, StockStatus(supply.StockQuantity),
            supply.ExpirationDate, supply.CreatedAtUtc, supply.IsActive ? "Active" : "Archived"
        }).ToArray();

        var categories = await _context.SupplyCategories.AsNoTracking().Where(category => category.OwnerUserId == OwnerId)
            .Select(category => new { category.SupplyCategoryId, category.Name, category.Description, Count = category.Supplies.Count })
            .OrderBy(category => category.Name).ToListAsync(cancellationToken);
        var categorySheet = new XlsxService.Sheet
        {
            Name = "Supply Categories",
            Columns = new[]
            {
                new XlsxService.Column { Header = "No.", Kind = XlsxService.CellKind.Integer, Width = 8 },
                new XlsxService.Column { Header = "Category", Width = 26 },
                new XlsxService.Column { Header = "Description", Width = 46 },
                new XlsxService.Column { Header = "Supplies", Kind = XlsxService.CellKind.Number, Width = 14 }
            },
            Rows = categories.Select((category, index) => new object?[] { index + 1, category.Name, category.Description, category.Count }).ToArray()
        };
        var sheets = new[]
        {
            new XlsxService.Sheet { Name = "Supply Inventory", Columns = columns, Rows = rows },
            categorySheet
        };
        return Xlsx(XlsxService.Write(sheets, "RISE Supply Records"), "Supply_Records.xlsx");
    }

    [HttpPost("supplies/preview")]
    public async Task<IActionResult> PreviewSupplies([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var parsed = await ParseRows(file, "Supply Inventory", cancellationToken);
        if (parsed.Error is not null) return BadRequest(parsed.Error);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<object>();
        int validCount = 0, invalidCount = 0, duplicateCount = 0;
        foreach (var (row, index) in parsed.Rows.Select((row, index) => (row, index)))
        {
            var errors = new List<string>();
            var name = Clean(Get(row, "Supply Name", "Name"));
            var category = Clean(Get(row, "Category Name", "Category"));
            var unit = Clean(Get(row, "Unit of Measure", "Unit"));
            var unitCostText = Get(row, "Unit Cost");
            var stockText = Get(row, "Stock Quantity");
            var expirationText = Get(row, "Expiration Date");

            if (name.Length < 2 || name.Length > 200) errors.Add("Supply Name must be 2 to 200 characters.");
            if (category.Length == 0 || category.Length > 100) errors.Add("Category Name is required (maximum 100 characters).");
            if (unit.Length == 0 || unit.Length > 30) errors.Add("Unit of Measure is required (maximum 30 characters).");
            if (!TryDecimal(unitCostText, out var unitCost) || unitCost < 0) errors.Add("Unit Cost must be zero or greater.");
            if (!TryDecimal(stockText, out var stock) || stock < 0) errors.Add("Stock Quantity must be zero or greater.");
            DateOnly? expiration = null;
            var parsedExpiration = default(DateOnly);
            if (expirationText is not null && !TryDate(expirationText, out parsedExpiration))
                errors.Add("Expiration Date is not a valid date.");
            else if (expirationText is not null) expiration = parsedExpiration;

            var duplicate = false;
            if (errors.Count == 0)
            {
                duplicate = await _context.Supplies.AsNoTracking().AnyAsync(
                    supply => supply.OwnerUserId == OwnerId && supply.IsActive && supply.Name == name && supply.Category.Name == category && supply.UnitOfMeasure == unit,
                    cancellationToken);
                if (!duplicate) duplicate = !seen.Add($"{name}||{category}||{unit}");
            }
            var valid = errors.Count == 0 && !duplicate;
            if (valid) validCount++; else if (duplicate) duplicateCount++; else invalidCount++;
            result.Add(new
            {
                rowNumber = index + 2,
                values = row,
                valid,
                duplicate,
                errors = duplicate ? new[] { "Possible duplicate of an active supply; this row will be skipped." } : errors.ToArray()
            });
        }
        return Ok(new
        {
            fileName = file.FileName,
            headers = SupplyHeaders,
            totalRows = result.Count,
            validCount,
            invalidCount,
            duplicateCount,
            canImport = validCount > 0,
            rows = result
        });
    }

    [HttpPost("supplies/import")]
    public async Task<IActionResult> ImportSupplies([FromForm] IFormFile file, [FromForm] bool confirm, CancellationToken cancellationToken)
    {
        if (!confirm) return BadRequest("Confirmation is required before importing records.");
        var parsed = await ParseRows(file, "Supply Inventory", cancellationToken);
        if (parsed.Error is not null) return BadRequest(parsed.Error);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categories = new Dictionary<string, SupplyCategory>(StringComparer.OrdinalIgnoreCase);
        int inserted = 0, skipped = 0;
        var errors = new List<string>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var (row, index) in parsed.Rows.Select((row, index) => (row, index)))
        {
            var name = Clean(Get(row, "Supply Name", "Name"));
            var categoryName = Clean(Get(row, "Category Name", "Category"));
            var unit = Clean(Get(row, "Unit of Measure", "Unit"));
            var rowErrors = new List<string>();
            if (name.Length < 2 || name.Length > 200) rowErrors.Add("Invalid Supply Name.");
            if (categoryName.Length == 0 || categoryName.Length > 100) rowErrors.Add("Invalid Category Name.");
            if (unit.Length == 0 || unit.Length > 30) rowErrors.Add("Invalid Unit of Measure.");
            if (!TryDecimal(Get(row, "Unit Cost"), out var unitCost) || unitCost < 0) rowErrors.Add("Invalid Unit Cost.");
            if (!TryDecimal(Get(row, "Stock Quantity"), out var stock) || stock < 0) rowErrors.Add("Invalid Stock Quantity.");
            DateOnly? expiration = null;
            var parsedExpiration = default(DateOnly);
            var expirationText = Get(row, "Expiration Date");
            if (expirationText is not null && !TryDate(expirationText, out parsedExpiration)) rowErrors.Add("Invalid Expiration Date.");
            else if (expirationText is not null) expiration = parsedExpiration;
            if (rowErrors.Count > 0) { skipped++; errors.Add($"Row {index + 2}: {string.Join(" ", rowErrors)}"); continue; }

            var duplicate = await _context.Supplies.AsNoTracking().AnyAsync(
                supply => supply.IsActive && supply.Name == name && supply.Category.Name == categoryName && supply.UnitOfMeasure == unit,
                cancellationToken);
            if (duplicate || !seen.Add($"{name}||{categoryName}||{unit}")) { skipped++; continue; }

            if (!categories.TryGetValue(categoryName, out var category))
            {
                category = await _context.SupplyCategories.AsNoTracking().FirstOrDefaultAsync(item => item.Name == categoryName && item.OwnerUserId == OwnerId, cancellationToken);
                if (category is null)
                {
                    category = new SupplyCategory { Name = categoryName, OwnerUserId = OwnerId };
                    _context.SupplyCategories.Add(category);
                }
                categories[categoryName] = category;
            }
            _context.Supplies.Add(new Supply
            {
                Name = name,
                UnitOfMeasure = unit,
                UnitCost = unitCost,
                StockQuantity = stock,
                ExpirationDate = expiration.HasValue ? expiration.Value.ToDateTime(TimeOnly.MinValue) : null,
                Category = category,
                IsActive = true,
                OwnerUserId = OwnerId,
                CreatedAtUtc = DateTime.UtcNow
            });
            inserted++;
        }
        if (inserted > 0) await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { inserted, skipped, errors });
    }

    private async Task<(IReadOnlyList<Dictionary<string, string>> Rows, string? Error)> ParseRows(
        IFormFile file, string sheetName, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return (Array.Empty<Dictionary<string, string>>(), "Choose a valid Excel (.xlsx) file.");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return (Array.Empty<Dictionary<string, string>>(), "Only .xlsx files are accepted.");
        if (file.Length > 10 * 1024 * 1024)
            return (Array.Empty<Dictionary<string, string>>(), "The Excel file must not exceed 10 MB.");
        try
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            var rows = XlsxService.Read(stream, sheetName);
            if (rows.Count == 0) return (rows, "The worksheet contains no data rows.");
            return (rows, null);
        }
        catch (Exception error) when (error is InvalidDataException or System.Xml.XmlException or IOException)
        {
            return (Array.Empty<Dictionary<string, string>>(), "The file is not a valid .xlsx workbook.");
        }
    }

    // ------------------------------------------------------------------
    // Additional Excel reports
    // ------------------------------------------------------------------

    [HttpGet("reports/{dataset}")]
    public async Task<IActionResult> ExportReport(string dataset, CancellationToken cancellationToken)
    {
        var title = $"RISE {dataset} Report";
        var subtitle = $"Generated {DateTime.Now:yyyy-MM-dd HH:mm}";
        switch (dataset.ToLowerInvariant())
        {
            case "farmers":
            {
                var farmers = await _context.Farmers.AsNoTracking().Where(farmer => farmer.IsActive && farmer.OwnerUserId == OwnerId)
                    .OrderBy(farmer => farmer.FullName)
                    .Select(farmer => new object?[]
                    {
                        farmer.FarmerId, farmer.FullName, farmer.ContactNumber, farmer.Barangay,
                        farmer.Municipality, farmer.Province, farmer.IsActive ? "Active" : "Archived"
                    }).ToArrayAsync(cancellationToken);
                return Xlsx(XlsxService.Write(new[]
                {
                    new XlsxService.Sheet
                    {
                        Name = "Farmer Records",
                        Columns = FarmerColumns(),
                        Rows = farmers,
                        Summary = $"Farmers: {farmers.Length}"
                    }
                }, title, subtitle), "Farmer_Records.xlsx");
            }
            case "inventory":
            {
                var supplies = await _context.Supplies.AsNoTracking().Where(supply => supply.IsActive && supply.OwnerUserId == OwnerId)
                    .OrderBy(supply => supply.Name)
                    .Select(supply => new object?[]
                    {
                        supply.SupplyId, supply.Name, supply.Category.Name, supply.UnitOfMeasure,
                        supply.StockQuantity, StockStatus(supply.StockQuantity), supply.ExpirationDate
                    }).ToArrayAsync(cancellationToken);
                return Xlsx(XlsxService.Write(new[]
                {
                    new XlsxService.Sheet { Name = "Inventory", Columns = InventoryColumns(), Rows = supplies, Summary = $"Supplies: {supplies.Length}" }
                }, title, subtitle), "Supply_Inventory.xlsx");
            }
            case "distributions":
            {
                var rows = await _context.DistributionItems.AsNoTracking().Where(item => item.Distribution.Farmer.OwnerUserId == OwnerId)
                    .OrderByDescending(item => item.Distribution.DistributionDate)
                    .Select(item => new object?[]
                    {
                        item.DistributionId, item.Distribution.ReferenceNumber, item.Distribution.DistributionDate,
                        item.Distribution.Status, item.Distribution.Farmer.FullName,
                        item.Distribution.Farmer.Barangay, item.Supply.Name, item.Supply.Category.Name,
                        item.Quantity, item.Supply.UnitOfMeasure, item.TargetMonitoringDate
                    }).ToArrayAsync(cancellationToken);
                return Xlsx(XlsxService.Write(new[]
                {
                    new XlsxService.Sheet { Name = "Distributions", Columns = DistributionColumns(), Rows = rows, Summary = $"Distribution items: {rows.Length}" }
                }, title, subtitle), "Supply_Distribution_Records.xlsx");
            }
            case "usage":
            case "disposed":
            {
                var query = _context.UtilizationMonitorings.AsNoTracking().Where(record => record.DistributionItem.Distribution.Farmer.OwnerUserId == OwnerId).AsQueryable();
                if (dataset.Equals("disposed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(record => record.QuantityDisposed > 0);
                var rows = await query.OrderByDescending(record => record.MonitoringDate)
                    .Select(record => new object?[]
                    {
                        record.DistributionItem.DistributionId,
                        record.DistributionItem.Distribution.Farmer.FullName,
                        record.DistributionItem.Supply.Name,
                        record.DistributionItem.Supply.Category.Name,
                        record.MonitoringDate,
                        record.QuantityUsed,
                        record.QuantityStillStocked,
                        record.QuantityDisposed,
                        record.DistributionItem.Supply.UnitOfMeasure,
                        record.Remarks
                    }).ToArrayAsync(cancellationToken);
                var sheetName = dataset.Equals("disposed", StringComparison.OrdinalIgnoreCase) ? "Disposed Supplies" : "Supply Usage";
                return Xlsx(XlsxService.Write(new[]
                {
                    new XlsxService.Sheet { Name = sheetName, Columns = UsageColumns(), Rows = rows, Summary = $"Monitoring records: {rows.Length}" }
                }, title, subtitle), dataset.Equals("disposed", StringComparison.OrdinalIgnoreCase) ? "Disposed_Supplies.xlsx" : "Supply_Usage_Report.xlsx");
            }
            case "stock-summary":
            {
                var supplies = await _context.Supplies.AsNoTracking().Where(supply => supply.IsActive && supply.OwnerUserId == OwnerId)
                    .Select(supply => new { supply.Category.Name, supply.UnitOfMeasure, supply.StockQuantity })
                    .ToListAsync(cancellationToken);
                var groups = supplies.GroupBy(supply => $"{supply.Name} ({supply.UnitOfMeasure})")
                    .OrderBy(group => group.Key)
                    .Select(group => new object?[]
                    {
                        group.Key,
                        group.Select(supply => supply.UnitOfMeasure).Distinct().FirstOrDefault() ?? "",
                        group.Count(),
                        group.Sum(supply => supply.StockQuantity)
                    }).ToArray();
                return Xlsx(XlsxService.Write(new[]
                {
                    new XlsxService.Sheet
                    {
                        Name = "Stock Summary",
                        Columns = new[]
                        {
                            new XlsxService.Column { Header = "Category and unit", Width = 34 },
                            new XlsxService.Column { Header = "Unit", Width = 14 },
                            new XlsxService.Column { Header = "Supplies", Kind = XlsxService.CellKind.Number, Width = 14 },
                            new XlsxService.Column { Header = "Total stock", Kind = XlsxService.CellKind.Number, Width = 18 }
                        },
                        Rows = groups,
                        Summary = $"Categories: {groups.Length}"
                    }
                }, title, subtitle), "Supply_Stock_Summary.xlsx");
            }
            case "full":
            {
                var sheets = new List<XlsxService.Sheet>();
                var inventory = await _context.Supplies.AsNoTracking().Where(supply => supply.IsActive && supply.OwnerUserId == OwnerId)
                    .OrderBy(supply => supply.Name)
                    .Select(supply => new object?[] { supply.SupplyId, supply.Name, supply.Category.Name, supply.UnitOfMeasure, supply.StockQuantity, StockStatus(supply.StockQuantity), supply.ExpirationDate })
                    .ToArrayAsync(cancellationToken);
                sheets.Add(new XlsxService.Sheet { Name = "Inventory", Columns = InventoryColumns(), Rows = inventory, Summary = $"Supplies: {inventory.Length}" });
                var distributions = await _context.DistributionItems.AsNoTracking().Where(item => item.Distribution.Farmer.OwnerUserId == OwnerId)
                    .Select(item => new object?[]
                    {
                        item.DistributionId, item.Distribution.ReferenceNumber, item.Distribution.DistributionDate,
                        item.Distribution.Status, item.Distribution.Farmer.FullName, item.Distribution.Farmer.Barangay,
                        item.Supply.Name, item.Supply.Category.Name, item.Quantity, item.Supply.UnitOfMeasure,
                        item.TargetMonitoringDate
                    }).ToArrayAsync(cancellationToken);
                sheets.Add(new XlsxService.Sheet { Name = "Distributions", Columns = DistributionColumns(), Rows = distributions, Summary = $"Distribution items: {distributions.Length}" });
                var usage = await _context.UtilizationMonitorings.AsNoTracking().Where(record => record.DistributionItem.Distribution.Farmer.OwnerUserId == OwnerId)
                    .Select(record => new object?[]
                    {
                        record.DistributionItem.DistributionId, record.DistributionItem.Distribution.Farmer.FullName,
                        record.DistributionItem.Supply.Name, record.DistributionItem.Supply.Category.Name,
                        record.MonitoringDate, record.QuantityUsed, record.QuantityStillStocked,
                        record.QuantityDisposed, record.DistributionItem.Supply.UnitOfMeasure,
                        record.Remarks
                    }).ToArrayAsync(cancellationToken);
                sheets.Add(new XlsxService.Sheet { Name = "Usage", Columns = UsageColumns(), Rows = usage, Summary = $"Monitoring records: {usage.Length}" });
                sheets.Add(new XlsxService.Sheet { Name = "Disposed Supplies", Columns = UsageColumns(), Rows = usage.Where(row => row[7] != null && Convert.ToDecimal(row[7]) > 0).ToArray(), Summary = "Only records with disposed quantity" });
                return Xlsx(XlsxService.Write(sheets, "RISE Supply Management Report", subtitle), "Supply_Management_Report.xlsx");
            }
            default:
                return BadRequest("Unknown report dataset.");
        }
    }

    private static XlsxService.Column[] FarmerColumns() => new[]
    {
        new XlsxService.Column { Header = "No.", Kind = XlsxService.CellKind.Integer, Width = 8 },
        new XlsxService.Column { Header = "Full Name", Width = 28 },
        new XlsxService.Column { Header = "Contact Number", Width = 18 },
        new XlsxService.Column { Header = "Barangay", Width = 20 },
        new XlsxService.Column { Header = "Municipality", Width = 20 },
        new XlsxService.Column { Header = "Province", Width = 20 },
        new XlsxService.Column { Header = "Status", Width = 12 }
    };

    private static XlsxService.Column[] InventoryColumns() => new[]
    {
        new XlsxService.Column { Header = "Supply ID", Kind = XlsxService.CellKind.Number, Width = 12 },
        new XlsxService.Column { Header = "Supply Name", Width = 28 },
        new XlsxService.Column { Header = "Category", Width = 20 },
        new XlsxService.Column { Header = "Unit", Width = 14 },
        new XlsxService.Column { Header = "Stock Quantity", Kind = XlsxService.CellKind.Number, Width = 18 },
        new XlsxService.Column { Header = "Stock Status", Width = 16 },
        new XlsxService.Column { Header = "Expiration Date", Kind = XlsxService.CellKind.Date, Width = 18 }
    };

    private static XlsxService.Column[] DistributionColumns() => new[]
    {
        new XlsxService.Column { Header = "Distribution ID", Kind = XlsxService.CellKind.Number, Width = 16 },
        new XlsxService.Column { Header = "Reference No.", Width = 26 },
        new XlsxService.Column { Header = "Date", Kind = XlsxService.CellKind.Date, Width = 16 },
        new XlsxService.Column { Header = "Status", Width = 14 },
        new XlsxService.Column { Header = "Farmer", Width = 28 },
        new XlsxService.Column { Header = "Barangay", Width = 20 },
        new XlsxService.Column { Header = "Supply", Width = 26 },
        new XlsxService.Column { Header = "Category", Width = 18 },
        new XlsxService.Column { Header = "Quantity", Kind = XlsxService.CellKind.Number, Width = 14 },
        new XlsxService.Column { Header = "Unit", Width = 12 },
        new XlsxService.Column { Header = "Target Monitoring Date", Kind = XlsxService.CellKind.Date, Width = 22 }
    };

    private static XlsxService.Column[] UsageColumns() => new[]
    {
        new XlsxService.Column { Header = "Distribution ID", Kind = XlsxService.CellKind.Number, Width = 16 },
        new XlsxService.Column { Header = "Farmer", Width = 26 },
        new XlsxService.Column { Header = "Supply", Width = 26 },
        new XlsxService.Column { Header = "Category", Width = 18 },
        new XlsxService.Column { Header = "Monitoring Date", Kind = XlsxService.CellKind.Date, Width = 18 },
        new XlsxService.Column { Header = "Used", Kind = XlsxService.CellKind.Number, Width = 14 },
        new XlsxService.Column { Header = "Still Stocked", Kind = XlsxService.CellKind.Number, Width = 16 },
        new XlsxService.Column { Header = "Disposed", Kind = XlsxService.CellKind.Number, Width = 14 },
        new XlsxService.Column { Header = "Unit", Width = 12 },
        new XlsxService.Column { Header = "Remarks", Width = 34 }
    };
}
