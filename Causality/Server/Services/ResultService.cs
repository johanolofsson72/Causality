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
    public class ResultService : Causality.Shared.Models.ResultService.ResultServiceBase
    {

        readonly Repository<Result, ApplicationDbContext> _manager;
        readonly ApplicationDbContext _context;
        readonly IConfiguration _config;
        readonly IMemoryCache _cache;
        readonly int _cacheTimeInSeconds;

        public ResultService(Repository<Result, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        public override async Task<ResultResponseGet> Get(ResultRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Result.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString();
            bool IsCached = true;
            ResultResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Result>>(cacheKey, out IEnumerable<Result> cacheEntry))
                {
                    Expression<Func<Result, bool>> filter = ExpressionBuilder.BuildFilter<Result>(request.Filter);
                    Func<IQueryable<Result>, IOrderedQueryable<Result>> orderBy = ExpressionBuilder.BuildOrderBy<Result>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Result.AddRange(cacheEntry);
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

            return await Task.FromResult<ResultResponseGet>(response);
        }

        public override async Task<ResultResponseGetById> GetById(ResultRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Result.GetById::" + request.Id.ToString();
            bool IsCached = true;
            var response = new ResultResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Result>(cacheKey, out Result cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Result = cacheEntry;
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

            return await Task.FromResult<ResultResponseGetById>(response);
        }

        public override async Task<ResultResponseInsert> Insert(ResultRequestInsert request, ServerCallContext context)
        {
            var response = new ResultResponseInsert();
            try
            {
                Result cacheEntry = await _manager.Insert(request.Result);
                Cache.Remove(_cache, "Result.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Result.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Result = cacheEntry;
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

            return await Task.FromResult<ResultResponseInsert>(response);
        }

        public override async Task<ResultResponseUpdate> Update(ResultRequestUpdate request, ServerCallContext context)
        {
            var response = new ResultResponseUpdate();
            try
            {
                Result cacheEntry = await _manager.Update(request.Result);
                Cache.Remove(_cache, "Result.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Result.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Result = cacheEntry;
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

            return await Task.FromResult<ResultResponseUpdate>(response);
        }

        public override async Task<ResultResponseDelete> Delete(ResultRequestDelete request, ServerCallContext context)
        {
            var response = new ResultResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "Result.");
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
                    response.Error = "Could not find Result for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<ResultResponseDelete>(response);
        }

    }
}
