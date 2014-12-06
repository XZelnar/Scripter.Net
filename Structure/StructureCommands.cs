using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureCommands : CommandStructure
    {
        public List<CommandStructure> commands = new List<CommandStructure>();
        public bool multiLine = true;

        public CommandStructure lastExecutedCommand = null;

        public override int Execute()
        {
            return Execute(0);
        }

        public int Execute(int start, bool top = false)
        {
            lastExecutedCommand = null;
            if (start < 0 || start >= commands.Count)
                return 0;
            parent._callStack.stack.Push(this);
            parent._variables.PushScope();

            int t = 0;
            for (int i = start; i < commands.Count; i++)
            {
                lastExecutedCommand = commands[i];
                if (commands[i] is CommandBreak || commands[i] is CommandContinue)
                {
                    t = commands[i].Execute();
                    goto End;
                }
                t = commands[i].Execute();
                if (t != 0)
                    goto End;
            }

        End:
            if (!top)
                parent._variables.PopScope();
            parent._callStack.stack.Pop();
            return t;
        }

        public virtual void AddCommand(CommandStructure s)
        {
            commands.Add(s);
        }

        public virtual CommandStructure GetLastCommand()
        {
            return commands.Count == 0 ? null : commands[commands.Count - 1];
        }

        public override void Parse(ScripterVM vm)
        {
            parent = vm;
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].Parse(vm);
            }
        }

        public override void PostLoad(ScripterVM vm)
        {
            base.PostLoad(vm);
            for (int i = 0; i < commands.Count; i++)
                commands[i].PostLoad(vm);
        }
    }
}
