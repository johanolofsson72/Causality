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
    public class EventService : Causality.Shared.Models.EventService.EventServiceBase
    {

        Repository<Event, ApplicationDbContext> _manager;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public EventService(Repository<Event, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
            _cacheTimeInSeconds = _cacheTimeInSeconds > 2 ? 2 : _cacheTimeInSeconds;
        }

        public override async Task<EventResponseGet> Get(EventRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Event.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            IEnumerable<Event> cacheEntry;
            EventResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Event>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<Event, bool>> filter = ExpressionBuilder.BuildFilter<Event>(request.Filter);
                    Func<IQueryable<Event>, IOrderedQueryable<Event>> orderBy = ExpressionBuilder.BuildOrderBy<Event>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy, request.IncludeProperties);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Events.AddRange(cacheEntry);
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

            return await Task.FromResult<EventResponseGet>(response);
        }

        public override async Task<EventResponseGetById> GetById(EventRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Event.GetById::" + request.Id.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            Event cacheEntry;
            var response = new EventResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Event>(cacheKey, out cacheEntry))
                {
                    cacheEntry = (await _manager.Get(x => x.Id == request.Id, x => x.OrderBy(x => x.Id), request.IncludeProperties)).FirstOrDefault<Event>();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Event = cacheEntry;
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

            return await Task.FromResult<EventResponseGetById>(response);
        }

        public override async Task<EventResponseInsert> Insert(EventRequestInsert request, ServerCallContext context)
        {
            var response = new EventResponseInsert();
            try
            {
                Event cacheEntry = await _manager.Insert(request.Event);
                Cache.Remove(_cache, "Event.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Event.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Event = cacheEntry;
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

            return await Task.FromResult<EventResponseInsert>(response);
        }

        public override async Task<EventResponseUpdate> Update(EventRequestUpdate request, ServerCallContext context)
        {
            var response = new EventResponseUpdate();
            try
            {
                Event cacheEntry = await _manager.Update(request.Event);
                Cache.Remove(_cache, "Event.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Event.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Event = cacheEntry;
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

            return await Task.FromResult<EventResponseUpdate>(response);
        }

        public override async Task<EventResponseDelete> Delete(EventRequestDelete request, ServerCallContext context)
        {
            var response = new EventResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id, x => x.OrderBy(x => x.Id), "Metas,Classes,Causes,Effects,Excludes");
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "Event.");
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
                    response.Error = "Could not find Event for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<EventResponseDelete>(response);
        }

    }
}
