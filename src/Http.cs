using System;
using System.Net;

namespace OledLiveScore
{
    // WebClient with a request timeout (WebClient has no timeout of its own).
    internal sealed class TimedWebClient : WebClient
    {
        public int TimeoutMs = 12000;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var r = base.GetWebRequest(address);
            if (r != null) r.Timeout = TimeoutMs;
            return r;
        }
    }
}
