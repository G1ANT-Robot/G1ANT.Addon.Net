using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.Listeners
{
    public class EwsListener : ITraceListener
    {
        private readonly StreamWriter _writer;
        public EwsListener(StreamWriter writer) => _writer = writer;

        public void Trace(string traceType, string traceMessage)
        {
            lock (_writer)
            {
                _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [EWS-{traceType}] {traceMessage}");
            }
        }
    }
}
