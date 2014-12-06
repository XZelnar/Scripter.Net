using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet
{
    class CallStack
    {
        public Stack<Structure.CommandStructure> stack = new Stack<Structure.CommandStructure>();
        internal object funcReturn = null;

        public void Clear()
        {
            stack.Clear();
            funcReturn = null;
        }

        internal object getFuncReturn()
        {
            var a = funcReturn;
            funcReturn = null;
            return a;
        }

        internal int[] GetExecutingPos()
        {
            if (stack.Peek() == null)
                return null;
            return stack.Peek().GetExecutingPosition();
        }
    }
}
