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
    public class GenericController : Controller
    {
        IConfiguration _config;
        Repository<Event, ApplicationDbContext> _eventService;
        Repository<Class, ApplicationDbContext> _classService;
        Repository<Cause, ApplicationDbContext> _causeService;
        Repository<Effect, ApplicationDbContext> _effectService;
        Repository<Exclude, ApplicationDbContext> _excludeService;
        Repository<User, ApplicationDbContext> _userService;
        Repository<Meta, ApplicationDbContext> _metaService;
        IMemoryCache _cache;
        private int _cacheInSeconds = 0;

        public GenericController(
            Repository<Event, ApplicationDbContext> eventService,
            Repository<Class, ApplicationDbContext> classService,
            Repository<Cause, ApplicationDbContext> causeService,
            Repository<Effect, ApplicationDbContext> effectService,
            Repository<Exclude, ApplicationDbContext> excludeService,
            Repository<User, ApplicationDbContext> userService,
            Repository<Meta, ApplicationDbContext> metaService,
            IMemoryCache cache,
            IConfiguration config)
        {
            _eventService = eventService;
            _classService = classService;
            _causeService = causeService;
            _effectService = effectService;
            _excludeService = excludeService;
            _userService = userService;
            _metaService = metaService;
            _cache = cache;
            _config = config;
            _cacheInSeconds = _config.GetValue<int>("AppSettings:DataCacheInSeconds");
        }

        [HttpGet]
        public async Task<ActionResult<APIEntityResponse<Event>>> Get()
        {
            string cacheKey = "Generic:Get";
            Event cacheEntry;
            bool fromCache = true;
            try
            {
                if (!_cache.TryGetValue<Event>(cacheKey, out cacheEntry))
                {
                    fromCache = false;
                    cacheEntry = await _eventService.GetById(1);

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(_cacheInSeconds));
                    _cache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                return Ok(new APIEntityResponse<Event>()
                {
                    Success = true,
                    Data = cacheEntry,
                    Source = RequestCodes.TWO_ZERO_ZERO + ", recived 1 rows from " + (fromCache ? Cache.MemoryCache : Cache.Database)
                });
            }
            catch
            {
                // log exception here
                return StatusCode(500);
            }
        }


    }
}
