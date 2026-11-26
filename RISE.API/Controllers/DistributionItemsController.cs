using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/distribution-items")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class DistributionItemsController : ControllerBase
{
    private readonly RiseDbContext _context;

    public DistributionItemsController(RiseDbContext context)
    {
        _context = context;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _context.DistributionItems
            .Include(entry => entry.Distribution)
            .FirstOrDefaultAsync(entry => entry.DistributionItemId == id, cancellationToken);
        if (item is null) return NotFound();
        if (item.Distribution.Status == "Cancelled")
            return Conflict("This distribution is already cancelled.");

        var hasOtherRecords = await _context.SupplyUsageRecords.AnyAsync(record => record.DistributionItemId == id, cancellationToken)
            || await _context.PlantingRecords.AnyAsync(record => record.SeedDistributionItemId == id, cancellationToken);
        if (hasOtherRecords)
            return Conflict("This item has related planting or usage records and cannot be deleted.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (item.Distribution.Status == "Distributed")
        {
            await _context.Supplies
                .Where(supply => supply.SupplyId == item.SupplyId && supply.IsActive)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    supply => supply.StockQuantity,
                    supply => supply.StockQuantity + item.Quantity), cancellationToken);
        }

        var monitorings = await _context.UtilizationMonitorings
            .Where(record => record.DistributionItemId == id).ToListAsync(cancellationToken);
        _context.UtilizationMonitorings.RemoveRange(monitorings);
        var archived = await _context.UtilizationSeasonRecords
            .Where(record => record.DistributionItemId == id).ToListAsync(cancellationToken);
        _context.UtilizationSeasonRecords.RemoveRange(archived);
        _context.DistributionItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}
