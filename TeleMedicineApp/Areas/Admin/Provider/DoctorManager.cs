using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQLHelper;
using TeleMedicineApp.Areas.Doctor.Models;
using TeleMedicineApp.Areas.Doctor.ViewModels; 

namespace TeleMedicineApp.Areas.Admin.Provider
{
    public class DoctorManager
    {
        public async Task<List<DoctorRegistrationViewModel>> GetTotalDoctors(int offset, int limit,
            string searchKeyword = "", string sortColumn = "", string sortOrder = "DESC")
        {
            SQLHandlerAsync sqlHelper = new SQLHandlerAsync();
            IList<KeyValue> parameters = new List<KeyValue>
            {
                new KeyValue("@Offset", offset),
                new KeyValue("@Limit", limit),
                new KeyValue("@SearchKeyword", searchKeyword),
                new KeyValue("@SortColumn", sortColumn),
                new KeyValue("@SortOrder", sortOrder)
            };

            // Your SQL query execution goes here, e.g.,:
            var result = await sqlHelper.ExecuteAsListAsync<DoctorRegistrationViewModel>("[dbo].[usp_GetDoctorList]", parameters);
            return result;
            
        }
    }
}