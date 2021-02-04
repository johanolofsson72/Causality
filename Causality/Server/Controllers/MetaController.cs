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
    public class MetaController : Controller
    {
        IConfiguration _config;
        Repository<Meta, ApplicationDbContext> _manager;
        IMemoryCache _cache;
        private int _cacheInSeconds = 0;

        public MetaController(Repository<Meta, ApplicationDbContext> manager, IMemoryCache cache, IConfiguration config)
        {
            _manager = manager;
            _cache = cache;
            _config = config;
            _cacheInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<Meta>>> Get()
        {
            string cacheKey = "Meta:Get";
            IEnumerable<Meta> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Meta>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<Meta>()
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
        public async Task<ActionResult<APIEntityResponse<Meta>>> GetById(string Id)
        {
            string cacheKey = "Meta:GetById" + Id;
            Meta cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<Meta>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = (await _manager.Get(x => x.Id == Convert.ToInt32(Id))).FirstOrDefault();
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                if (cacheEntry != null)
                {
                    return Ok(new APIEntityResponse<Meta>()
                    {
                        Success = true,
                        Data = cacheEntry,
                        Source = RequestCodes.TWO_ZERO_ZERO + ", recived 1 row from " + (fromCache ? Cache.MemoryCache : Cache.Database)
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Meta>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Meta Not Found" },
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

        [HttpGet("{key}/getbykeycontains")]
        public async Task<ActionResult<APIListOfEntityResponse<Meta>>> GetByKeyContains(string key)
        {
            string cacheKey = "Cause:GetByKeyContains:" + key;
            IEnumerable<Meta> cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<IEnumerable<Meta>>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _manager.Get(x => x.Key.Contains(key));
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIListOfEntityResponse<Meta>()
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
        public async Task<ActionResult<APIEntityResponse<Meta>>> Insert([FromBody] Meta Meta)
        {
            try
            {
                await _manager.Insert(Meta);
                Cache.Remove(_cache, "Meta:");
                var result = (await _manager.Get(x => x.Id == Meta.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Meta>()
                    {
                        Success = true,
                        Data = result,
                        Source = Cache.Database
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Meta>()
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
        public async Task<ActionResult<APIEntityResponse<Meta>>> Update([FromBody] Meta Meta)
        {
            try
            {
                await _manager.Update(Meta);
                Cache.Remove(_cache, "Meta:");
                var result = (await _manager.Get(x => x.Id == Meta.Id)).FirstOrDefault();
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Meta>()
                    {
                        Success = true,
                        Data = result,
                        Source = null
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Meta>()
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
                        Cache.Remove(_cache, "Meta:");
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
