using System.Data;
using Microsoft.AspNetCore.Mvc;
using SQLHelper;
using TeleMedicineApp.Areas.Admin.Models;
using TeleMedicineApp.Areas.Admin.ViewModels;

namespace TeleMedicineApp.Areas.Admin.Provider;

public class AppointmentManager
{
    private readonly DoctorManager _doctorManager; // Declare the dependency

    // Constructor with DoctorManager injection
    public AppointmentManager(DoctorManager doctorManager)
    {
        _doctorManager = doctorManager; // Initialize the dependency
    }
    [HttpGet]
    public async Task<List<AppointmentUpdateViewModel>> GetTotalAppointments(int offset, int limit,
        string searchKeyword = "", string sortColumn = "CreatedAt", string sortOrder = "ASC")
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>();
        param.Add(new KeyValue("@Offset", offset));
        param.Add(new KeyValue("@Limit", limit));
        param.Add(new KeyValue("@SearchKeyword", searchKeyword));
        param.Add(new KeyValue("@SortColumn", sortColumn));
        param.Add(new KeyValue("@SortOrder", sortOrder));
        
        var result = await sqlHelper.ExecuteAsListAsync<AppointmentUpdateViewModel>("[dbo].[usp_GetTotalAppointments]", param);
        return result;

    }


    [HttpGet]
    public async Task<AppointmentDetailsViewModel> GetAppointmentById(int userId)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>();
        param.Add(new KeyValue("@AppointmentId", userId));
        var result = await sqlHelper.ExecuteAsListAsync<AppointmentDetailsViewModel>("[dbo].[usp_GetAppointmentById]", param);
        return result.FirstOrDefault();;

    }
    

    public async Task<String> DeleteAppointment(int id)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

        IList<KeyValue> param = new List<KeyValue>()
        {
            new KeyValue("@AppointmentId", id),
        };

        var result = await sqlHelper.ExecuteAsScalarAsync<string>("[dbo].[DeleteAppointmentById]", param);
        return result;
    }

    public async Task<bool> UpdateAppointment(int appointmentId, AppointmentUpdateViewModel updatedAppointment)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();

        IList<KeyValue> param = new List<KeyValue>()
        {
            new KeyValue("@AppointmentId", appointmentId),
            new KeyValue("@DoctorName", updatedAppointment.DoctorName),
            new KeyValue("@PatientName", updatedAppointment.PatientName),
            new KeyValue("@Status", updatedAppointment.Status),
            new KeyValue("@ScheduledTime", updatedAppointment.ScheduledTime),
            new KeyValue("@VideoCallLink", updatedAppointment.VideoCallLink),
        };

        // Execute the stored procedure
        var result = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_UpdateAppointment]", param);

        if (result == 0)
        {
            Console.WriteLine("Update executed, but no rows modified. This could be because the values were already the same.");
            return true; // ✅ Treat this as a success instead of failure
        }

        Console.WriteLine($"Update successful, rows affected: {result}");
        return result > 0;
    }

    public async Task<OperationResponse<string>> CreateAppointment(AppointmentDetailsViewModel appointment, string username)
    {
        var response = new OperationResponse<string>();
        try
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> param = new List<KeyValue>();

            // Prepare parameters for the stored procedure
            param.Add(new KeyValue("@DoctorName", appointment.DoctorName));
            param.Add(new KeyValue("@PatientName", appointment.PatientName));
            param.Add(new KeyValue("@ScheduledTime", appointment.ScheduledTime));
            param.Add(new KeyValue("@VideoCallLink", appointment.VideoCallLink));
            param.Add(new KeyValue("@AddedBy", username));

            // Execute the stored procedure and retrieve the newly created appointment ID
            var appointmentId = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_CreateAppointment]", param);

            // Check if appointment creation was successful
            if (appointmentId > 0)
            {
                response.Result = $"Appointment Created Successfully with ID: {appointmentId}";
                response.ResultMessage = "Success";
            }
            else
            {
                response.AddError("Failed to create appointment.");
            }
        }
        catch (Exception ex)
        {
            response.AddError($"Exception: {ex.Message}");
        }

        return response;
    }
    public async Task<List<AppointmentDetailsViewModel>> GetAppointmentsByDoctorUserId(string userId)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>
        {
            new KeyValue("@UserId", userId)
        };

        // Execute the stored procedure to get appointments by userId (doctor)
        var result = await sqlHelper.ExecuteAsListAsync<AppointmentDetailsViewModel>("[dbo].[usp_GetAppointmentsByDoctorUserId]", param);
        return result;
    }
    public async Task<bool> HasAppointments(int doctorId)
    {
        // Get the userId using the doctorId
        var response = await _doctorManager.GetUserIdByDoctorId(doctorId);
        string userId = response.Result;

        if (string.IsNullOrEmpty(userId))
        {
            return false; // Return false if no userId is found for the given doctorId
        }

        // Fetch all appointments for the given userId
        var appointments = await GetAppointmentsByDoctorUserId(userId);

        // Check if there are any appointments for the given userId (doctor)
        return appointments != null && appointments.Any();
    }
}