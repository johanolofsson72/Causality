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
    public class ClassController : Controller
    {
        IConfiguration _config;
        Repository<Class, ApplicationDbContext> _manager;
        IMemoryCache _cache;
        private int _cacheInSeconds = 0;

        public ClassController(Repository<Class, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<Class>>> Get()
        {
            string cacheKey = "Class:Get";
            IEnumerable<Class> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Class>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<Class>()
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
        public async Task<ActionResult<APIEntityResponse<Class>>> GetById(string Id)
        {
            string cacheKey = "Class:GetById" + Id;
            Class cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<Class>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = (await _manager.Get(x => x.Id == Convert.ToInt32(Id))).FirstOrDefault();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                if (cacheEntry != null)
                {
                    return Ok(new APIEntityResponse<Class>()
                    {
                        Success = true,
                        Data = cacheEntry,
                        Source = RequestCodes.TWO_ZERO_ZERO + ", recived 1 row from " + (fromCache ? Cache.MemoryCache : Cache.Database)
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Class>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Class Not Found" },
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

        [HttpGet("{value}/getbyvaluecontains")]
        public async Task<ActionResult<APIListOfEntityResponse<Class>>> GetByValueContains(string value)
        {
            string cacheKey = "Class:GetByValueContains:" + value;
            IEnumerable<Class> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Class>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get(x => x.Value.Contains(value));
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<Class>()
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
        public async Task<ActionResult<APIEntityResponse<Class>>> Insert([FromBody] Class Class)
        {
            try
            {
                await _manager.Insert(Class);
                Cache.Remove(_cache, "Class:");
                var result = (await _manager.Get(x => x.Id == Class.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Class>()
                    {
                        Success = true,
                        Data = result,
                        Source = Cache.Database
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Class>()
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
        public async Task<ActionResult<APIEntityResponse<Class>>> Update([FromBody] Class Class)
        {
            try
            {
                await _manager.Update(Class);
                Cache.Remove(_cache, "Class:");
                var result = (await _manager.Get(x => x.Id == Class.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Class>()
                    {
                        Success = true,
                        Data = result,
                        Source = null
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Class>()
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
                        Cache.Remove(_cache, "Class:");
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
