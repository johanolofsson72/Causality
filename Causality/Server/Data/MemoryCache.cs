using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Causality.Server.Data
{
    public static class Cache
    {
        public static string Database = "Database";
        public static string MemoryCache = "MemoryCache";

        public static void Remove(IMemoryCache cache, string prefix)
        {
            var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = field.GetValue(cache) as ICollection;
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    var value = item.GetType().GetProperty("Key").GetValue(item);
                    if (value.ToString().StartsWith(prefix))
                    {
                        cache.Remove(value.ToString());
                    }
                }
            }
        }
    }
}
