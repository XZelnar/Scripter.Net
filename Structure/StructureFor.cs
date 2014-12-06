using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureFor : StructureCommands
    {
        //initial == base
        public CallTree condition = null, iteration = null;
        public Command c_Condition, c_Iteration;
        public bool b_SingleTrue = false, b_SingleFalse = false;
        public int cur = 0;
        [NonSerialized]
        int curStep = 0;

        public override int Execute()
        {
            curStep = 0;
            parent._callStack.stack.Push(this);
            parent._variables.PushScope();
            command.Execute();
            parent.e_InvokeDebug(c_command);
            object a = null;
            int i = 0;
            int b = 0;
            while (true)
            {
                curStep = 1;
                try
                {
                    a = condition.Execute();
                    parent.e_InvokeDebug(c_Condition);
                }
                catch (Exception e)
                {
                    if (!e.Message.StartsWith("["))
                        throw new Exception("[" + c_Condition.lineStart.ToString() + ", " + c_Condition.posStart.ToString() + "] " + e.Message, e);
                    else
                        throw e;
                }
                if (c_Condition.text == "")
                    a = true;
                if (a == null || !(a is bool))
                    throw new NullReferenceException("[" + c_Condition.lineStart.ToString() + ", " + c_Condition.posStart.ToString() + "] " + "Only expressions resulting in bool can be used in \"for\" statement condition");
                if ((bool)a)
                {
                    curStep = 2;
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
                try
                {
                    curStep = 3;
                    iteration.Execute();
                    parent.e_InvokeDebug(c_Iteration);
                }
                catch (Exception e)
                {
                    if (!e.Message.StartsWith("["))
                        throw new Exception("[" + c_Iteration.lineStart.ToString() + ", " + c_Iteration.posStart.ToString() + "] " + e.Message, e);
                    else
                        throw e;
                }

                if (i++ > parent.infiniteLoopControl)
                {
                    throw new Exception("[" + c_command.lineStart.ToString() + ", " + c_command.posStart.ToString() + "] " + "Number of iterations has surpassed the acceptable value. This is usually an indicator of infinite loop.");
                }
            }
        End:
            parent._variables.PopScope();
            parent._callStack.stack.Pop();
            return b;
        }

        public override void AddCommand(CommandStructure s)
        {
            if (cur == 0)
            {
                c_command = s.c_command;
                cur++;
            }
            else if (cur == 1)
            {
                c_Condition = s.c_command;
                cur++;
            }
            else if (cur == 2)
            {
                c_Iteration = s.c_command;
                cur++;
            }
            else if (cur == 3)
            {
                commands.Add(s);
            }
        }

        public override CommandStructure GetLastCommand()
        {
            if (cur != 3)
                return null;
            else
                return commands.Count == 0 ? null : commands[commands.Count - 1];
        }

        public override void Parse(ScripterVM vm)
        {
            command = vm._parser.Parse(c_command);
            condition = vm._parser.Parse(c_Condition);
            iteration = vm._parser.Parse(c_Iteration);
            base.Parse(vm);
        }

        public override void PostLoad(ScripterVM vm)
        {
            base.PostLoad(vm);
            iteration.PostLoad(vm);
            condition.PostLoad(vm);
        }

        public override int[] GetExecutingPosition()
        {
            if (curStep == 0)
                return new int[] { c_command.lineStart, c_command.posStart };
            if (curStep == 1)
                return new int[] { c_Condition.lineStart, c_Condition.posStart };
            if (curStep == 2)
                return base.GetExecutingPosition();
            if (curStep == 3)
                return new int[] { c_Iteration.lineStart, c_Iteration.posStart };
            return null;
        }
    }
}
