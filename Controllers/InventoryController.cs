using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ms_course_logitrack.Data;
using ms_course_logitrack.Models;

namespace ms_course_logitrack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly LogiTrackContext _context;

        public InventoryController(LogiTrackContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllInventoryItems()
        {
            var items = await _context.InventoryItems
                .AsNoTracking()
                .ToListAsync();

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

            return NoContent();
        }
    }
}
