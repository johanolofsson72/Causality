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
    public class StateService : Causality.Shared.Models.StateService.StateServiceBase
    {

        readonly Repository<State, ApplicationDbContext> _manager;
        readonly IConfiguration _config;
        readonly IMemoryCache _cache;
        readonly int _cacheTimeInSeconds;

        public StateService(Repository<State, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
            _cacheTimeInSeconds = _cacheTimeInSeconds > 2 ? 2 : _cacheTimeInSeconds;
        }

        public override async Task<StateResponseGet> Get(StateRequestGet request, ServerCallContext context)
        {
            string cacheKey = "State.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            StateResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<State>>(cacheKey, out IEnumerable<State> cacheEntry))
                {
                    Expression<Func<State, bool>> filter = ExpressionBuilder.BuildFilter<State>(request.Filter);
                    Func<IQueryable<State>, IOrderedQueryable<State>> orderBy = ExpressionBuilder.BuildOrderBy<State>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy, request.IncludeProperties);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.State.AddRange(cacheEntry);
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

            return await Task.FromResult<StateResponseGet>(response);
        }

        public override async Task<StateResponseGetById> GetById(StateRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "State.GetById::" + request.Id.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            var response = new StateResponseGetById();
            try
            {
                if (!_cache.TryGetValue<State>(cacheKey, out State cacheEntry))
                {
                    cacheEntry = (await _manager.Get(x => x.Id == request.Id, x => x.OrderBy(x => x.Id), request.IncludeProperties)).FirstOrDefault<State>();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.State = cacheEntry;
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

            return await Task.FromResult<StateResponseGetById>(response);
        }

        public override async Task<StateResponseInsert> Insert(StateRequestInsert request, ServerCallContext context)
        {
            var response = new StateResponseInsert();
            try
            {
                State cacheEntry = await _manager.Insert(request.State);
                Cache.Remove(_cache, "State.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "State.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.State = cacheEntry;
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

            return await Task.FromResult<StateResponseInsert>(response);
        }

        public override async Task<StateResponseUpdate> Update(StateRequestUpdate request, ServerCallContext context)
        {
            var response = new StateResponseUpdate();
            try
            {
                State cacheEntry = await _manager.Update(request.State);
                Cache.Remove(_cache, "State.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "State.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.State = cacheEntry;
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

            return await Task.FromResult<StateResponseUpdate>(response);
        }

        public override async Task<StateResponseDelete> Delete(StateRequestDelete request, ServerCallContext context)
        {
            var response = new StateResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id, x => x.OrderBy(x => x.Id), "Metas");
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "State.");
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
                    response.Error = "Could not find State for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<StateResponseDelete>(response);
        }

    }
}
