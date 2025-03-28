using TeleMedicineApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLHelper;
using TeleMedicineApp.Areas.Admin.ViewModels;
using TeleMedicineApp.Areas.Patient.Models;
using TeleMedicineApp.Data;
using TeleMedicineApp.Models;

namespace TeleMedicineApp.Areas.Admin.Provider
{
    public class PatientManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PatientManager(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Get all patients with pagination, filtering, and sorting
        public async Task<List<PatientDetailsViewModel>> GetAllPatients(int offset, int limit,
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

                // Execute the stored procedure and map the result to a list of RegisterPatientDTO
                var result = await sqlHelper.ExecuteAsListAsync<PatientDetailsViewModel>("[dbo].[usp_GetAllPatients]", param);
        
                // Return the list of patients
                return result;
            }
            catch (Exception ex)
            {
                // Log the error (you may use a logger like ILogger here)
                Console.WriteLine($"Error retrieving patients: {ex.Message}");
        
                // Optionally, throw a custom exception or return an empty list
                // For example, you could return an empty list in case of failure:
                return new List<PatientDetailsViewModel>();
            }
        }

        // Get a specific patient by ID
        public async Task<PatientDetailsViewModel> GetPatientById(int patientId)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>
            {
                new KeyValue("@PatientId", patientId)
            };

            var result = await sqlHelper.ExecuteAsListAsync<PatientDetailsViewModel>("[dbo].[usp_GetPatientById]", param);
            return result.FirstOrDefault(); // Return the first item or null if no patient found
        }

        // Delete a patient by userId
        public async Task<OperationResponse<string>> DeletePatientByUserId(string userId)
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
                var status = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_DeletePatientByUserId]", param);

                // Handle the different statuses returned by the stored procedure
                if (status == 0)
                {
                    response.Result = "Patient deleted successfully.";
                }
                else if (status == 1)
                {
                    response.Result = "Patient not found for this UserId.";
                }
                else if (status == 2)
                {
                    response.Result = "Cannot delete patient. The patient has existing appointments.";
                }
                else if (status == 3)
                {
                    response.Result = "Error during patient deletion.";
                }
            }
            catch (Exception ex)
            {
                response.Result = "Error deleting patient.";
                
            }

            return response;
        }

        // Update patient details
        public async Task<bool> UpdatePatient(PatientUpdateViewModel model, int patientId)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

            IList<KeyValue> param = new List<KeyValue>
            {
                new KeyValue("@PatientId", patientId),
                new KeyValue("@FullName", string.IsNullOrEmpty(model.FullName) ? (object)DBNull.Value : model.FullName),
                new KeyValue("@PhoneNumber", string.IsNullOrEmpty(model.PhoneNumber) ? (object)DBNull.Value : model.PhoneNumber),
                new KeyValue("@Gender", string.IsNullOrEmpty(model.Gender) ? (object)DBNull.Value : model.Gender),
                new KeyValue("@DateOfBirth", model.DateOfBirth ?? (object)DBNull.Value),
                new KeyValue("@BloodGroup", string.IsNullOrEmpty(model.BloodGroup) ? (object)DBNull.Value : model.BloodGroup),
                new KeyValue("@EmergencyContactName", string.IsNullOrEmpty(model.EmergencyContactName) ? (object)DBNull.Value : model.EmergencyContactName),
                new KeyValue("@EmergencyContactNumber", string.IsNullOrEmpty(model.EmergencyContactNumber) ? (object)DBNull.Value : model.EmergencyContactNumber),
                new KeyValue("@MedicalHistory", string.IsNullOrEmpty(model.MedicalHistory) ? (object)DBNull.Value : model.MedicalHistory),
                new KeyValue("@UpdatedAt", DateTime.Now)
            };

            var result = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_UpdatePatientDetails]", param);

            return result > 0;
        }

        // Get a patient's appointments
        public async Task<List<AppointmentUpdateViewModel>> GetPatientAppointments(int patientId, int offset, int limit)
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>
            {
                new KeyValue("@PatientId", patientId),
                new KeyValue("@Offset", offset),
                new KeyValue("@Limit", limit)
            };

            var result = await sqlHelper.ExecuteAsListAsync<AppointmentUpdateViewModel>("[dbo].[usp_GetPatientAppointments]", param);
            return result;
        }

        // Get userId by patientId
        public async Task<OperationResponse<string>> GetUserIdByPatientId(int patientId)
        {
            var response = new OperationResponse<string>();
            try
            {
                SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

                IList<KeyValue> param = new List<KeyValue>
                {
                    new KeyValue("@PatientId", patientId)
                };

                var result = await sqlHelper.ExecuteAsScalarAsync<string>("[dbo].[usp_GetUserIdByPatientId]", param);

                if (string.IsNullOrEmpty(result))
                {
                    response.Result = "Patient not found or no userId associated with this patient.";
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