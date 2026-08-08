using System.ComponentModel.DataAnnotations;

namespace ms_course_logitrack.Models
{
    public class InventoryItem
    {
        [Key]
        public int ItemId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public string Location { get; set; }

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public string DisplayInfo()
        {
            return $"Item: {Name} (ID: {ItemId}) | Quantity: {Quantity} | Location: {Location}";
        }
    }
}
