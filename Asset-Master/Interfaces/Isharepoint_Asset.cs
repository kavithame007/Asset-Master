using Asset_Master.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Asset_Master.Interfaces;

public interface Isharepoint_Asset
{
    Task<IEnumerable<sharepoint_Asset>> GetAllavailableassets();
    
}
