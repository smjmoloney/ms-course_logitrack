using System.ComponentModel.DataAnnotations;

namespace ms_course_logitrack.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public DateTime DatePlaced { get; set; }
        [Required]
        public List<InventoryItem> Items { get; set; } = new List<InventoryItem>();

        public void AddItem(InventoryItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Items.Add(item);
        }

        public void RemoveItem(int itemId)
        {
            Items.RemoveAll(item => item.ItemId == itemId);
        }

        public string GetOrderSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Order #{OrderId}");
            summary.AppendLine($"Customer: {CustomerName}");
            summary.AppendLine($"Date Placed: {DatePlaced:yyyy-MM-dd}");
            summary.AppendLine($"Items in Order: {Items.Count}");
            summary.Append($"Total Quantity: {Items.Sum(item => item.Quantity)}");
            return summary.ToString();
        }
    }
}
