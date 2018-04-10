using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace NSMGFunc.Models
{
    public class HttpModel
    {
        public string id { get; set; }
        public JObject jobject { get; set; }
    }
}
