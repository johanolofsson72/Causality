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
    public class ExcludeService : Causality.Shared.Models.ExcludeService.ExcludeServiceBase
    {

        Repository<Exclude, ApplicationDbContext> _manager;
        Repository<Meta, ApplicationDbContext> _meta;
        ApplicationDbContext _context;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public ExcludeService(Repository<Exclude, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        public override async Task<ExcludeResponseGet> Get(ExcludeRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Exclude.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            IEnumerable<Exclude> cacheEntry;
            ExcludeResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Exclude>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<Exclude, bool>> filter = ExpressionBuilder.BuildFilter<Exclude>(request.Filter);
                    Func<IQueryable<Exclude>, IOrderedQueryable<Exclude>> orderBy = ExpressionBuilder.BuildOrderBy<Exclude>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);

                    foreach (var includeProperty in request.IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        foreach (var item in cacheEntry)
                        {
                            if (includeProperty.ToLower().Equals("meta"))
                            {
                                var _ret = await _meta.Get(m => m.ExcludeId == item.Id, m => m.OrderBy("Id ASC"));
                                item.Metas.AddRange(_ret);
                            }
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Excludes.AddRange(cacheEntry);
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

            return await Task.FromResult<ExcludeResponseGet>(response);
        }

        public override async Task<ExcludeResponseGetById> GetById(ExcludeRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Exclude.GetById::" + request.Id.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            Exclude cacheEntry;
            var response = new ExcludeResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Exclude>(cacheKey, out cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);

                    foreach (var includeProperty in request.IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (includeProperty.ToLower().Equals("meta"))
                        {
                            var _ret = await _meta.Get(m => m.ExcludeId == cacheEntry.Id, m => m.OrderBy("Id ASC"));
                            cacheEntry.Metas.AddRange(_ret);
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Exclude = cacheEntry;
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

            return await Task.FromResult<ExcludeResponseGetById>(response);
        }

        public override async Task<ExcludeResponseInsert> Insert(ExcludeRequestInsert request, ServerCallContext context)
        {
            var response = new ExcludeResponseInsert();
            try
            {
                Exclude cacheEntry = await _manager.Insert(request.Exclude);
                Cache.Remove(_cache, "Exclude.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Exclude.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Exclude = cacheEntry;
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

            return await Task.FromResult<ExcludeResponseInsert>(response);
        }

        public override async Task<ExcludeResponseUpdate> Update(ExcludeRequestUpdate request, ServerCallContext context)
        {
            var response = new ExcludeResponseUpdate();
            try
            {
                Exclude cacheEntry = await _manager.Update(request.Exclude);
                Cache.Remove(_cache, "Exclude.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Exclude.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Exclude = cacheEntry;
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

            return await Task.FromResult<ExcludeResponseUpdate>(response);
        }

        public override async Task<ExcludeResponseDelete> Delete(ExcludeRequestDelete request, ServerCallContext context)
        {
            var response = new ExcludeResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    foreach (var item in await _meta.Get(x => x.ExcludeId == request.Id))
                    {
                        if (!await _meta.Delete(item))
                        {
                            throw new Exception("Could not delete " + nameof(item.GetType));
                        }
                    }

                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "Exclude.");
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
                    response.Error = "Could not find Exclude for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<ExcludeResponseDelete>(response);
        }

    }
}
