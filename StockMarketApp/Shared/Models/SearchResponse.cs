using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class SearchResponse
    {
        public int count { get; set; }
        public List<SearchResultItem> result { get; set; }
    }
}
