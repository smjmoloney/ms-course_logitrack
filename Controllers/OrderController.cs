using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ms_course_logitrack.Data;
using ms_course_logitrack.Models;

namespace ms_course_logitrack.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly LogiTrackContext _context;

        public OrderController(LogiTrackContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrderById(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .FirstOrDefaultAsync(order => order.OrderId == id);

            if (order == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Order not found",
                    detail: $"No order with ID {id} was found.");
            }

            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Order not found",
                    detail: $"No order with ID {id} was found.");
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
