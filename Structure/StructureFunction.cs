using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet.Structure
{
    [Serializable]
    class StructureFunction : StructureCommands
    {
        public Type returnType;
        public String functionName = "";
        public Dictionary<String, Type> parameters = new Dictionary<String, Type>();

        public override int Execute()
        {
            return 0;
        }

        public object Execute(object[] pars)
        {
            if (parameters.Count != pars.Length)
                throw new Exception(errorPrefix(0, 0) + "Invalid number of parameters");
            parent._variables.PushScope(true, true);
            parent._callStack.stack.Push(this);
            Type t;
            for (int i = 0; i < pars.Length; i++)
            {
                t = parameters.Values.ElementAt(i);
                if (pars[i] == null && !t.IsClass)
                    throw new Exception(errorPrefix(0, 0) + "Cannot pass \"null\" to a non-nullable parameter \"" + 
                        parameters.Keys.ElementAt(i) + "\"");
                if (pars[i] != null)
                    pars[i] = ReflectionHelper.DoConvert(pars[i], t);

                parent._variables.Create(parameters.Keys.ElementAt(i), t, pars[i]);
            }

            try
            {
                base.Execute();
            }
            catch (Exception e)
            {
                if (!e.Message.StartsWith("["))
                {
                    String es = "";

                    //if (base.lastExecutedCommand == null || base.lastExecutedCommand.c_command == null)
                    //    ;//es = errorPrefix(0, 0);
                    //else
                    //    es = errorPrefix(lastExecutedCommand.c_command.lineStart, lastExecutedCommand.c_command.posStart);
                    if (base.lastExecutedCommand != null && base.lastExecutedCommand is StructureCommands &&
                        (base.lastExecutedCommand as StructureCommands).lastExecutedCommand != null &&
                        (base.lastExecutedCommand as StructureCommands).lastExecutedCommand.c_command != null)
                        es = errorPrefix((base.lastExecutedCommand as StructureCommands).lastExecutedCommand.c_command.lineStart, 
                            (base.lastExecutedCommand as StructureCommands).lastExecutedCommand.c_command.posStart);

                    es = e.Message.StartsWith("[") ? es + e.Message.Substring(e.Message.IndexOf(']') + 1) : es + e.Message;
                    throw new Exception(es, e);
                }
                else
                    throw e;
            }
            var a = parent._callStack.getFuncReturn();
            if (returnType == null && a != null)
                if (base.lastExecutedCommand == null)
                    throw new Exception(errorPrefix(c_command.lineStart, c_command.posStart) + "Cannot convert returned \"" + a.GetType() + "\" to \"void\"");
                else
                    throw new Exception(errorPrefix(lastExecutedCommand.c_command.lineStart, lastExecutedCommand.c_command.posStart) + 
                        "Cannot convert returned \"" + a.GetType() + "\" to \"void\"");

            if (returnType != null)
            {
                if (a == null)
                {
                    if (!returnType.IsClass && !returnType.IsArray)
                        if (base.lastExecutedCommand == null)
                            throw new NullReferenceException(errorPrefix(0, 0) + "Null returned for a non-nullable type");
                        else
                            throw new NullReferenceException(errorPrefix(lastExecutedCommand.c_command.lineStart, lastExecutedCommand.c_command.posStart) + 
                                "Null returned for a non-nullable type");
                }
                else
                    a = ReflectionHelper.DoConvert(a, returnType);
            }
            parent._variables.PopScope();
            parent._callStack.stack.Pop();
            return a;
        }

        public String errorPrefix(int l, int p)
        {
            String s = " ";
            for (int i = 0; i < parameters.Keys.Count; i++)
                s += parameters.Values.ElementAt(i).ToString() + ", ";
            if (s != "")
                s = s.Substring(0, s.Length - 2).Trim();
            if (l == 0 && p == 0)
                return "{" + functionName + "(" + s + ")} ";
            l -= c_command.lineStart;
            return "[" + l.ToString() + ", " + p.ToString() + "] in {" + functionName + "(" + s + ")} ";
        }

        public override void PostLoad(ScripterVM vm)
        {
            base.PostLoad(vm);
            vm._functions.AddScriptedFunction(functionName, this);
        }
    }
}
