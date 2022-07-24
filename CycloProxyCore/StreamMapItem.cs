using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycloProxyCore
{
    internal class StreamMapItem
    {
        public Stream ClientStream { get; set; }
        public Stream RemoteStream { get; set; }
    }
}
