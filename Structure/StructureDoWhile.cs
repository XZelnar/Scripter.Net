using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureDoWhile : StructureCommands
    {
        public override int Execute()
        {
            parent._callStack.stack.Push(this);
            parent._variables.PushScope();
            object a = null;
            int i = 0;
            int t = 0;
            while (true)
            {
                t = base.Execute();
                if (t > 0)
                {
                    t--;
                    goto End;
                }
                else if (t == -1)
                    t = 0;

                try
                {
                    a = command.Execute();
                    parent.e_InvokeDebug(c_command);
                }
                catch (Exception e)
                {
                    if (!e.Message.StartsWith("["))
                        throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + e.Message, e);
                    else
                        throw e;
                }
                if (a == null || !(a is bool))
                    throw new NullReferenceException("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "Only expressions resulting in bool can be used in \"while\" statement condition");
                if (!(bool)a)
                    break;

                if (i++ > parent.infiniteLoopControl)
                {
                    throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "Number of iterations has surpassed the acceptable value. This is usually an indicator of infinite loop.");
                }
            }
        End:
            parent._callStack.stack.Pop();
            parent._variables.PopScope();
            return t;
        }

        public override void Parse(ScripterVM vm)
        {
            base.Parse(vm);
            command = vm._parser.Parse(c_command);
        }
    }
}
