using CompassionCloud.Models;

namespace CompassionCloud.Models
{
    public class EmployeeDashboardViewModel
    {
        public List<ReliefProjectUpdate> RecentUpdates { get; set; } = new();
        public List<Volunteer> RecentVolunteers { get; set; } = new();
    }
}