using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class CommandBreak : CommandStructure
    {
        public int number = 1;

        public override int Execute()
        {
            return number;
        }

        public override void Parse(ScripterVM vm)
        {
        }
    }
}
