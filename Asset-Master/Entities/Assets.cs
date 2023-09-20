using Microsoft.AspNetCore.Mvc;

namespace Asset_Master.Entities
{
    public class assets
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? asset_tag { get; set; }
        public string? serial { get; set; }
        public int? assigned_to { get; set; } // Use int? for nullable type
        public int model_id { get; set; }
        public int? user_id { get; set; } // Use int? for nullable type
        public int? status_id { get; set; } // Use int? for nullable type
        public string? _snipeit_workstation_1 { get; set; }
    }

    public class Createassets
    {
        public string? name { get; set; }
        public string? asset_tag { get; set; }
        public string? serial { get; set; }
        public int? assigned_to { get; set; } // Use int? for nullable type
        public int model_id { get; set; }
        public int? user_id { get; set; } // Use int? for nullable type
        public int? status_id { get; set; } // Use int? for nullable type
        public string? _snipeit_workstation_1 { get; set; }
    }
}