using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpPerfTestTool
{
    public class HttpMonster
    {
        public ILog Log { get; set; }

        public HttpMonster(ILog log)
        {
            Log = log;
        }

        public string Start(string url, string method, string data)
        {
            throw new NotImplementedException();
        }

        public void Stop(string testId)
        {
            throw new NotImplementedException();
        }
    }
}
