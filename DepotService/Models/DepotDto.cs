using System;

namespace DepotService.Models
{
    public class DepotDto
    {
        public string Computer { get; set; } = "";
        public string Domain { get; set; } = "";
        public DateTime? LastCheck { get; set; }
        public string Status { get; set; } = "";
        public string? Info { get; set; }
    }
}
