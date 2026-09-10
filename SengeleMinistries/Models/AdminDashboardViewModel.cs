using System.Collections.Generic;

namespace SengeleMinistries.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalServeApplications { get; set; }
        public int TotalProducts { get; set; }

        // Recent members (most recent first)
        public List<Member> RecentMembers { get; set; } = new List<Member>();
    }
}
