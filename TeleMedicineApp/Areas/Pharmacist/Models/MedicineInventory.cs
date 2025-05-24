using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeleMedicineApp.Areas.Pharmacist.Models
{
    public class MedicineInventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InventoryId { get; set; }

        [Required]
        [MaxLength(150)]
        public string MedicineName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [MaxLength(50)]
        public string BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(150)]
        public string Manufacturer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}