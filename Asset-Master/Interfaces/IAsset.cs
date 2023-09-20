using Asset_Master.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Asset_Master.Interfaces;

public interface IAsset
{
    //Task<int> Createassets(Createassets model);
    //Task<int> SaveAssetsToSharePoint();
    Task Updateassets(int id, Createassets model);
    //Task Deleteassets(int id);
    Task<IEnumerable<assets>> GetAllassets();
    Task<assets> GetassetsById(int id);
    Task SaveAssetsToSharePoint(IEnumerable<assets> assets);
}
