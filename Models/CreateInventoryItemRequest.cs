using System.ComponentModel.DataAnnotations;

namespace ms_course_logitrack.Models
{
    public class CreateInventoryItemRequest
    {
        [Required]
        public required string Name { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public required string Location { get; set; }
    }
}