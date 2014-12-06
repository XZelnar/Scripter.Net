using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureIf : StructureCommands
    {
        public StructureCommands IfTrue = new StructureCommands(), IfFalse = new StructureCommands();
        public bool b_SingleTrue = false, b_SingleFalse = false;
        public bool curTrue = true;

        public override int Execute()
        {
            parent._callStack.stack.Push(this);
            parent._variables.PushScope();
            object a = null;
            try
            {
                a = command.Execute();
            }
            catch (Exception e)
            {
                if (!e.Message.StartsWith("["))
                    throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + e.Message, e);
                else
                    throw e;
            }
            if (!(a is bool))
                throw new NullReferenceException("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "Only expressions resulting in bool can be used in \"if\" statement condition");

            int t = 0;
            if ((bool)a)
                t = IfTrue.Execute();
            else if (IfFalse.commands.Count != 0)
                t = IfFalse.Execute();

            parent._callStack.stack.Pop();
            parent._variables.PopScope();
            return t;
        }

        public override void AddCommand(CommandStructure s)
        {
            if (curTrue)
                IfTrue.AddCommand(s);
            else
                IfFalse.AddCommand(s);
        }

        public override CommandStructure GetLastCommand()
        {
            if (curTrue)
                return IfTrue.GetLastCommand();
            else
                return IfFalse.GetLastCommand();
        }

        public override void Parse(ScripterVM vm)
        {
            parent = vm;
            command = vm._parser.Parse(c_command);
            IfTrue.Parse(vm);
            IfFalse.Parse(vm);
        }

        public override void PostLoad(ScripterVM vm)
        {
            base.PostLoad(vm);
            IfTrue.PostLoad(vm);
            IfFalse.PostLoad(vm);
        }
    }
}
