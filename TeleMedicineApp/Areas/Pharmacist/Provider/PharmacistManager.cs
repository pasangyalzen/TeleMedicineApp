using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SQLHelper;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Areas.Pharmacist.ViewModels;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Areas.Admin.Provider
{
    public class PharmacistManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PharmacistManager(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Get all pharmacists with pagination, filtering, and sorting
        public async Task<List<PharmacistDetailsViewModel>> GetAllPharmacists(int offset, int limit,
            string searchKeyword = "", string sortColumn = "CreatedAt", string sortOrder = "ASC")
        {
            try
            {
                // Instantiate SQLHandlerAsync to handle database operations
                SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        
                // Prepare the parameters to pass into the stored procedure
                IList<KeyValue> param = new List<KeyValue>
                {
                    new KeyValue("@Offset", offset),
                    new KeyValue("@Limit", limit),
                    new KeyValue("@SearchKeyword", searchKeyword),
                    new KeyValue("@SortColumn", sortColumn),
                    new KeyValue("@SortOrder", sortOrder)
                };

                // Execute the stored procedure and map the result to a list of PharmacistDetailsViewModel
                var result = await sqlHelper.ExecuteAsListAsync<PharmacistDetailsViewModel>("[dbo].[usp_GetAllPharmacists]", param);
        
                // Return the list of pharmacists
                return result;
            }
            catch (Exception ex)
            {
                // Log the error (you may use a logger here)
                Console.WriteLine($"Error retrieving pharmacists: {ex.Message}");
        
                // Optionally, return an empty list in case of failure:
                return new List<PharmacistDetailsViewModel>();
            }
        }

        // Get a specific pharmacist by ID
        public async Task<PharmacistDetailsViewModel> GetPharmacistById(int pharmacistId)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>
            {
                new KeyValue("@PharmacistId", pharmacistId)
            };

            var result = await sqlHelper.ExecuteAsListAsync<PharmacistDetailsViewModel>("[dbo].[usp_GetPharmacistById]", param);
            return result.FirstOrDefault(); // Return the first item or null if no pharmacist found
        }

        // Delete a pharmacist by userId
        public async Task<OperationResponse<string>> DeletePharmacistByUserId(string userId)
        {
            var response = new OperationResponse<string>();
            try
            {
                SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
                IList<KeyValue> param = new List<KeyValue>
                {
                    new KeyValue("@UserId", userId)
                };

                // Execute stored procedure and get the status
                var status = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_DeletePharmacistByUserId]", param);

                // Handle the different statuses returned by the stored procedure
                if (status == 0)
                {
                    response.Result = "Pharmacist deleted successfully.";
                }
                else if (status == 1)
                {
                    response.Result = "Pharmacist not found for this UserId.";
                }
                else if (status == 2)
                {
                    response.Result = "Cannot delete pharmacist. The pharmacist has existing appointments.";
                }
                else if (status == 3)
                {
                    response.Result = "There was an error deleting the pharmacist.";
                }
            }
            catch (Exception ex)
            {
                response.Result = "Error deleting pharmacist.";
            }

            return response;
        }
        
        

        // Update pharmacist details
        public async Task<bool> UpdatePharmacistDetails(PharmacistUpdateViewModel model, int pharmacistId)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

            IList<KeyValue> param = new List<KeyValue>
            {
                new KeyValue("@PharmacistId", pharmacistId),
                new KeyValue("@FullName", model.FullName ?? (object)DBNull.Value),
                new KeyValue("Email", model.Email ?? (object)DBNull.Value),
                new KeyValue("@PhoneNumber", model.PhoneNumber ?? (object)DBNull.Value),
                new KeyValue("@Gender", model.Gender ?? (object)DBNull.Value),
                new KeyValue("@DateOfBirth", model.DateOfBirth),
                new KeyValue("@PharmacyName", model.PharmacyName ?? (object)DBNull.Value),
                new KeyValue("@LicenseNumber", model.LicenseNumber ?? (object)DBNull.Value),
                new KeyValue("@PharmacyAddress", model.PharmacyAddress ?? (object)DBNull.Value),
                new KeyValue("@WorkingHours", model.WorkingHours ?? (object)DBNull.Value),
                new KeyValue("@ServicesOffered", model.ServicesOffered ?? (object)DBNull.Value),
                new KeyValue("@ProfileImage", model.ProfileImage ?? (object)DBNull.Value),
            };

            var result = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_UpdatePharmacistDetails]", param);

            if (result == 0)
            {
                Console.WriteLine("Update executed, but no rows modified. This could be because the values were already the same.");
                return true; // Consider this as a success when no changes were needed
            }

            Console.WriteLine($"Update successful, rows affected: {result}");
            return result > 0;
        }

        // Get a pharmacist's appointments (if applicable)
        public async Task<List<AppointmentUpdateViewModel>> GetPharmacistAppointments(int pharmacistId, int offset, int limit)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>
            {
                new KeyValue("@PharmacistId", pharmacistId),
                new KeyValue("@Offset", offset),
                new KeyValue("@Limit", limit)
            };

            var result = await sqlHelper.ExecuteAsListAsync<AppointmentUpdateViewModel>("[dbo].[usp_GetPharmacistAppointments]", param);
            return result;
        }

        // Get userId by pharmacistId
        public async Task<OperationResponse<string>> GetUserIdByPharmacistId(int pharmacistId)
        {
            var response = new OperationResponse<string>();
            try
            {
                SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

                IList<KeyValue> param = new List<KeyValue>
                {
                    new KeyValue("@PharmacistId", pharmacistId)
                };

                var result = await sqlHelper.ExecuteAsScalarAsync<string>("[dbo].[usp_GetUserIdByPharmacistId]", param);

                if (string.IsNullOrEmpty(result))
                {
                    response.Result = "Pharmacist not found or no userId associated with this pharmacist.";
                }
                else
                {
                    response.Result = result; // Return the userId as the result
                }
            }
            catch (Exception ex)
            {
                response.Result = "Error: " + ex.Message;
            }

            return response;
        }
    }
}