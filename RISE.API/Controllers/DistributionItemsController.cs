using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Services;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/distribution-items")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class DistributionItemsController : ControllerBase
{
    private int OwnerId => OwnerScope.UserId(User);

    private readonly RiseDbContext _context;

    public DistributionItemsController(RiseDbContext context)
    {
        _context = context;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _context.DistributionItems
            .Include(entry => entry.Distribution).ThenInclude(entry => entry.Farmer)
            .FirstOrDefaultAsync(entry => entry.DistributionItemId == id &&
                entry.Distribution.Farmer.OwnerUserId == OwnerId, cancellationToken);
        if (item is null) return NotFound();
        if (item.Distribution.Status == "Cancelled")
            return Conflict("This distribution is already cancelled.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (item.Distribution.Status == "Distributed")
        {
            await _context.Supplies
                .Where(supply => supply.SupplyId == item.SupplyId && supply.OwnerUserId == OwnerId && supply.IsActive)
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
