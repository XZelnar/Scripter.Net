using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class CommandReturn : CommandStructure
    {
        public override int Execute()
        {
            object r = null;
            if (command != null)
                r = command.Execute();
            parent._callStack.funcReturn = r;
            return Int32.MaxValue;
        }
    }
}
