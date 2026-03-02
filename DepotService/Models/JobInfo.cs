using System;

namespace DepotService.Models
{
    public class JobInfo
    {
        public int JobId { get; set; }
        public string Command { get; set; } = "";
        public int Status { get; set; }
        public string Parameters { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }

        public string StatusText => Status switch
        {
            0 => "Wartend",
            1 => "Läuft",
            2 => "Erfolgreich",
            3 => "Fehler",
            _ => "Unbekannt"
        };

        public bool IsActive => Status == 0 || Status == 1;
        public bool IsCompleted => Status == 2 || Status == 3;
        public bool HasError => Status == 3;

        public TimeSpan? Duration
        {
            get
            {
                if (StartedAt.HasValue)
                {
                    var endTime = CompletedAt ?? DateTime.Now;
                    return endTime - StartedAt.Value;
                }
                return null;
            }
        }
    }
}
