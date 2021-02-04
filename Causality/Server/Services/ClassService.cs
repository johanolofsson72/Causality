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
    public class ClassService : Causality.Shared.Models.ClassService.ClassServiceBase
    {

        Repository<Class, ApplicationDbContext> _manager;
        ApplicationDbContext _context;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public ClassService(Repository<Class, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        public override async Task<ClassResponseGet> Get(ClassRequestGet request, ServerCallContext context)
        {
            string cacheKey = "Class.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString();
            bool IsCached = true;
            IEnumerable<Class> cacheEntry;
            ClassResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Class>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<Class, bool>> filter = ExpressionBuilder.BuildFilter<Class>(request.Filter);
                    Func<IQueryable<Class>, IOrderedQueryable<Class>> orderBy = ExpressionBuilder.BuildOrderBy<Class>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Classes.AddRange(cacheEntry);
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

            return await Task.FromResult<ClassResponseGet>(response);
        }

        public override async Task<ClassResponseGetById> GetById(ClassRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "Class.GetById::" + request.Id.ToString();
            bool IsCached = true;
            Class cacheEntry;
            var response = new ClassResponseGetById();
            try
            {
                if (!_cache.TryGetValue<Class>(cacheKey, out cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Class = cacheEntry;
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

            return await Task.FromResult<ClassResponseGetById>(response);
        }

        public override async Task<ClassResponseInsert> Insert(ClassRequestInsert request, ServerCallContext context)
        {
            var response = new ClassResponseInsert();
            try
            {
                Class cacheEntry = await _manager.Insert(request.Class);
                Cache.Remove(_cache, "Class.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Class.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Class = cacheEntry;
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

            return await Task.FromResult<ClassResponseInsert>(response);
        }

        public override async Task<ClassResponseUpdate> Update(ClassRequestUpdate request, ServerCallContext context)
        {
            var response = new ClassResponseUpdate();
            try
            {
                Class cacheEntry = await _manager.Update(request.Class);
                Cache.Remove(_cache, "Class.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "Class.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.Class = cacheEntry;
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

            return await Task.FromResult<ClassResponseUpdate>(response);
        }

        public override async Task<ClassResponseDelete> Delete(ClassRequestDelete request, ServerCallContext context)
        {
            var response = new ClassResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "Class.");
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
                    response.Error = "Could not find Class for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<ClassResponseDelete>(response);
        }

    }
}
