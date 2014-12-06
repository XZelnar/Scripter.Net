using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureSwitch : StructureCommands
    {
        private Dictionary<CallTree, int> caseIndices = new Dictionary<CallTree, int>();
        public Dictionary<Command, int> s_caseIndices = new Dictionary<Command, int>();
        public int def = -1;


        public override int Execute()
        {
            parent._callStack.stack.Push(this);
            parent._variables.PushScope();
            dynamic a = null, v = null;
            CallTree t;
            int ind = -1;

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

            for (int i = 0; i < caseIndices.Keys.Count; i++)
            {
                t = caseIndices.Keys.ElementAt(i);
                try
                {
                    v = t.Execute();
                    parent.e_InvokeDebug(s_caseIndices.Keys.ElementAt(i));
                }
                catch (Exception e)
                {
                    var cc = s_caseIndices.Keys.ElementAt(i);
                    throw new Exception("[" + cc.lineStart.ToString() + ", " + cc.posStart.ToString() + "] " + e.Message, e);
                }
                if (v == a)
                {
                    ind = caseIndices[t];
                    break;
                }
            }
            if (ind == -1 && def != -1)
                ind = def;

            if (ind == -1)
                ind = 0;
            else
            {
                ind = base.Execute(ind);
                if (ind > 0)
                    ind--;
            }

            parent._callStack.stack.Pop();
            parent._variables.PopScope();
            return ind;
        }

        public override void Parse(ScripterVM vm)
        {
            if (commands.Count == 0)
                throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "Cannot declare switch with no cases");

            base.Parse(vm);
            if (!(commands[0] is StructureCommands))
                throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "\"{\" required after switch");
            commands = (commands[0] as StructureCommands).commands;
            command = vm._parser.Parse(c_command);

            Command t;
            for (int i = 0; i < s_caseIndices.Keys.Count; i++)
                caseIndices.Add(vm._parser.Parse(t = s_caseIndices.Keys.ElementAt(i)), s_caseIndices[t]);
        }
    }
}
