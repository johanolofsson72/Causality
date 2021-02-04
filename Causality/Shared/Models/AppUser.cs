using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Interaction> Interactions { get; set; }
        public List<ExcludedInteraction> ExcludedInteractions { get; set; }
        public string ExecutionTime { get; set; }

        public class Interaction
        {
            public String Class { get; set; } = "";
            public String Cause { get; set; } = "";
            public String Effect { get; set; } = "";
        }

        public class ExcludedInteraction
        {
            public string Cause { get; set; }
        }        
    }

}
