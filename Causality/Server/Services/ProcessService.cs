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
    public class ProcessService : Causality.Shared.Models.ProcessService.ProcessServiceBase
    {

        readonly Repository<Process, ApplicationDbContext> _manager;
        readonly IConfiguration _config;
        readonly IMemoryCache _cache;
        private readonly int _cacheTimeInSeconds;

        public ProcessService(Repository<Process, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        public override async Task<ProcessResponseGet> Get(ProcessRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Process.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            ProcessResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Process>>(cacheKey, out IEnumerable<Process> cacheEntry))
                {
                    Expression<Func<Process, bool>> filter = ExpressionBuilder.BuildFilter<Process>(request.Filter);
                    Func<IQueryable<Process>, IOrderedQueryable<Process>> orderBy = ExpressionBuilder.BuildOrderBy<Process>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy, request.IncludeProperties);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Process.AddRange(cacheEntry);
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

            return await Task.FromResult<ProcessResponseGet>(response);
        }

        public override async Task<ProcessResponseGetById> GetById(ProcessRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Process.GetById::" + request.Id.ToString() + "::" + request.IncludeProperties;
            bool IsCached = true;
            var response = new ProcessResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Process>(cacheKey, out Process cacheEntry))
                {
                    cacheEntry = (await _manager.Get(x => x.Id == request.Id, x => x.OrderBy(x => x.Id), request.IncludeProperties)).FirstOrDefault<Process>();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Process = cacheEntry;
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

            return await Task.FromResult<ProcessResponseGetById>(response);
        }

        public override async Task<ProcessResponseInsert> Insert(ProcessRequestInsert request, ServerCallContext context)
        {
            var response = new ProcessResponseInsert();
            try
            {
                Process cacheEntry = await _manager.Insert(request.Process);
                Cache.Remove(_cache, "Process.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Process.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Process = cacheEntry;
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

            return await Task.FromResult<ProcessResponseInsert>(response);
        }

        public override async Task<ProcessResponseUpdate> Update(ProcessRequestUpdate request, ServerCallContext context)
        {
            var response = new ProcessResponseUpdate();
            try
            {
                Process cacheEntry = await _manager.Update(request.Process);
                Cache.Remove(_cache, "Process.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Process.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Process = cacheEntry;
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

            return await Task.FromResult<ProcessResponseUpdate>(response);
        }

        public override async Task<ProcessResponseDelete> Delete(ProcessRequestDelete request, ServerCallContext context)
        {
            var response = new ProcessResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id, x => x.OrderBy(x => x.Id), "Metas");
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "Process.");
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
                    response.Error = "Could not find Process for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<ProcessResponseDelete>(response);
        }

    }
}
