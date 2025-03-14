using Microsoft.AspNetCore.Mvc;
using SQLHelper;
using TeleMedicineApp.Areas.Admin.Models;
using TeleMedicineApp.Areas.Admin.ViewModels;

namespace TeleMedicineApp.Areas.Admin.Provider;

public class AppointmentManager
{
    [HttpGet]
    public async Task<List<AppointmentDetailsViewModel>> GetTotalAppointments(int offset, int limit,
        string searchKeyword = "", string sortColumn = "CreatedAt", string sortOrder = "ASC")
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>();
        param.Add(new KeyValue("@Offset", offset));
        param.Add(new KeyValue("@Limit", limit));
        param.Add(new KeyValue("@SearchKeyword", searchKeyword));
        param.Add(new KeyValue("@SortColumn", sortColumn));
        param.Add(new KeyValue("@SortOrder", sortOrder));
        
        var result = await sqlHelper.ExecuteAsListAsync<AppointmentDetailsViewModel>("[dbo].[usp_GetTotalAppointments]", param);
        return result;

    }


    [HttpGet]
    public async Task<AppointmentDetailsViewModel> GetAppointmentById(int id)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>();
        param.Add(new KeyValue("@AppointmentId", id));
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
            new KeyValue("@ScheduledTime", updatedAppointment.ScheduledTime ?? (object)DBNull.Value),
            new KeyValue("@Status", string.IsNullOrEmpty(updatedAppointment.Status) ? (object)DBNull.Value : updatedAppointment.Status),
            new KeyValue("@VideoCallLink", string.IsNullOrEmpty(updatedAppointment.VideoCallLink) ? (object)DBNull.Value : updatedAppointment.VideoCallLink)
        };

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
        OperationResponse<string> response = new OperationResponse<string>();
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>();

        // Prepare parameters for the stored procedure
        param.Add(new KeyValue("@DoctorId", appointment.DoctorId));
        param.Add(new KeyValue("@PatientId", appointment.PatientId));
        param.Add(new KeyValue("@ScheduledTime", appointment.ScheduledTime));
        param.Add(new KeyValue("@Status", appointment.Status));
        param.Add(new KeyValue("@VideoCallLink", appointment.VideoCallLink));
        //param.Add(new KeyValue("@CreatedAt", appointment.CreatedAt));
        param.Add(new KeyValue("@AddedBy", username)); // AddedBy parameter to track who created the appointment

        // Execute the stored procedure and get the result (AppointmentId)
        var appointmentId = await sqlHelper.ExecuteAsScalarAsync<int>("[dbo].[usp_CreateAppointment]", param);

        if (appointmentId > 0)
        {
            response.Result = "Appointment Created Successfully with ID: " + appointmentId;
        }
        else
        {
            response.Result = "Appointment Creation Failed";
        }

        return response;
    }
}