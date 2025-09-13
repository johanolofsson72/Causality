using System;
using System.Collections.Generic;
using System.Text;

namespace Causality.Shared.Models
{
    public class APIEntityResponse<TEntity> where TEntity : class
    {
        public bool Success { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public string Source { get; set; } = "";
        public TEntity? Data { get; set; }
    }
}
