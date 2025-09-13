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
        public string Name { get; set; } = string.Empty;
        public List<Interaction> Interactions { get; set; } = new();
        public List<ExcludedInteraction> ExcludedInteractions { get; set; } = new();
        public string ExecutionTime { get; set; } = string.Empty;

        public class Interaction
        {
            public String Class { get; set; } = "";
            public String Cause { get; set; } = "";
            public String Effect { get; set; } = "";
        }

        public class ExcludedInteraction
        {
            public string Cause { get; set; } = string.Empty;
        }        
    }

}
