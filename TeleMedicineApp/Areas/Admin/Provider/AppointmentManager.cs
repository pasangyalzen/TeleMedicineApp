using Microsoft.AspNetCore.Mvc;
using SQLHelper;
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
    public async Task<List<AppointmentDetailsViewModel>> GetAppointmentById(int id)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        IList<KeyValue> param = new List<KeyValue>();
        param.Add(new KeyValue("@AppointmentId", id));
        var result = await sqlHelper.ExecuteAsListAsync<AppointmentDetailsViewModel>("[dbo].[usp_GetAppointmentById]", param);
        return result;

    }

    [HttpDelete]
    public async Task<OperationResponse<string>> DeleteAppointment(int id)
    {
        OperationResponse<string> response = new OperationResponse<string>();
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        return null;
    }

    public async Task<OperationResponse<string>> UpdateAppointment(AppointmentDetailsViewModel appointment)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        return null;
    }

    public async Task<OperationResponse<string>> CreateAppointment(AppointmentDetailsViewModel appointment)
    {
        SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
        return null;
    }
}