using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ms_course_logitrack.Data;
using ms_course_logitrack.Models;

namespace ms_course_logitrack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private const string InventoryCacheKey = "inventory-items";
        private readonly LogiTrackContext _context;
        private readonly IMemoryCache _cache;

        public InventoryController(LogiTrackContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllInventoryItems()
        {
            var stopwatch = Stopwatch.StartNew();
            var cacheHit = _cache.TryGetValue(InventoryCacheKey, out List<InventoryItem>? items);

            if (!cacheHit)
            {
                items = await _context.InventoryItems
                    .AsNoTracking()
                    .ToListAsync();

                _cache.Set(
                    InventoryCacheKey,
                    items,
                    TimeSpan.FromSeconds(30));
            }

            stopwatch.Stop();
            Response.Headers["X-Cache"] = cacheHit ? "HIT" : "MISS";
            Response.Headers["X-Elapsed-Milliseconds"] = stopwatch.Elapsed.TotalMilliseconds
                .ToString("F3", CultureInfo.InvariantCulture);

            return Ok(items);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<InventoryItem>> AddInventoryItem(CreateInventoryItemRequest request)
        {
            var item = new InventoryItem
            {
                Name = request.Name,
                Quantity = request.Quantity,
                Location = request.Location
            };

            await _context.InventoryItems.AddAsync(item);
            await _context.SaveChangesAsync();
            _cache.Remove(InventoryCacheKey);

            return CreatedAtAction(nameof(GetAllInventoryItems), item);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteInventoryItem(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);

            if (item == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Inventory item not found",
                    detail: $"No inventory item with ID {id} was found.");
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
            _cache.Remove(InventoryCacheKey);

            return NoContent();
        }
    }
}
