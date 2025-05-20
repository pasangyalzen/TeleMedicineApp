using TeleMedicineApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SQLHelper;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Doctor.ViewModels;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Areas.Admin.Provider
{
    public class DoctorManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DoctorManager(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
            
        }

        public async Task<List<DoctorDetailsViewModel>> GetTotalDoctors(int offset, int limit,
            string searchKeyword = "", string sortColumn = "CreatedAt", string sortOrder = "ASC")
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>();

            param.Add(new KeyValue("@Offset", offset));
            param.Add(new KeyValue("@Limit", limit));
            param.Add(new KeyValue("@SearchKeyword", searchKeyword));
            param.Add(new KeyValue("@SortColumn", sortColumn));
            param.Add(new KeyValue("@SortOrder", sortOrder));


            // Your SQL query execution goes here, e.g.,:
            var result =
                await sqlHelper.ExecuteAsListAsync<DoctorDetailsViewModel>("[dbo].[usp_Doctors_GetDoctorList]", param);
            return result;

        }

        public async Task<DoctorDetailsViewModel> GetDoctorById(int doctorId)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>();
            param.Add(new KeyValue("@DoctorId", doctorId));
    
            // Use ExecuteAsListAsync to get the result as a list
            var result = await sqlHelper.ExecuteAsListAsync<DoctorDetailsViewModel>("[dbo].[usp_Doctors_GetDoctorById]", param);
    
            // Return the first item from the list or null if no doctor is found
            return result.FirstOrDefault(); // Will return null if no doctor is found
        }

        //     public async Task<OperationResponse<string>> CompleteDoctorDetails(DoctorDetailsViewModel model)
        //     {
        //         OperationResponse<string> response = new OperationResponse<string>();
        //
        //         // Check if a user with the given email already exists
        //         var user = await _userManager.FindByEmailAsync(model.Email);
        //         if (user != null)
        //         {
        //             response.Result = "User already exists";
        //             return response;
        //         }
        //
        //         // Step 1: Create a new user in AspNetUsers
        //         user = new ApplicationUser
        //         {
        //             UserName = model.Email,
        //             Email = model.Email
        //         };
        //
        //         var createUserResult = await _userManager.CreateAsync(user, model.Password);
        //         if (!createUserResult.Succeeded)
        //         {
        //             response.Result = "Error creating user: " +
        //                               string.Join(", ", createUserResult.Errors.Select(e => e.Description));
        //             return response;
        //         }
        //
        //         // Step 2: Assign the "Doctor" role to the user
        //         await _userManager.AddToRoleAsync(user, "Doctor");
        //
        //         // Step 3: Insert new doctor details
        //         SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        //         IList<KeyValue> param = new List<KeyValue>
        //         {
        //             new KeyValue("@Email", model.Email),
        //             new KeyValue("@FullName", model.FullName),
        //             new KeyValue("@PhoneNumber", model.PhoneNumber),
        //             new KeyValue("@Gender", model.Gender),
        //             new KeyValue("@DateOfBirth", model.DateOfBirth),
        //             new KeyValue("@LicenseNumber", model.LicenseNumber),
        //             new KeyValue("@MedicalCollege", model.MedicalCollege),
        //             new KeyValue("@Specialization", model.Specialization),
        //             new KeyValue("@YearsOfExperience", model.YearsOfExperience),
        //             new KeyValue("@ClinicName", model.ClinicName),
        //             new KeyValue("@ClinicAddress", model.ClinicAddress),
        //             new KeyValue("@ConsultationFee", model.ConsultationFee),
        //             new KeyValue("@ProfileImage",
        //                 string.IsNullOrEmpty(model.ProfileImage) ? DBNull.Value : (object)model.ProfileImage),
        //             new KeyValue("@CreatedAt", DateTime.UtcNow)
        //         };
        //
        //         // Execute stored procedure
        //         int opStatus =
        //             await sqlHelper.ExecuteNonQueryAsync("[dbo].[usp_CompleteDoctorDetails]", param, "@OpStatus");
        //
        //         if (opStatus > 0)
        //         {
        //             response.Result = "Doctor created successfully";
        //         }
        //         else
        //         {
        //             response.Result = "Error: Doctor not created";
        //         }
        //
        //         return response;
        //     }
        // }
        public async Task<OperationResponse<string>> DeleteDoctor(string userId)
        {
            var response = new OperationResponse<string>();

            try
            {
                SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
                IList<KeyValue> param = new List<KeyValue>
                {
                    new KeyValue("@UserId", userId)
                };

                // Execute the stored procedure and get the result
                int opStatus = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_DeleteDoctorDetails]", param);

                if (opStatus == 1)
                {
                    response.Result = "Doctor deleted successfully.";
                }
                else
                {
                    response.Result = "Error: Doctor deletion failed. Either the doctor was not found, or the user was not deleted.";
                }
            }
            catch (Exception ex)
            {
                response.Result = "Error: " + ex.Message;
            }

            return response;
        }
    
        public async Task<bool> UpdateDoctor(DoctorDetailsViewModel model, int doctorId)
{
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

        // Prepare parameters to pass to the stored procedure
        IList<KeyValue> param = new List<KeyValue>()
        {
            new KeyValue("@DoctorId", doctorId),
            new KeyValue("@FullName", string.IsNullOrEmpty(model.FullName) ? (object)DBNull.Value : model.FullName),
            new KeyValue("@Email", string.IsNullOrEmpty(model.Email) ? (object)DBNull.Value : model.Email),
            new KeyValue("@PhoneNumber", string.IsNullOrEmpty(model.PhoneNumber) ? (object)DBNull.Value : model.PhoneNumber),
            new KeyValue("@Gender", string.IsNullOrEmpty(model.Gender) ? (object)DBNull.Value : model.Gender),
            new KeyValue("@DateOfBirth", model.DateOfBirth ?? (object)DBNull.Value),
            new KeyValue("@LicenseNumber", string.IsNullOrEmpty(model.LicenseNumber) ? (object)DBNull.Value : model.LicenseNumber),
            new KeyValue("@MedicalCollege", string.IsNullOrEmpty(model.MedicalCollege) ? (object)DBNull.Value : model.MedicalCollege),
            new KeyValue("@Specialization", string.IsNullOrEmpty(model.Specialization) ? (object)DBNull.Value : model.Specialization),
            new KeyValue("@YearsOfExperience", model.YearsOfExperience ?? (object)DBNull.Value),
            new KeyValue("@ClinicName", string.IsNullOrEmpty(model.ClinicName) ? (object)DBNull.Value : model.ClinicName),
            new KeyValue("@ClinicAddress", string.IsNullOrEmpty(model.ClinicAddress) ? (object)DBNull.Value : model.ClinicAddress),
            new KeyValue("@ConsultationFee", model.ConsultationFee ?? (object)DBNull.Value),
            new KeyValue("@UpdatedAt", DateTime.Now)
        };

        // Execute stored procedure
        var result = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_UpdateDoctorDetails]", param);

        // Check if any rows were affected
        if (result == 0)
        {
            Console.WriteLine("Update executed, but no rows modified. This could be because the values were already the same.");
            return true; // Consider this as a success when no changes were needed
        }

        Console.WriteLine($"Update successful, rows affected: {result}");
        return result > 0;
    }

        [HttpGet]
        public async Task<List<AppointmentUpdateViewModel>>GetDoctorAppointments(int doctorId, int offset, int limit)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>()
            {
                new KeyValue("@DoctorId", doctorId),
                new KeyValue("Offset", offset),
                new KeyValue("@Limit", limit),
            };
            var result = await sqlHelper.ExecuteAsListAsync<AppointmentUpdateViewModel>("[dbo].[usp_GetDoctorAppointments]", param);
            return result;

        }
        public async Task<OperationResponse<string>> GetUserIdByDoctorId(int doctorId)
        {
            var response = new OperationResponse<string>();
            try
            {
                SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

                // Prepare the parameters for the stored procedure
                IList<KeyValue> param = new List<KeyValue>
                {
                    new KeyValue("@DoctorId", doctorId)
                };

                // Execute the stored procedure to get the userId
                var result = await sqlHelper.ExecuteAsScalarAsync<string>("[dbo].[usp_GetUserIdByDoctorId]", param);

                // If result is null, return an appropriate error message
                if (string.IsNullOrEmpty(result))
                {
                    response.Result = "Doctor not found or no userId associated with this doctor.";
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