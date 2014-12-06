using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class CommandContinue : CommandStructure
    {
        public override int Execute()
        {
            return -1;
        }

        public override void Parse(ScripterVM vm)
        {
        }
    }
}
