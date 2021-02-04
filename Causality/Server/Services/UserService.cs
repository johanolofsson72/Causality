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
    public class UserService : Causality.Shared.Models.UserService.UserServiceBase
    {

        Repository<User, ApplicationDbContext> _manager;
        ApplicationDbContext _context;
        IConfiguration _config;
        IMemoryCache _cache;
        int _cacheTimeInSeconds;

        public UserService(Repository<User, ApplicationDbContext> manager, ApplicationDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _context = context;
            _cache = cache;
            _config = config;
            _cacheTimeInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        public override async Task<UserResponseGet> Get(UserRequestGet request, ServerCallContext context)
        {
            string cacheKey = "User.Get::" + request.Filter + "::" + request.OrderBy + "::" + request.Ascending.ToString();
            bool IsCached = true;
            IEnumerable<User> cacheEntry;
            UserResponseGet response = new();
            try
            {
                if (!_cache.TryGetValue<IEnumerable<User>>(cacheKey, out cacheEntry))
                {
                    Expression<Func<User, bool>> filter = ExpressionBuilder.BuildFilter<User>(request.Filter);
                    Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = ExpressionBuilder.BuildOrderBy<User>(request.OrderBy, request.Ascending);
                    cacheEntry = await _manager.Get(filter, orderBy);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.Users.AddRange(cacheEntry);
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

            return await Task.FromResult<UserResponseGet>(response);
        }

        public override async Task<UserResponseGetById> GetById(UserRequestGetById request, ServerCallContext context)
        {
            string cacheKey = "User.GetById::" + request.Id.ToString();
            bool IsCached = true;
            User cacheEntry;
            var response = new UserResponseGetById();
            try
            {
                if (!_cache.TryGetValue<User>(cacheKey, out cacheEntry))
                {
                    cacheEntry = await _manager.GetById(request.Id);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    IsCached = false;
                }
                response.User = cacheEntry;
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

            return await Task.FromResult<UserResponseGetById>(response);
        }

        public override async Task<UserResponseInsert> Insert(UserRequestInsert request, ServerCallContext context)
        {
            var response = new UserResponseInsert();
            try
            {
                User cacheEntry = await _manager.Insert(request.User);
                Cache.Remove(_cache, "User.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "User.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.User = cacheEntry;
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

            return await Task.FromResult<UserResponseInsert>(response);
        }

        public override async Task<UserResponseUpdate> Update(UserRequestUpdate request, ServerCallContext context)
        {
            var response = new UserResponseUpdate();
            try
            {
                User cacheEntry = await _manager.Update(request.User);
                Cache.Remove(_cache, "User.");
                var result = (await _manager.Get(x => x.Id == cacheEntry.Id)).FirstOrDefault();
                if (result != null)
                {
                    string cacheKey = "User.GetById::" + cacheEntry.Id.ToString();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheTimeInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                    response.User = cacheEntry;
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

            return await Task.FromResult<UserResponseUpdate>(response);
        }

        public override async Task<UserResponseDelete> Delete(UserRequestDelete request, ServerCallContext context)
        {
            var response = new UserResponseDelete();
            try
            {
                var list = await _manager.Get(x => x.Id == request.Id);
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "User.");
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
                    response.Error = "Could not find User for deletion";
                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Status = RequestCodes.FIVE_ZERO_ZERO;
                response.Error = e.ToString();
            }

            return await Task.FromResult<UserResponseDelete>(response);
        }

    }
}
