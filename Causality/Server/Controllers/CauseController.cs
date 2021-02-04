using Causality.Shared.Models;
using Causality.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Causality.Shared.Data;

namespace Causation.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CauseController : Controller
    {
        IConfiguration _config;
        Repository<Cause, ApplicationDbContext> _manager;
        IMemoryCache _cache;
        private int _cacheInSeconds = 0;

        public CauseController(Repository<Cause, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<Cause>>> Get()
        {
            string cacheKey = "Cause:Get";
            IEnumerable<Cause> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Cause>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<Cause>()
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
        public async Task<ActionResult<APIEntityResponse<Cause>>> GetById(string Id)
        {
            string cacheKey = "Cause:GetById" + Id;
            Cause cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<Cause>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = (await _manager.Get(x => x.Id == Convert.ToInt32(Id))).FirstOrDefault();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                if (cacheEntry != null)
                {
                    return Ok(new APIEntityResponse<Cause>()
                    {
                        Success = true,
                        Data = cacheEntry,
                        Source = RequestCodes.TWO_ZERO_ZERO + ", recived 1 row from " + (fromCache ? Cache.MemoryCache : Cache.Database)
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Cause>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Cause Not Found" },
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
        public async Task<ActionResult<APIListOfEntityResponse<Cause>>> GetByValueContains(string value)
        {
            string cacheKey = "Cause:GetByValueContains:" + value;
            IEnumerable<Cause> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Cause>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get(x => x.Value.Contains(value));
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<Cause>()
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
        public async Task<ActionResult<APIEntityResponse<Cause>>> Insert([FromBody] Cause Cause)
        {
            try
            {
                await _manager.Insert(Cause);
                Cache.Remove(_cache, "Cause:");
                var result = (await _manager.Get(x => x.Id == Cause.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Cause>()
                    {
                        Success = true,
                        Data = result,
                        Source = Cache.Database
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Cause>()
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
        public async Task<ActionResult<APIEntityResponse<Cause>>> Update([FromBody] Cause Cause)
        {
            try
            {
                await _manager.Update(Cause);
                Cache.Remove(_cache, "Cause:");
                var result = (await _manager.Get(x => x.Id == Cause.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Cause>()
                    {
                        Success = true,
                        Data = result,
                        Source = null
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Cause>()
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
                        Cache.Remove(_cache, "Cause:");
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
