using System;

namespace DepotService.Models
{
    public class DepotItem
    {
        public string Computer { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? LastCheck { get; set; }
        public string? LastJobName { get; set; }

        // UI helper
        public bool IsSelected { get; set; }
    }
}