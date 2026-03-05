using System;

namespace DepotService.Models
{
    public class DepotDto
    {
        public int Id { get; set; }
        public string Computer { get; set; } = "";
        public string Domain { get; set; } = "";
        public DateTime? LastCheck { get; set; }
        public int Status { get; set; }
        public string? Info { get; set; }
        public DateTime? CreatedTime { get; set; }
        public int? DepotSyncId { get; set; }
    }
}
