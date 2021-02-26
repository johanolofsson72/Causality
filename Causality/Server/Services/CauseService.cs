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
    public class CauseService : Causality.Shared.Models.CauseService.CauseServiceBase
    {

        Repository<Cause, ApplicationDbContext> _manager;
        Repository<Effect, ApplicationDbContext> _effect;
        Repository<Exclude, ApplicationDbContext> _exclude;
        Repository<Meta, ApplicationDbContext> _meta;
        ApplicationDbContext _context;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public CauseService(Repository<Cause, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config, Repository<Effect, ApplicationDbContext> effect, Repository<Exclude, ApplicationDbContext> exclude, Repository<Meta, ApplicationDbContext> meta)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
            _effect = effect;
            _exclude = exclude;
            _meta = meta;
        }

        public override async Task<CauseResponseGet> Get(CauseRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Cause.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            IEnumerable<Cause> cacheEntry;
            CauseResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Cause>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<Cause, bool>> filter = ExpressionBuilder.BuildFilter<Cause>(request.Filter);
                    Func<IQueryable<Cause>, IOrderedQueryable<Cause>> orderBy = ExpressionBuilder.BuildOrderBy<Cause>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);

                    foreach (var includeProperty in request.IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        foreach (var item in cacheEntry)
                        {
                            if (includeProperty.ToLower().Equals("effect"))
                            {
                                var _ret = await _effect.Get(m => m.CauseId == item.Id, m => m.OrderBy("Id ASC"));
                                item.Effects.AddRange(_ret);
                            }
                            if (includeProperty.ToLower().Equals("exclude"))
                            {
                                var _ret = await _exclude.Get(m => m.CauseId == item.Id, m => m.OrderBy("Id ASC"));
                                item.Excludes.AddRange(_ret);
                            }
                            if (includeProperty.ToLower().Equals("meta"))
                            {
                                var _ret = await _meta.Get(m => m.CauseId == item.Id, m => m.OrderBy("Id ASC"));
                                item.Metas.AddRange(_ret);
                            }
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Causes.AddRange(cacheEntry);
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

            return await Task.FromResult<CauseResponseGet>(response);
        }

        public override async Task<CauseResponseGetById> GetById(CauseRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Cause.GetById::" + request.Id.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            Cause cacheEntry;
            var response = new CauseResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Cause>(cacheKey, out cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);

                    foreach (var includeProperty in request.IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (includeProperty.ToLower().Equals("effect"))
                        {
                            var _ret = await _effect.Get(m => m.CauseId == cacheEntry.Id, m => m.OrderBy("Id ASC"));
                            cacheEntry.Effects.AddRange(_ret);
                        }
                        if (includeProperty.ToLower().Equals("exclude"))
                        {
                            var _ret = await _exclude.Get(m => m.CauseId == cacheEntry.Id, m => m.OrderBy("Id ASC"));
                            cacheEntry.Excludes.AddRange(_ret);
                        }
                        if (includeProperty.ToLower().Equals("meta"))
                        {
                            var _ret = await _meta.Get(m => m.CauseId == cacheEntry.Id, m => m.OrderBy("Id ASC"));
                            cacheEntry.Metas.AddRange(_ret);
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Cause = cacheEntry;
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

            return await Task.FromResult<CauseResponseGetById>(response);
        }

        public override async Task<CauseResponseInsert> Insert(CauseRequestInsert request, ServerCallContext context)
        {
            var response = new CauseResponseInsert();
            try
            {
                Cause cacheEntry = await _manager.Insert(request.Cause);
                Cache.Remove(_cache, "Cause.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Cause.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Cause = cacheEntry;
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

            return await Task.FromResult<CauseResponseInsert>(response);
        }

        public override async Task<CauseResponseUpdate> Update(CauseRequestUpdate request, ServerCallContext context)
        {
            var response = new CauseResponseUpdate();
            try
            {
                Cause cacheEntry = await _manager.Update(request.Cause);
                Cache.Remove(_cache, "Cause.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Cause.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Cause = cacheEntry;
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

            return await Task.FromResult<CauseResponseUpdate>(response);
        }

        public override async Task<CauseResponseDelete> Delete(CauseRequestDelete request, ServerCallContext context)
        {
            var response = new CauseResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    foreach (var item in await _effect.Get(x => x.CauseId == request.Id))
                    {
                        if (!await _effect.Delete(item))
                        {
                            throw new Exception("Could not delete " + nameof(item.GetType));
                        }
                    }

                    foreach (var item in await _exclude.Get(x => x.CauseId == request.Id))
                    {
                        if (!await _exclude.Delete(item))
                        {
                            throw new Exception("Could not delete " + nameof(item.GetType));
                        }
                    }

                    foreach (var item in await _meta.Get(x => x.CauseId == request.Id))
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
                        Cache.Remove(_cache, "Cause.");
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
                    response.Error = "Could not find Cause for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<CauseResponseDelete>(response);
        }

    }
}
