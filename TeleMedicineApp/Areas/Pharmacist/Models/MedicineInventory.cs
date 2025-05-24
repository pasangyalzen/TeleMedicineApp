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

        [Required(ErrorMessage = "Medicine name is required.")]
        [StringLength(150, ErrorMessage = "Medicine name cannot exceed 150 characters.")]
        public string MedicineName { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or a positive number.")]
        public int Quantity { get; set; }

        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters.")]
        public string BatchNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(150, ErrorMessage = "Manufacturer name cannot exceed 150 characters.")]
        public string Manufacturer { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Unit price must be a positive value.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}