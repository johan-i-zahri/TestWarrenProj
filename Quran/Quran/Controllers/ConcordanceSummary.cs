using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quran.Controllers
{
    public class ConcordanceSummary
    {
        public string Root { get; set; }
        public HashSet<InflectionCategory> Categories { get; set; }

        public ConcordanceSummary()
        {
            Categories = new HashSet<InflectionCategory>();
        }
    }
}