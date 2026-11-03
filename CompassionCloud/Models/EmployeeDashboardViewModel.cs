namespace CompassionCloud.Models
{
    public class EmployeeDashboardViewModel
    {
        public List<ReliefProjectUpdate> RecentUpdates { get; set; } = new();
        public List<Volunteer> RecentVolunteers { get; set; } = new();

        // Dashboard stats (for stat cards)
        public int TotalVolunteers { get; set; }
        public int TotalUpdates { get; set; }
        public int TotalDonations { get; set; }
        public decimal TotalDonationAmount { get; set; }
    }
}
