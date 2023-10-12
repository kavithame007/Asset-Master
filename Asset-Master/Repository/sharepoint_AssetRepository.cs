using Asset_Master.Entities;
using Asset_Master.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.ExternalConnectors;
using static Asset_Master.Repository.sharepoint_AssetRepository;


namespace Asset_Master.Repository;

public class sharepoint_AssetRepository : Isharepoint_Asset
    {
    private readonly APIDbContext _dbcontext;
    private IMapper _mapper;
    public sharepoint_AssetRepository(APIDbContext dbContext, IMapper mapper)
    {
        _dbcontext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<sharepoint_Asset>> GetAllavailableassets()
    {
        return await _dbcontext.sharepoint_Asset.ToListAsync().ConfigureAwait(true);
    }
    
}