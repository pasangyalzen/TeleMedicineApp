using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleMedicineApp.Areas.Admin.Provider;
using TeleMedicineApp.Data;
using TeleMedicineApp.Controllers;

namespace TeleMedicineApp.Areas.Admin.Controllers
{
    [AllowAnonymous]
    [Area("admin")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class PatientDashboardController : ApiControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly DoctorManager _doctorManager;
        

        public PatientDashboardController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext =
                applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            _doctorManager = new DoctorManager();
        }

        [AllowAnonymous]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string search = "", string sortColumn = "FullName", string sortOrder = "DESC", int page = 1, int pageSize = 5)
        {
            try
            {
                // Fetch all doctors (without applying search filter initially)
                var allDoctors = await _doctorManager.GetTotalDoctors(0, int.MaxValue, "", sortColumn, sortOrder);
        
                // Apply search filter if search keyword is provided
                if (!string.IsNullOrWhiteSpace(search))
                {
                    allDoctors = allDoctors
                        .Where(c => c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Count total number of items
                var totalItems = allDoctors.Count();

                // Apply pagination
                var paginatedDoctors = allDoctors.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Calculate total pages
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                return ApiResponse(new
                {
                    services = paginatedDoctors,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages,
                        sortColumn,
                        sortOrder,
                        search
                    }
                }, "Services retrieved successfully");
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                //_logger.LogError(ex, "Error occurred while fetching doctors.");
        
                return ApiError("Failed to retrieve services", statusCode: 500);
            }
        }

    }
}
