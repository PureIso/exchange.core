using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace exchange.service
{
    public class ExchangeSettings
    {
        public string Uri { get; set; }
        public string APIKey { get; set; }
        public string PassPhrase { get; set; }
        public string Secret { get; set; }
        public string EndpointUrl { get; set; }
    }
}
