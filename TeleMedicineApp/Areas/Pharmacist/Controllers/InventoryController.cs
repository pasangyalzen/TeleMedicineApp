using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TeleMedicineApp.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using TeleMedicineApp.Areas.Pharmacist.Models;

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
            try
            {
                var items = await _context.MedicineInventory
                    .OrderByDescending(i => i.UpdatedAt)
                    .ToListAsync();

                if (!items.Any())
                    return NotFound("No inventory items found.");

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory items");
                return StatusCode(500, "An error occurred while fetching inventory items.");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInventoryById(int id)
        {
            try
            {
                var item = await _context.MedicineInventory.FindAsync(id);
                if (item == null)
                    return NotFound("Inventory item not found.");

                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory item by id");
                return StatusCode(500, "An error occurred while fetching inventory item.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateInventory([FromBody] MedicineInventory newItem)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                newItem.CreatedAt = DateTime.UtcNow;
                newItem.UpdatedAt = DateTime.UtcNow;

                _context.MedicineInventory.Add(newItem);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInventoryById), new { id = newItem.InventoryId }, newItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory item");
                return StatusCode(500, "An error occurred while creating inventory item.");
            }
        }

        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryUpdateDTO updatedItem)
        {
            if (id != updatedItem.InventoryId)
                return BadRequest("Inventory ID mismatch.");

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory item");
                return StatusCode(500, "An error occurred while updating inventory item.");
            }
        }

        [HttpDelete("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid inventory ID.");

            try
            {
                var existingItem = await _context.MedicineInventory.FindAsync(id);
                if (existingItem == null)
                    return NotFound("Inventory item not found.");

                _context.MedicineInventory.Remove(existingItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Inventory item deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory item");
                return StatusCode(500, "An error occurred while deleting inventory item.");
            }
        }

        [HttpPatch("{id}/quantity")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] QuantityChangeDTO qtyChange)
        {
            if (qtyChange == null)
                return BadRequest("Invalid quantity change data.");

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory quantity");
                return StatusCode(500, "An error occurred while updating inventory quantity.");
            }
        }
    }

    public class QuantityChangeDTO
    {
        public int QuantityChange { get; set; }
    }
}