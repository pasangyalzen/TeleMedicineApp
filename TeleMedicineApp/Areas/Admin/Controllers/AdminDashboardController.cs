// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using TeleMedicineApp.Data;
// using TeleMedicineApp.Controllers;
//
// namespace TeleMedicineApp.Areas.Admin.Controllers
// {
//     [AllowAnonymous]
//     [Area("admin")]
//     [Route("api/[area]/[controller]")]
//     [ApiController]
//     public class AdminDashBoardController : ApiControllerBase
//     {
//         private readonly ApplicationDbContext _applicationDbContext;
//
//         public AdminDashBoardController(ApplicationDbContext applicationDbContext)
//         {
//             _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
//         }
//
//         // --------------- Get Total Users with (user) Role
//         [HttpGet("GetUserCount")]
//         public async Task<IActionResult> GetTotalUsers()
//         {
//             try
//             {
//                 // Count the total number of users
//                 var allUsersCount = await _applicationDbContext.Users.CountAsync();
//
//                 // Count the users for each specific role
//                 var adminCount = await (from user in _applicationDbContext.Users
//                                          join userRole in _applicationDbContext.UserRoles on user.Id equals userRole.UserId
//                                          join role in _applicationDbContext.Roles on userRole.RoleId equals role.Id
//                                          where role.Name == "admin"
//                                          select user).CountAsync();
//
//                 var doctorCount = await (from user in _applicationDbContext.Users
//                                           join userRole in _applicationDbContext.UserRoles on user.Id equals userRole.UserId
//                                           join role in _applicationDbContext.Roles on userRole.RoleId equals role.Id
//                                           where role.Name == "doctor"
//                                           select user).CountAsync();
//
//                 var patientCount = await (from user in _applicationDbContext.Users
//                                           join userRole in _applicationDbContext.UserRoles on user.Id equals userRole.UserId
//                                           join role in _applicationDbContext.Roles on userRole.RoleId equals role.Id
//                                           where role.Name == "patient"
//                                           select user).CountAsync();
//
//                 var pharmacistCount = await (from user in _applicationDbContext.Users
//                                               join userRole in _applicationDbContext.UserRoles on user.Id equals userRole.UserId
//                                               join role in _applicationDbContext.Roles on userRole.RoleId equals role.Id
//                                               where role.Name == "pharmacist"
//                                               select user).CountAsync();
//
//                 // Return the counts as part of the response
//                 return ApiResponse(new
//                 {
//                     TotalUsers = allUsersCount,
//                     AdminUsers = adminCount,
//                     DoctorUsers = doctorCount,
//                     PatientUsers = patientCount,
//                     PharmacistUsers = pharmacistCount
//                 });
//             }
//             catch (Exception ex)
//             {
//                 return ApiError("Failed to get user count");
//             }
//         }
//         
//         [HttpDelete("DeleteUserById/{userId}")]
//         public async Task<IActionResult> DeleteUserById(string userId)
//         {
//             try
//             {
//                 // Fetch the user from the database using the provided UserId
//                 var user = await _applicationDbContext.Users
//                     .FirstOrDefaultAsync(u => u.Id == userId);
//
//                 // If the user is not found, return a Not Found response
//                 if (user == null)
//                 {
//                     return NotFound(new { message = "User not found" });
//                 }
//
//                 // Fetch the user roles
//                 var userRoles = await _applicationDbContext.UserRoles
//                     .Where(ur => ur.UserId == userId)
//                     .ToListAsync();
//
//                 // Delete user roles before deleting the user
//                 _applicationDbContext.UserRoles.RemoveRange(userRoles);
//
//                 // Delete the user from the Users table
//                 _applicationDbContext.Users.Remove(user);
//
//                 // Commit the transaction
//                 await _applicationDbContext.SaveChangesAsync();
//
//                 return Ok(new { message = "User deleted successfully" });
//             }
//             catch (Exception ex)
//             {
//                 // Handle any exceptions and return a bad request with an error message
//                 return StatusCode(500, new { message = "Failed to delete the user", error = ex.Message });
//             }
//         }
//
//     }
// }
