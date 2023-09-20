using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
//using Microsoft.SharePoint.Client;
using System.Net;
using System.Security;
using Asset_Master.Entities;
using Asset_Master.Interfaces;
using Asset_Master.Controllers;
using System.Xml.Linq;

namespace Asset_Master.Repository;


public class AssetRepository : IAsset
{
    private readonly APIDbContext _dbcontext;
    private IMapper _mapper;
    public AssetRepository(APIDbContext dbContext, IMapper mapper)
    {
        _dbcontext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<assets>> GetAllassets()
    {
        return await _dbcontext.assets.ToListAsync().ConfigureAwait(true);
    }
    public async Task<assets> GetassetsById(int id)
    {
        return await getassets(id);
    }
    private async Task<assets> getassets(int id)
    {
        assets? assets = await _dbcontext.assets.Where(x => x.id == id).FirstOrDefaultAsync().ConfigureAwait(true); ;
        if (assets == null) throw new KeyNotFoundException("assets not found");
        return assets;
    }
    public async Task<int> Createassets(Createassets model)
    {
        // validate
        if (_dbcontext.assets.Any(x => x.serial == model.serial))
            throw new ApplicationException("assets with the email '" + model.serial + "' already exists");

        // map model to new assets object
        var assets = _mapper.Map<assets>(model);

        // save user
        _dbcontext.assets.Add(assets);
        await _dbcontext.SaveChangesAsync().ConfigureAwait(true);
        return assets.id;
    }
    public async Task Updateassets(int id, Createassets model)
    {
        assets? assets = await getassets(id);

        // validate
        if (model.serial != assets.serial && _dbcontext.assets.Any(x => x.serial == model.serial))
            throw new ApplicationException("assets with the email '" + model.serial + "' already exists");

        _mapper.Map(model, assets);
        _dbcontext.assets.Update(assets);
        await _dbcontext.SaveChangesAsync().ConfigureAwait(true);
    }

    public async Task Deleteassets(int id)
    {
        assets? assets = await getassets(id);
        _dbcontext.assets.Remove(assets);
        await _dbcontext.SaveChangesAsync().ConfigureAwait(true);
    }

    Task IAsset.SaveAssetsToSharePoint(IEnumerable<assets> assets)
    {
        throw new NotImplementedException();
    }
}