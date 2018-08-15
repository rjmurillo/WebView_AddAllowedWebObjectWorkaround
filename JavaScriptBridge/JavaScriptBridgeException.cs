using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaScriptBridge
{
    internal class JavaScriptBridgeException : Exception
    {
        public JavaScriptBridgeException(string message)
        :base(message)
        {
            
        }
    }
}
