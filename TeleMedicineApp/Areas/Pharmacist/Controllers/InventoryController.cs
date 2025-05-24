using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TeleMedicineApp.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using TeleMedicineApp.Areas.Pharmacist.Models;
using System.ComponentModel.DataAnnotations;

namespace TeleMedicineApp.Areas.Pharmacist.Controllers
{
    [Authorize(Roles = "SuperAdmin, Pharmacist, Doctor")]
    [Area("Pharmacist")]
    [Route("api/[area]/[action]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(ApplicationDbContext context, ILogger<InventoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllInventory()
        {
            var items = await _context.MedicineInventory
                .OrderByDescending(i => i.UpdatedAt)
                .ToListAsync();

            if (!items.Any())
                return NotFound("No inventory items found.");

            return Ok(items);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInventoryById(int id)
        {
            var item = await _context.MedicineInventory.FindAsync(id);
            return item == null ? NotFound("Inventory item not found.") : Ok(item);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrAddInventory([FromBody] MedicineInventory newItem)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingItem = await _context.MedicineInventory.FirstOrDefaultAsync(i =>
                    i.MedicineName == newItem.MedicineName &&
                    i.BatchNumber == newItem.BatchNumber &&
                    i.ExpiryDate == newItem.ExpiryDate &&
                    i.Manufacturer == newItem.Manufacturer);

                if (existingItem != null)
                {
                    existingItem.Quantity += newItem.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    _context.MedicineInventory.Update(existingItem);
                }
                else
                {
                    newItem.CreatedAt = DateTime.UtcNow;
                    newItem.UpdatedAt = DateTime.UtcNow;
                    _context.MedicineInventory.Add(newItem);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = existingItem != null ? "Inventory updated successfully." : "Inventory created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating inventory item");
                return StatusCode(500, "An error occurred while creating/updating inventory item.");
            }
        }

        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryUpdateDTO updatedItem)
        {
            if (id != updatedItem.InventoryId)
                return BadRequest("Inventory ID mismatch.");

            var existingItem = await _context.MedicineInventory.FindAsync(id);
            if (existingItem == null)
                return NotFound("Inventory item not found.");

            existingItem.MedicineName = updatedItem.MedicineName;
            existingItem.Quantity = updatedItem.Quantity;
            existingItem.BatchNumber = updatedItem.BatchNumber;
            existingItem.ExpiryDate = updatedItem.ExpiryDate;
            existingItem.Manufacturer = updatedItem.Manufacturer;
            existingItem.UnitPrice = updatedItem.UnitPrice;
            existingItem.UpdatedAt = DateTime.UtcNow;

            _context.MedicineInventory.Update(existingItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inventory item updated successfully." });
        }

        [HttpDelete("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid inventory ID.");

            var existingItem = await _context.MedicineInventory.FindAsync(id);
            if (existingItem == null)
                return NotFound("Inventory item not found.");

            _context.MedicineInventory.Remove(existingItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inventory item deleted successfully." });
        }

        [HttpPatch("{id}/quantity")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] QuantityChangeDTO qtyChange)
        {
            if (qtyChange == null)
                return BadRequest("Invalid quantity change data.");

            var item = await _context.MedicineInventory.FindAsync(id);
            if (item == null)
                return NotFound("Inventory item not found.");

            item.Quantity += qtyChange.QuantityChange;
            if (item.Quantity < 0)
                item.Quantity = 0;

            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(item);
        }
    }

    public class QuantityChangeDTO
    {
        [Range(-100000, 100000, ErrorMessage = "Quantity change must be between -100000 and 100000.")]
        public int QuantityChange { get; set; }
    }

    public class InventoryUpdateDTO
    {
        [Required]
        public int InventoryId { get; set; }

        [Required]
        [StringLength(150)]
        public string MedicineName { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(50)]
        public string BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(150)]
        public string Manufacturer { get; set; }

        [Required]
        [Range(0.0, 999999.99)]
        public decimal UnitPrice { get; set; }
    }
}