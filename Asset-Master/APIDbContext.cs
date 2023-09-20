namespace Asset_Master;

using Asset_Master.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;


public class APIDbContext : DbContext
{
    protected readonly IConfiguration _configuration;
    public APIDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public DbSet<assets> assets { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(_configuration.GetConnectionString("DataBase"), new MySqlServerVersion(new Version(8, 0, 11)));
    }
}
