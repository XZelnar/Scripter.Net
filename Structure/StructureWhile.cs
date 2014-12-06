using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureWhile : StructureCommands
    {
        public override int Execute()
        {
            parent._callStack.stack.Push(this);
            parent._variables.PushScope();
            object a = null;
            int i = 0;
            int b = 0;
            while (true)
            {
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
                if ((bool)a)
                {
                    b = base.Execute();
                    if (b > 0)
                    {
                        b--;
                        goto End;
                    }
                    else if (b == -1)
                        b = 0;
                }
                else
                    break;

                if (i++ > parent.infiniteLoopControl)
                {
                    throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "Number of iterations has surpassed the acceptable value. This is usually an indicator of infinite loop.");
                }
            }

        End: 
            parent._callStack.stack.Pop();
            parent._variables.PopScope();
            return b;
        }

        public override void Parse(ScripterVM vm)
        {
            base.Parse(vm);
            command = vm._parser.Parse(c_command);
        }
    }
}
