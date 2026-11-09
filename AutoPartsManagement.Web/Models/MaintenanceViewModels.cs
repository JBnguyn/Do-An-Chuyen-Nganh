using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Web.Models;

public class MaintenanceIndexViewModel
{
    public List<UserVehicle> Vehicles { get; set; } = new();
    public VehicleMaintenanceSummary? Summary { get; set; }

    /// <summary>Form thêm xe mới.</summary>
    public UserVehicle NewVehicle { get; set; } = new();
}
