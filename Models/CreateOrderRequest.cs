using System.ComponentModel.DataAnnotations;

namespace ms_course_logitrack.Models
{
    public class CreateOrderRequest
    {
        [Required]
        public required string CustomerName { get; set; }

        [Required]
        public DateTime? DatePlaced { get; set; }

        [Required]
        [MinLength(1)]
        public required List<CreateOrderItemRequest> Items { get; set; }
    }

    public class CreateOrderItemRequest
    {
        [Required]
        public required string Name { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public required string Location { get; set; }
    }
}