using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using Causality.Server.Data;
using Causality.Shared.Models;
using Causality.Shared.Data;

/// <summary>
/// Can be copied when adding new service
/// Mark the prefix before "xxxxService" and replace and you are good to go
/// </summary>
namespace Causality.Server.Services
{
    public class MetaService : Causality.Shared.Models.MetaService.MetaServiceBase
    {

        Repository<Meta, ApplicationDbContext> _manager;
        ApplicationDbContext _context;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public MetaService(Repository<Meta, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        public override async Task<MetaResponseGet> Get(MetaRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Meta.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString();
            bool IsCached = true;
            IEnumerable<Meta> cacheEntry;
            MetaResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Meta>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<Meta, bool>> filter = ExpressionBuilder.BuildFilter<Meta>(request.Filter);
                    Func<IQueryable<Meta>, IOrderedQueryable<Meta>> orderBy = ExpressionBuilder.BuildOrderBy<Meta>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Metas.AddRange(cacheEntry);
                response.Success = true;
                response.Status = RequestCodes.TWO_ZERO_ZERO + ", recived " + cacheEntry.Count().ToString() + " rows from " + (IsCached ? Cache.MemoryCache : Cache.Database);
                response.Error = "";
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<MetaResponseGet>(response);
        }

        public override async Task<MetaResponseGetById> GetById(MetaRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Meta.GetById::" + request.Id.ToString();
            bool IsCached = true;
            Meta cacheEntry;
            var response = new MetaResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Meta>(cacheKey, out cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Meta = cacheEntry;
                response.Success = true;
                response.Status = RequestCodes.TWO_ZERO_ZERO + ", recived 1 row from " + (IsCached ? Cache.MemoryCache : Cache.Database);
                response.Error = "";
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<MetaResponseGetById>(response);
        }

        public override async Task<MetaResponseInsert> Insert(MetaRequestInsert request, ServerCallContext context)
        {
            var response = new MetaResponseInsert();
            try
            {
                Meta cacheEntry = await _manager.Insert(request.Meta);
                Cache.Remove(_cache, "Meta.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Meta.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Meta = cacheEntry;
                    response.Success = true;
                    response.Status = RequestCodes.TWO_ZERO_ZERO + ", inserted 1 row and then selected 1 row from " + Cache.Database;
                    response.Error = "";
                }
                else
                {
                    response.Success = false;
                    response.Status = RequestCodes.FIVE_ZERO_ONE;
                    response.Error = "Could not find setting after adding it.";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<MetaResponseInsert>(response);
        }

        public override async Task<MetaResponseUpdate> Update(MetaRequestUpdate request, ServerCallContext context)
        {
            var response = new MetaResponseUpdate();
            try
            {
                Meta cacheEntry = await _manager.Update(request.Meta);
                Cache.Remove(_cache, "Meta.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Meta.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Meta = cacheEntry;
                    response.Success = true;
                    response.Status = RequestCodes.TWO_ZERO_ZERO + ", updated 1 row and then selected 1 row from " + Cache.Database;
                    response.Error = "";
                }
                else
                {
                    response.Success = false;
                    response.Status = RequestCodes.FIVE_ZERO_ONE;
                    response.Error = "Could not find setting after adding it.";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<MetaResponseUpdate>(response);
        }

        public override async Task<MetaResponseDelete> Delete(MetaRequestDelete request, ServerCallContext context)
        {
            var response = new MetaResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "Meta.");
                        response.Success = true;
                        response.Status = RequestCodes.TWO_ZERO_ZERO + ", deleted 1 row";
                        response.Error = "";
                    }
                    else
                    {
                        response.Success = false;
                        response.Status = RequestCodes.FIVE_ZERO_TWO;
                        response.Error = "Delete did not work";
                    }
                }
                else
                {
                    response.Success = false;
                    response.Status = RequestCodes.FIVE_ZERO_ZERO;
                    response.Error = "Could not find meta for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<MetaResponseDelete>(response);
        }

    }
}
