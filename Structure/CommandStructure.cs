using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class CommandStructure
    {
        [NonSerialized]
        protected ScripterVM parent;

        internal Command c_command;
        public CallTree command;

        //int for the amount of breaks
        public virtual int Execute()
        {
            parent._callStack.stack.Push(this);
            if (command != null)
                try
                {
                    command.Execute();
                }
                catch (Exception e)
                {
                    if (e is System.Threading.ThreadAbortException)
                        return Int32.MaxValue;
                    if (!e.Message.StartsWith("["))
                        throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + e.Message, e);
                    else
                        throw e;
                }
            parent.e_InvokeDebug(c_command);
            parent._callStack.stack.Pop();
            return 0;
        }

        public virtual void Parse(ScripterVM vm)
        {
            parent = vm;
            if (c_command != null)
                command = vm._parser.Parse(c_command);
        }

        public virtual void PostLoad(ScripterVM vm)
        {
            parent = vm;
            if (command != null)
                command.PostLoad(vm);
        }

        public virtual int[] GetExecutingPosition()
        {
            return new int[] { c_command.lineStart, c_command.posStart };
        }
    }
}
