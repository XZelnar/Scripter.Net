using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ScripterNet
{
    class FunctionsEngine
    {
        public Dictionary<String, MethodBase> functions = new Dictionary<string, MethodBase>();
        private Dictionary<String, Structure.StructureFunction> scriptedFunctions = new Dictionary<string, Structure.StructureFunction>();

        public void RegisterFunction(String name, MethodBase func)
        {
            if (functions.Keys.Contains(name) || scriptedFunctions.Keys.Contains(name))
                throw new Exception("Function named \"" + name + "\" already exists in current scope");
            lock (functions)
                functions.Add(name, func);
        }

        internal void AddScriptedFunction(String name, Structure.StructureFunction func)
        {
            if (functions.Keys.Contains(name) || scriptedFunctions.Keys.Contains(name))
                throw new Exception("Function named \"" + name + "\" already exists in current scope");
            lock (scriptedFunctions)
                scriptedFunctions.Add(name, func);
        }

        public void RemoveFunction(String name)
        {
            lock (functions)
                if (functions.Keys.Contains(name))
                    functions.Remove(name);
                else
                    throw new Exception("Function \"" + name + "\" not registered");
        }

        public void Clear()
        {
            scriptedFunctions.Clear();
        }

        public bool Contains(String name)
        {
            lock (functions)
                return functions.Keys.Contains(name) || scriptedFunctions.Keys.Contains(name);
        }

        public object Invoke(String name, object[] parameters)
        {
            lock (functions) 
                lock (scriptedFunctions)
                    if (scriptedFunctions.Keys.Contains(name))
                    {
                        var a = scriptedFunctions[name];
                        return a.Execute(parameters);
                    }
                    else if (functions.Keys.Contains(name))
                    {
                        var a = functions[name];
                        try
                        {
                            return a.Invoke(null, parameters);
                        }
                        catch
                        {
                            String pars = "";
                            if (parameters.Length >= 1)
                                pars = parameters[0] == null ? "null" : parameters[0].GetType().ToString();
                            for (int i = 1; i < parameters.Length; i++)
                                pars += ", " + (parameters[i] == null ? "null" : parameters[i].GetType().ToString());
                            throw new Exception("Error when invoking " + name + "(" + pars + ")");
                        }
                    }
                    else if (parameters.Length == 1 || parameters.Length == 2)
                    {
                        dynamic a = PreDefinedFunctions.processBaseFunction(name, parameters[0], parameters.Length == 1 ? null : parameters[1]);
                        if (a != null)
                            return a;
                        throw new Exception("Function not found: \"" + name + "\"");
                    }
                    else
                        throw new Exception("Function not found: \"" + name + "\"");
        }

        public void Reset()
        {
            lock (functions)
                functions.Clear();
        }
    }
}
