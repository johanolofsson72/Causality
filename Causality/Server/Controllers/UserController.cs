using Causality.Server.Data;
using Causality.Shared.Data;
using Causality.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Causation.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        IConfiguration _config;
        Repository<User, ApplicationDbContext> _manager;
        IMemoryCache _cache;
        private int _cacheInSeconds = 0;

        public UserController(Repository<User, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<User>>> Get()
        {
            string cacheKey = "User:Get";
            IEnumerable<User> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<User>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<User>()
                {
                    Success = true,
                    Data = cacheEntry,
                    Source = RequestCodes.TWO_ZERO_ZERO + ", recived " + cacheEntry.Count().ToString() + " rows from " + (fromCache ? Cache.MemoryCache : Cache.Database)
            });
            }
            catch
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIEntityResponse<User>>> GetById(string Id)
        {
            string cacheKey = "User:GetById" + Id;
            User cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<User>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = (await _manager.Get(x => x.Id == Convert.ToInt32(Id))).FirstOrDefault();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                if (cacheEntry != null)
                {
                    return Ok(new APIEntityResponse<User>()
                    {
                        Success = true,
                        Data = cacheEntry,
                        Source = RequestCodes.TWO_ZERO_ZERO + ", recived 1 row from " + (fromCache ? Cache.MemoryCache : Cache.Database)
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<User>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "User Not Found" },
                        Data = null,
                        Source = null
                    });
                }
            }
            catch
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpGet("{key}/getbynamecontains")]
        public async Task<ActionResult<APIListOfEntityResponse<User>>> GetByNameContains(string key)
        {
            string cacheKey = "User:GetByNameContains:" + key;
            IEnumerable<User> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<User>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get(x => x.Name.Contains(key));
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<User>()
                {
                    Success = true,
                    Data = cacheEntry,
                    Source = RequestCodes.TWO_ZERO_ZERO + ", recived " + cacheEntry.Count().ToString() + " rows from " + (fromCache ? Cache.MemoryCache : Cache.Database)
                });
            }
            catch
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIEntityResponse<User>>> Insert([FromBody] User User)
        {
            try
            {
                await _manager.Insert(User);
                Cache.Remove(_cache, "User:");
                var result = (await _manager.Get(x => x.Id == User.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<User>()
                    {
                        Success = true,
                        Data = result,
                        Source = Cache.Database
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<User>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Could not find after adding it." },
                        Data = null,
                        Source = null
                    });
                }
            }
            catch
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPut]
        public async Task<ActionResult<APIEntityResponse<User>>> Update([FromBody] User User)
        {
            try
            {
                await _manager.Update(User);
                Cache.Remove(_cache, "User:");
                var result = (await _manager.Get(x => x.Id == User.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<User>()
                    {
                        Success = true,
                        Data = result,
                        Source = null
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<User>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Could not find after updating it." },
                        Data = null,
                        Source = null
                    });
                }
            }
            catch
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string Id)
        {
            try
            {
                var list = await _manager.Get(x => x.Id == Convert.ToInt32(Id));
                if (list != null)
                {
                    var first = list.First();
                    var success = await _manager.Delete(first);
                    if (success)
                    {
                        Cache.Remove(_cache, "User:");
                        return NoContent();
                    }
                    else
                        return StatusCode(500);
                }
                else
                    return StatusCode(500);
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }

    }
}
