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
using Serialize.Linq.Serializers;
using System.Text;

/// <summary>
/// Can be copied when adding new service
/// Mark the prefix before "xxxxService" and replace and you are good to go
/// </summary>
namespace Causality.Server.Services
{
    public class EffectService : Causality.Shared.Models.EffectService.EffectServiceBase
    {

        Repository<Effect, ApplicationDbContext> _manager;
        Repository<Meta, ApplicationDbContext> _meta;
        ApplicationDbContext _context;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public EffectService(Repository<Effect, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config, Repository<Meta, ApplicationDbContext> meta)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
            _meta = meta;
        }

        public override async Task<EffectResponseGet> Get(EffectRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Effect.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            IEnumerable<Effect> cacheEntry;
            EffectResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Effect>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<Effect, bool>> filter = ExpressionBuilder.BuildFilter<Effect>(request.Filter);
                    Func<IQueryable<Effect>, IOrderedQueryable<Effect>> orderBy = ExpressionBuilder.BuildOrderBy<Effect>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);

                    foreach (var includeProperty in request.IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        foreach (var item in cacheEntry)
                        {
                            if (includeProperty.ToLower().Equals("meta"))
                            {
                                var _ret = await _meta.Get(m => m.EffectId == item.Id, m => m.OrderBy("Id ASC"));
                                item.Metas.AddRange(_ret);
                            }
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Effects.AddRange(cacheEntry);
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

            return await Task.FromResult<EffectResponseGet>(response);
        }

        public override async Task<EffectResponseGetById> GetById(EffectRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Effect.GetById::" + request.Id.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            Effect cacheEntry;
            var response = new EffectResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Effect>(cacheKey, out cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);

                    foreach (var includeProperty in request.IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (includeProperty.ToLower().Equals("meta"))
                        {
                            var _ret = await _meta.Get(m => m.EffectId == cacheEntry.Id, m => m.OrderBy("Id ASC"));
                            cacheEntry.Metas.AddRange(_ret);
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Effect = cacheEntry;
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

            return await Task.FromResult<EffectResponseGetById>(response);
        }

        public override async Task<EffectResponseInsert> Insert(EffectRequestInsert request, ServerCallContext context)
        {
            var response = new EffectResponseInsert();
            try
            {
                Effect cacheEntry = await _manager.Insert(request.Effect);
                Cache.Remove(_cache, "Effect.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Effect.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Effect = cacheEntry;
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

            return await Task.FromResult<EffectResponseInsert>(response);
        }

        public override async Task<EffectResponseUpdate> Update(EffectRequestUpdate request, ServerCallContext context)
        {
            var response = new EffectResponseUpdate();
            try
            {
                Effect cacheEntry = await _manager.Update(request.Effect);
                Cache.Remove(_cache, "Effect.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Effect.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Effect = cacheEntry;
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

            return await Task.FromResult<EffectResponseUpdate>(response);
        }

        public override async Task<EffectResponseDelete> Delete(EffectRequestDelete request, ServerCallContext context)
        {
            var response = new EffectResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    foreach (var item in await _meta.Get(x => x.EffectId == request.Id))
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
                        Cache.Remove(_cache, "Effect.");
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
                    response.Error = "Could not find Effect for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<EffectResponseDelete>(response);
        }

    }
}
