using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;

namespace RISE.API.Controllers;

[ApiController]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class OperationalRecordsController : ControllerBase
{
    private readonly RiseDbContext _context;
    private readonly ILogger<OperationalRecordsController> _logger;

    public OperationalRecordsController(
        RiseDbContext context,
        ILogger<OperationalRecordsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("api/associations")]
    public async Task<ActionResult<IReadOnlyList<AssociationDto>>> GetAssociations(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.FarmerAssociations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(association => association.Name.Contains(search));
        }

        return Ok(await query
            .OrderBy(association => association.Name)
            .Select(association => new AssociationDto(
                association.FarmerAssociationId,
                association.Name,
                association.Farmers.Count(farmer => farmer.IsActive)))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/associations/{id:int}")]
    public async Task<ActionResult<AssociationDto>> GetAssociation(
        int id,
        CancellationToken cancellationToken)
    {
        var association = await _context.FarmerAssociations.AsNoTracking()
            .Where(item => item.FarmerAssociationId == id)
            .Select(item => new AssociationDto(
                item.FarmerAssociationId, item.Name,
                item.Farmers.Count(farmer => farmer.IsActive)))
            .FirstOrDefaultAsync(cancellationToken);
        return association is null ? NotFound() : Ok(association);
    }

    [HttpPost("api/associations")]
    public async Task<ActionResult<AssociationDto>> CreateAssociation(
        AssociationWriteDto request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _context.FarmerAssociations.AnyAsync(
                association => association.Name == name, cancellationToken))
        {
            return Conflict("An association with this name already exists.");
        }

        var association = new FarmerAssociation { Name = name };
        _context.FarmerAssociations.Add(association);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetAssociation),
            new { id = association.FarmerAssociationId },
            new AssociationDto(association.FarmerAssociationId, association.Name, 0));
    }

    [HttpPut("api/associations/{id:int}")]
    public async Task<IActionResult> UpdateAssociation(
        int id,
        AssociationWriteDto request,
        CancellationToken cancellationToken)
    {
        var association = await _context.FarmerAssociations
            .FirstOrDefaultAsync(item => item.FarmerAssociationId == id, cancellationToken);
        if (association is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (await _context.FarmerAssociations.AnyAsync(
                item => item.FarmerAssociationId != id && item.Name == name,
                cancellationToken))
        {
            return Conflict("An association with this name already exists.");
        }

        association.Name = name;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not rename association {AssociationId}.", id);
            return Conflict("An association with this name may already exist.");
        }
        return NoContent();
    }

    [HttpDelete("api/associations/{id:int}")]
    public async Task<IActionResult> DeleteAssociation(
        int id,
        CancellationToken cancellationToken)
    {
        var association = await _context.FarmerAssociations
            .FirstOrDefaultAsync(item => item.FarmerAssociationId == id, cancellationToken);
        if (association is null)
        {
            return NotFound();
        }

        if (await _context.Farmers.AnyAsync(
                farmer => farmer.FarmerAssociationId == id, cancellationToken))
        {
            return Conflict("This association is still assigned to one or more farmers.");
        }

        _context.FarmerAssociations.Remove(association);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete association {AssociationId}.", id);
            return Conflict("This association could not be deleted because it is still referenced.");
        }
        return NoContent();
    }

    [HttpGet("api/farms")]
    public async Task<ActionResult<IReadOnlyList<FarmDto>>> GetFarms(
        [FromQuery] string? search,
        [FromQuery] int? farmerId,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = _context.Farms.AsNoTracking().AsQueryable();
        if (!includeArchived)
        {
            query = query.Where(farm => farm.IsActive);
        }
        if (farmerId.HasValue)
        {
            query = query.Where(farm => farm.FarmerId == farmerId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(farm =>
                farm.Name.Contains(search) ||
                farm.Barangay.Contains(search) ||
                farm.Municipality.Contains(search) ||
                farm.Farmer.FullName.Contains(search));
        }

        return Ok(await query.OrderBy(farm => farm.Name)
            .Select(farm => new FarmDto(
                farm.FarmId, farm.FarmerId, farm.Farmer.FullName, farm.Name,
                farm.Barangay, farm.Municipality, farm.AreaHectares, farm.SoilType,
                farm.IsActive))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/farms/{id:int}")]
    public async Task<ActionResult<FarmDto>> GetFarm(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _context.Farms.AsNoTracking()
            .Where(farm => farm.FarmId == id)
            .Select(farm => new FarmDto(
                farm.FarmId, farm.FarmerId, farm.Farmer.FullName, farm.Name,
                farm.Barangay, farm.Municipality, farm.AreaHectares, farm.SoilType,
                farm.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("api/farms")]
    public async Task<ActionResult<FarmDto>> CreateFarm(
        FarmWriteDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Farmers.AnyAsync(
                farmer => farmer.FarmerId == request.FarmerId && farmer.IsActive,
                cancellationToken))
        {
            return BadRequest("Select an active farmer for this farm.");
        }

        var farm = new Farm
        {
            FarmerId = request.FarmerId,
            Name = request.Name.Trim(),
            Barangay = request.Barangay.Trim(),
            Municipality = request.Municipality.Trim(),
            AreaHectares = request.AreaHectares,
            SoilType = Clean(request.SoilType),
            IsActive = true
        };
        _context.Farms.Add(farm);
        await _context.SaveChangesAsync(cancellationToken);

        var farmerName = await _context.Farmers.Where(item => item.FarmerId == farm.FarmerId)
            .Select(item => item.FullName).SingleAsync(cancellationToken);
        var result = new FarmDto(
            farm.FarmId, farm.FarmerId, farmerName, farm.Name, farm.Barangay,
            farm.Municipality, farm.AreaHectares, farm.SoilType, farm.IsActive);
        return CreatedAtAction(nameof(GetFarm), new { id = farm.FarmId }, result);
    }

    [HttpPut("api/farms/{id:int}")]
    public async Task<IActionResult> UpdateFarm(
        int id,
        FarmWriteDto request,
        CancellationToken cancellationToken)
    {
        var farm = await _context.Farms.FirstOrDefaultAsync(
            item => item.FarmId == id && item.IsActive, cancellationToken);
        if (farm is null)
        {
            return NotFound();
        }
        if (!await _context.Farmers.AnyAsync(
                farmer => farmer.FarmerId == request.FarmerId && farmer.IsActive,
                cancellationToken))
        {
            return BadRequest("Select an active farmer for this farm.");
        }

        farm.FarmerId = request.FarmerId;
        farm.Name = request.Name.Trim();
        farm.Barangay = request.Barangay.Trim();
        farm.Municipality = request.Municipality.Trim();
        farm.AreaHectares = request.AreaHectares;
        farm.SoilType = Clean(request.SoilType);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("api/farms/{id:int}")]
    public async Task<IActionResult> ArchiveFarm(
        int id,
        CancellationToken cancellationToken)
    {
        var farm = await _context.Farms.FirstOrDefaultAsync(
            item => item.FarmId == id, cancellationToken);
        if (farm is null)
        {
            return NotFound();
        }
        farm.IsActive = false;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not archive farm {FarmId}.", id);
            return Conflict("This farm cannot be archived while it has related operational records.");
        }
        return NoContent();
    }

    [HttpGet("api/planting")]
    public async Task<ActionResult<IReadOnlyList<PlantingDto>>> GetPlantings(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? farmId,
        CancellationToken cancellationToken)
    {
        var query = _context.PlantingRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(record =>
                record.CropName.Contains(search) ||
                (record.Variety != null && record.Variety.Contains(search)) ||
                record.Farm.Name.Contains(search) ||
                record.Farm.Farmer.FullName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(record => record.Status == status);
        }
        if (farmId.HasValue)
        {
            query = query.Where(record => record.FarmId == farmId.Value);
        }

        return Ok(await query.OrderByDescending(record => record.PlantingDate)
            .Select(record => new PlantingDto(
                record.PlantingRecordId, record.FarmId, record.Farm.Name,
                record.Farm.Farmer.FullName, record.CropName, record.Variety,
                record.Season, record.PlantingDate, record.AreaPlantedHectares,
                record.ExpectedHarvestDate, record.Status))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/planting/{id:int}")]
    public async Task<ActionResult<PlantingDto>> GetPlanting(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.PlantingRecords.AsNoTracking()
            .Where(item => item.PlantingRecordId == id)
            .Select(item => new PlantingDto(
                item.PlantingRecordId, item.FarmId, item.Farm.Name,
                item.Farm.Farmer.FullName, item.CropName, item.Variety,
                item.Season, item.PlantingDate, item.AreaPlantedHectares,
                item.ExpectedHarvestDate, item.Status))
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("api/planting")]
    public async Task<ActionResult<PlantingDto>> CreatePlanting(
        PlantingWriteDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Farms.AnyAsync(
                farm => farm.FarmId == request.FarmId && farm.IsActive,
                cancellationToken))
        {
            return BadRequest("Select an active farm for this planting record.");
        }
        var validation = ValidatePlantingDates(request.PlantingDate, request.ExpectedHarvestDate);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var record = new PlantingRecord
        {
            FarmId = request.FarmId,
            CropName = request.CropName.Trim(),
            Variety = Clean(request.Variety),
            Season = Clean(request.Season),
            PlantingDate = request.PlantingDate,
            AreaPlantedHectares = request.AreaPlantedHectares,
            ExpectedHarvestDate = request.ExpectedHarvestDate,
            Status = request.Status
        };
        _context.PlantingRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        var response = await GetPlanting(record.PlantingRecordId, cancellationToken);
        return CreatedAtAction(nameof(GetPlanting), new { id = record.PlantingRecordId }, response.Value);
    }

    [HttpPut("api/planting/{id:int}")]
    public async Task<IActionResult> UpdatePlanting(
        int id,
        PlantingWriteDto request,
        CancellationToken cancellationToken)
    {
        var record = await _context.PlantingRecords.FirstOrDefaultAsync(
            item => item.PlantingRecordId == id, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }
        if (!await _context.Farms.AnyAsync(
                farm => farm.FarmId == request.FarmId && farm.IsActive,
                cancellationToken))
        {
            return BadRequest("Select an active farm for this planting record.");
        }
        var validation = ValidatePlantingDates(request.PlantingDate, request.ExpectedHarvestDate);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        record.FarmId = request.FarmId;
        record.CropName = request.CropName.Trim();
        record.Variety = Clean(request.Variety);
        record.Season = Clean(request.Season);
        record.PlantingDate = request.PlantingDate;
        record.AreaPlantedHectares = request.AreaPlantedHectares;
        record.ExpectedHarvestDate = request.ExpectedHarvestDate;
        record.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("api/planting/{id:int}")]
    public async Task<IActionResult> DeletePlanting(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.PlantingRecords.FirstOrDefaultAsync(
            item => item.PlantingRecordId == id, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }
        _context.PlantingRecords.Remove(record);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete planting record {PlantingRecordId}.", id);
            return Conflict("This planting record has related records and cannot be deleted.");
        }
        return NoContent();
    }

    [HttpGet("api/harvest")]
    public async Task<ActionResult<IReadOnlyList<HarvestDto>>> GetHarvests(
        [FromQuery] string? search,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = _context.HarvestRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(record =>
                record.PlantingRecord.CropName.Contains(search) ||
                record.PlantingRecord.Farm.Farmer.FullName.Contains(search) ||
                (record.Grade != null && record.Grade.Contains(search)));
        }
        if (from.HasValue)
        {
            query = query.Where(record => record.HarvestDate >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(record => record.HarvestDate <= to.Value);
        }

        return Ok(await query.OrderByDescending(record => record.HarvestDate)
            .Select(record => new HarvestDto(
                record.HarvestRecordId, record.PlantingRecordId,
                record.PlantingRecord.CropName,
                record.PlantingRecord.Farm.Farmer.FullName,
                record.HarvestDate, record.QuantityKilograms,
                record.MoisturePercent, record.Grade, record.Notes))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/harvest/{id:int}")]
    public async Task<ActionResult<HarvestDto>> GetHarvest(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.HarvestRecords.AsNoTracking()
            .Where(item => item.HarvestRecordId == id)
            .Select(item => new HarvestDto(
                item.HarvestRecordId, item.PlantingRecordId,
                item.PlantingRecord.CropName,
                item.PlantingRecord.Farm.Farmer.FullName,
                item.HarvestDate, item.QuantityKilograms,
                item.MoisturePercent, item.Grade, item.Notes))
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("api/harvest")]
    public async Task<ActionResult<HarvestDto>> CreateHarvest(
        HarvestWriteDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.PlantingRecords.AnyAsync(
                record => record.PlantingRecordId == request.PlantingRecordId,
                cancellationToken))
        {
            return BadRequest("Select an existing planting record.");
        }
        var record = new HarvestRecord
        {
            PlantingRecordId = request.PlantingRecordId,
            HarvestDate = request.HarvestDate,
            QuantityKilograms = request.QuantityKilograms,
            MoisturePercent = request.MoisturePercent,
            Grade = Clean(request.Grade),
            Notes = Clean(request.Notes)
        };
        _context.HarvestRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        var response = await GetHarvest(record.HarvestRecordId, cancellationToken);
        return CreatedAtAction(nameof(GetHarvest), new { id = record.HarvestRecordId }, response.Value);
    }

    [HttpPut("api/harvest/{id:int}")]
    public async Task<IActionResult> UpdateHarvest(
        int id,
        HarvestWriteDto request,
        CancellationToken cancellationToken)
    {
        var record = await _context.HarvestRecords.FirstOrDefaultAsync(
            item => item.HarvestRecordId == id, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }
        if (!await _context.PlantingRecords.AnyAsync(
                item => item.PlantingRecordId == request.PlantingRecordId,
                cancellationToken))
        {
            return BadRequest("Select an existing planting record.");
        }
        record.PlantingRecordId = request.PlantingRecordId;
        record.HarvestDate = request.HarvestDate;
        record.QuantityKilograms = request.QuantityKilograms;
        record.MoisturePercent = request.MoisturePercent;
        record.Grade = Clean(request.Grade);
        record.Notes = Clean(request.Notes);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("api/harvest/{id:int}")]
    public async Task<IActionResult> DeleteHarvest(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.HarvestRecords.FirstOrDefaultAsync(
            item => item.HarvestRecordId == id, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }
        _context.HarvestRecords.Remove(record);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete harvest record {HarvestRecordId}.", id);
            return Conflict("This harvest record has related verification or document records.");
        }
        return NoContent();
    }

    [HttpGet("api/supplies")]
    public async Task<ActionResult<IReadOnlyList<SupplyDto>>> GetSupplies(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = _context.Supplies.AsNoTracking().AsQueryable();
        if (!includeArchived)
        {
            query = query.Where(supply => supply.IsActive);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(supply => supply.SupplyCategoryId == categoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(supply =>
                supply.Name.Contains(search) ||
                supply.Category.Name.Contains(search));
        }

        return Ok(await query.OrderBy(supply => supply.Name)
            .Select(supply => new SupplyDto(
                supply.SupplyId, supply.SupplyCategoryId, supply.Category.Name,
                supply.Name, supply.UnitOfMeasure, supply.StockQuantity, supply.IsActive, supply.UnitCost))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/supplies/options")]
    public async Task<ActionResult<SupplyOptionsDto>> GetSupplyOptions(
        CancellationToken cancellationToken)
    {
        var categories = await _context.SupplyCategories.AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new LookupDto(item.SupplyCategoryId, item.Name))
            .ToListAsync(cancellationToken);
        var suppliers = await _context.Suppliers.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupDto(item.SupplierId, item.Name))
            .ToListAsync(cancellationToken);
        return Ok(new SupplyOptionsDto(categories, suppliers));
    }

    [HttpGet("api/supply-categories")]
    public async Task<ActionResult<IReadOnlyList<SupplyCategoryDto>>> GetSupplyCategories(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.SupplyCategories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(category => category.Name.Contains(search));
        }
        return Ok(await query.OrderBy(category => category.Name)
            .Select(category => new SupplyCategoryDto(
                category.SupplyCategoryId,
                category.Name,
                category.Description,
                category.Supplies.Count))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/supply-categories/{id:int}")]
    public async Task<ActionResult<SupplyCategoryDto>> GetSupplyCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _context.SupplyCategories.AsNoTracking()
            .Where(item => item.SupplyCategoryId == id)
            .Select(item => new SupplyCategoryDto(
                item.SupplyCategoryId, item.Name, item.Description, item.Supplies.Count))
            .FirstOrDefaultAsync(cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpGet("api/suppliers")]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetSuppliers(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(supplier => supplier.IsActive);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(supplier =>
                supplier.Name.Contains(search) ||
                (supplier.ContactPerson != null && supplier.ContactPerson.Contains(search)));
        }
        return Ok(await query.OrderBy(supplier => supplier.Name)
            .Select(supplier => new SupplierDto(
                supplier.SupplierId, supplier.Name, supplier.ContactPerson,
                supplier.PhoneNumber, supplier.Email, supplier.Address, supplier.IsActive))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/suppliers/{id:int}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(
        int id,
        CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers.AsNoTracking()
            .Where(item => item.SupplierId == id)
            .Select(item => new SupplierDto(
                item.SupplierId, item.Name, item.ContactPerson,
                item.PhoneNumber, item.Email, item.Address, item.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost("api/supply-categories")]
    public async Task<ActionResult<SupplyCategoryDto>> CreateSupplyCategory(
        SupplyCategoryWriteDto request,
        CancellationToken cancellationToken)
    {
        var category = new SupplyCategory
        {
            Name = request.Name.Trim(),
            Description = Clean(request.Description)
        };
        _context.SupplyCategories.Add(category);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create supply category.");
            return Conflict("A supply category with this name may already exist.");
        }
        return CreatedAtAction(
            nameof(GetSupplyCategory),
            new { id = category.SupplyCategoryId },
            new SupplyCategoryDto(category.SupplyCategoryId, category.Name, category.Description, 0));
    }

    [HttpPost("api/suppliers")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(
        SupplierWriteDto request,
        CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactPerson = Clean(request.ContactPerson),
            PhoneNumber = Clean(request.PhoneNumber),
            Email = Clean(request.Email),
            Address = Clean(request.Address),
            IsActive = true
        };
        _context.Suppliers.Add(supplier);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create supplier.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The supplier could not be saved.");
        }
        return CreatedAtAction(
            nameof(GetSupplier),
            new { id = supplier.SupplierId },
            new SupplierDto(
                supplier.SupplierId, supplier.Name, supplier.ContactPerson,
                supplier.PhoneNumber, supplier.Email, supplier.Address, supplier.IsActive));
    }

    [HttpGet("api/supplies/{id:int}")]
    public async Task<ActionResult<SupplyDto>> GetSupply(
        int id,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.AsNoTracking()
            .Where(item => item.SupplyId == id)
            .Select(item => new SupplyDto(
                item.SupplyId, item.SupplyCategoryId, item.Category.Name,
                item.Name, item.UnitOfMeasure, item.StockQuantity, item.IsActive, item.UnitCost))
            .FirstOrDefaultAsync(cancellationToken);
        return supply is null ? NotFound() : Ok(supply);
    }

    [HttpPost("api/supplies")]
    public async Task<ActionResult<SupplyDto>> CreateSupply(
        SupplyWriteDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Supply name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.UnitOfMeasure))
        {
            return BadRequest("Unit of measure is required.");
        }

        // Resolve category by name (create if it does not exist yet)
        var categoryName = request.CategoryName?.Trim();
        int categoryId;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var category = await _context.SupplyCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
            if (category is null)
            {
                category = new SupplyCategory { Name = categoryName };
                _context.SupplyCategories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);
            }
            categoryId = category.SupplyCategoryId;
        }
        else if (request.SupplyCategoryId.HasValue)
        {
            categoryId = request.SupplyCategoryId.Value;
        }
        else
        {
            return BadRequest("Category is required.");
        }

        var supply = new Supply
        {
            SupplyCategoryId = categoryId,
            Name = request.Name.Trim(),
            UnitOfMeasure = request.UnitOfMeasure.Trim(),
            UnitCost = request.UnitCost,
            StockQuantity = request.StockQuantity ?? 0m,
            IsActive = true
        };
        _context.Supplies.Add(supply);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create supply {SupplyName}.", request.Name);
            return Conflict("The supply could not be saved.");
        }
        var response = await GetSupply(supply.SupplyId, cancellationToken);
        return CreatedAtAction(nameof(GetSupply), new { id = supply.SupplyId }, response.Value);
    }

    [HttpPut("api/supplies/{id:int}")]
    public async Task<IActionResult> UpdateSupply(
        int id,
        SupplyWriteDto request,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id && item.IsActive, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }

        // Resolve category by name (create if it does not exist yet)
        var categoryName = request.CategoryName?.Trim();
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var category = await _context.SupplyCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
            if (category is null)
            {
                category = new SupplyCategory { Name = categoryName };
                _context.SupplyCategories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);
            }
            supply.SupplyCategoryId = category.SupplyCategoryId;
        }
        else if (request.SupplyCategoryId.HasValue)
        {
            supply.SupplyCategoryId = request.SupplyCategoryId.Value;
        }

        supply.Name = request.Name.Trim();
        supply.UnitOfMeasure = request.UnitOfMeasure.Trim();
        supply.UnitCost = request.UnitCost;
        // Stock is managed through the Add Stock action; preserve it when the
        // edit payload omits the quantity.
        if (request.StockQuantity.HasValue)
        {
            supply.StockQuantity = request.StockQuantity.Value;
        }
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update supply {SupplyId}.", id);
            return Conflict("The supply could not be updated. Check that its code is unique.");
        }
        return NoContent();
    }

    [HttpDelete("api/supplies/{id:int}")]
    public async Task<IActionResult> ArchiveSupply(
        int id,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }
        supply.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/supplies/{id:int}/restore")]
    public async Task<IActionResult> RestoreSupply(
        int id,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }
        supply.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/supplies/{id:int}/add-stock")]
    public async Task<IActionResult> AddStock(
        int id,
        AddStockRequest request,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }
        supply.StockQuantity += request.StockQuantity;
        if (supply.StockQuantity < 0) supply.StockQuantity = 0;
        supply.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("api/audit")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(log =>
                log.Action.Contains(search) ||
                log.EntityName.Contains(search) ||
                (log.EntityId != null && log.EntityId.Contains(search)));
        }
        if (from.HasValue)
        {
            query = query.Where(log => log.OccurredAtUtc >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(log => log.OccurredAtUtc <= to.Value);
        }

        return Ok(await query.OrderByDescending(log => log.OccurredAtUtc)
            .Take(500)
            .Select(log => new AuditLogDto(
                log.AuditLogId, log.UserId,
                log.User == null ? null : log.User.DisplayName,
                log.Action, log.EntityName, log.EntityId, log.OccurredAtUtc,
                log.OldValuesJson, log.NewValuesJson))
            .ToListAsync(cancellationToken));
    }

    private static string? ValidatePlantingDates(
        DateOnly plantingDate,
        DateOnly? expectedHarvestDate) =>
        expectedHarvestDate.HasValue && expectedHarvestDate.Value < plantingDate
            ? "Expected harvest date must not be earlier than the planting date."
            : null;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
