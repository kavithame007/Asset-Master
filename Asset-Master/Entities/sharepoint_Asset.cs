using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Asset_Master.Entities
{
    public class sharepoint_Asset
    {
        [Key]
        public int rownum { get; set; }
        public string? name { get; set; }
        public string? asset_tag { get; set; }
        public string? serial { get; set; }
        public string? modelname { get; set; }
        public string? categoryname { get; set; }
    }

    public class Createsharepoint_Asset
    {
        public string? name { get; set; }
        public string? asset_tag { get; set; }
        public string? serial { get; set; }
        public string? modelname { get; set; }
        public string? categoryname { get; set; }
    }
}
