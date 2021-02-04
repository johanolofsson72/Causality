using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Data
{
    public static class RequestCodes
    {
        public static string TWO_ZERO_ZERO = "200 OK";
        public static string FIVE_ZERO_ZERO = "500 Internal Server Error";
        public static string FIVE_ZERO_ONE = "501 Could Not Find Data After Adding It.";
        public static string FIVE_ZERO_TWO = "502 Delete Did Not Work.";
        public static string FIVE_ZERO_THREE = "503 Service Unavailable";
        public static string FIVE_ZERO_FOUR = "504 No Internet Connection";
        public static string FIVE_ZERO_FIVE = "505 No Internet Connection And No Local Data";
    }
}
