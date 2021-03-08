using Causality.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Causality.Shared.Data
{
    public static class MetaExtensions
    {
        public static string GetPropertyValueAsString(this string propertyName, IEnumerable<Meta> list)
        {
            var ret = "missing";
            try
            {
                foreach (var item in list)
                {
                    if (item.Key.ToLower().Equals(propertyName.ToLower()))
                    {
                        return item.Value;
                    }
                }
                return ret;
            }
            catch
            {
                return ret;
            }
        }
        public static Int32 GetPropertyValueAsInt32(this string propertyName, IEnumerable<Meta> list)
        {
            var ret = 0;
            try
            {
                foreach (var item in list)
                {
                    if (item.Key.ToLower().Equals(propertyName.ToLower()))
                    {
                        return Int32.Parse(item.Value);
                    }
                }
                return ret;
            }
            catch
            {
                return ret;
            }
        }
        public static DateTime GetPropertyValueAsDateTime(this string propertyName, IEnumerable<Meta> list)
        {
            var ret = new DateTime();
            try
            {
                foreach (var item in list)
                {
                    if (item.Key.ToLower().Equals(propertyName.ToLower()))
                    {
                        _ = DateTime.TryParse(item.Value, out DateTime dt);
                        return dt;
                    }
                }
                return ret;
            }
            catch
            {
                return ret;
            }
        }
    }


    public static class StringExtension
    {
        /// <summary>
        /// Use the current thread's culture info for conversion
        /// </summary>
        public static string ToTitleCase(this string str)
        {
            var cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            return cultureInfo.TextInfo.ToTitleCase(str.ToLower());
        }

        /// <summary>
        /// Overload which uses the culture info with the specified name
        /// </summary>
        public static string ToTitleCase(this string str, string cultureInfoName)
        {
            var cultureInfo = new CultureInfo(cultureInfoName);
            return cultureInfo.TextInfo.ToTitleCase(str.ToLower());
        }

        /// <summary>
        /// Overload which uses the specified culture info
        /// </summary>
        public static string ToTitleCase(this string str, CultureInfo cultureInfo)
        {
            return cultureInfo.TextInfo.ToTitleCase(str.ToLower());
        }

        public static DateTime ToDateTime(this string str)
        {
            _ = DateTime.TryParse(str, out DateTime dt);
            return dt;
        }
    }


    public static class Property
    {
        public static object Search(string propertyName, IEnumerable<Meta> list)
        {
            var ret = "missing";
            try
            {
                foreach (var item in list)
                {
                    if (item.Key.ToLower().Equals(propertyName.ToLower()))
                    {
                        return item.Value;
                    }
                }
                return ret;
            }
            catch
            {
                return ret;
            }
        }
    }
}
