using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet
{
    class VariablesEngine
    {
        internal class Var { }
        internal class Dynamic { }

        struct Variable : IComparable
        {
            public object value;
            public bool IsDynamic;
            public Type type;

            public int CompareTo(Object a)
            {
                return ((a is Variable) &&
                    ((Variable)a).value == value &&
                    ((Variable)a).IsDynamic == IsDynamic &&
                    ((Variable)a).type == type) ? 1 : 0;
            }
        }

        private Dictionary<String, Variable> variables = new Dictionary<string, Variable>();
        private Dictionary<String, Variable> localVariables = new Dictionary<string, Variable>();
        internal ScripterVM parent;



        public void Clear()
        {
            lock (variables) lock (localVariables)
                {
                    localVariables.Clear();
                    variables.Clear();
                    scopes.Clear();
                    scopesCount = 0;
                }
        }



        public bool Contains(String name)
        {
            lock (variables) lock (localVariables)
                    return variables.Keys.Contains(name) || localVariables.Keys.Contains(name);
        }

        public void Create(String name, Type type, Object value = null)
        {
            if (Contains(name))
                throw new Exception("Variable named \"" + name + "\" already exists in current scope");
            Variable v = new Variable() { type = type, value = null, IsDynamic = type == typeof(Dynamic) };
            if (parent._callStack.stack.Count > 2)
                lock (localVariables)
                    localVariables.Add(name, v);
            else
                lock (variables)
                    variables.Add(name, v);
            if (value != null)
                SetVariable(name, value);
        }
        
        public object GetVariable(String name)
        {
            lock (variables) lock (localVariables)
                {
                    if (variables.Keys.Contains(name))
                        return variables[name].value;
                    else if (localVariables.Keys.Contains(name))
                        return localVariables[name].value;
                    else
                        throw new Exception("Unknown identifyer: \"" + name + "\"");
                }
        }

        public object GetVariable(String name, object[] indices)
        {
            lock (variables) lock (localVariables)
                {
                    if (variables.Keys.Contains(name))
                        return typeof(Variable).GetProperty("value").GetValue(variables[name], indices);
                    else if (localVariables.Keys.Contains(name))
                        return typeof(Variable).GetProperty("value").GetValue(localVariables[name], indices);
                    else
                        throw new Exception("Use of unassigned variable \"" + name + "\"");
                }
        }

        public void SetVariable(String name, Object value)
        {
            lock (variables) lock (localVariables)
                {
                    Variable a;
                    bool local = false;
                    if (variables.Keys.Contains(name))
                        a = variables[name];
                    else if (localVariables.Keys.Contains(name))
                    {
                        a = localVariables[name];
                        local = true;
                    }
                    else
                        throw new Exception("Use of unassigned variable \"" + name + "\"");
                    if (value == null && !a.type.IsClass)
                        throw new Exception("Cannot assign \"null\" to a variable \"" + name + "\" of a non-nullable type \"" + a.type.ToString() + "\"");
                    else if (a.type != typeof(Var) && !a.IsDynamic && !ReflectionHelper.IsAssignable(a.type, value == null ? typeof(object) : value.GetType()) && 
                        !(value == null && a.type.IsClass))
                        throw new Exception("Cannot implicitly convert \"" + value.GetType().ToString() + "\" to \"" + a.type.ToString() + "\"");
                    if (a.CompareTo(value) == 0)
                    {
                        var old = a.value;
                        a.value = value;
                        if (a.type == typeof(Var) || a.IsDynamic)
                            a.type = value == null ? typeof(object) : value.GetType();
                        if (local)
                            localVariables[name] = a;
                        else
                            variables[name] = a;
                        parent.e_InvokeVarChanged(name, old, value, a.type);
                    }
                }
        }

        public Type GetType(String name)
        {
            lock (variables) lock (localVariables)
                    if (variables.Keys.Contains(name))
                        return variables[name].type;
                    else if (localVariables.Keys.Contains(name))
                        return localVariables[name].type;
                    else
                        throw new Exception("Use of unassigned variable \"" + name + "\"");
        }

        public bool IsDynamic(String name)
        {
            lock (variables) lock (localVariables)
                    if (variables.Keys.Contains(name))
                        return variables[name].IsDynamic;
                    else if (localVariables.Keys.Contains(name))
                        return localVariables[name].IsDynamic;
                    else
                        throw new Exception("Use of unassigned variable \"" + name + "\"");
        }

        Stack<Dictionary<String, Variable>> scopes = new Stack<Dictionary<String, Variable>>();
        Stack<bool> deepSave = new Stack<bool>();
        int scopesCount = 0;

        internal void PushScope(bool PushValues = false, bool function = false)
        {
            Dictionary<String, Variable> t = new Dictionary<String, Variable>();
            lock(localVariables)
                for (int i = 0; i < localVariables.Keys.Count; i++)
                    t.Add(localVariables.Keys.ElementAt(i), localVariables.Values.ElementAt(i));
            if (function)
                localVariables.Clear();
            scopes.Push(t);
            deepSave.Push(PushValues);
            scopesCount++;
        }

        internal void PopScope()
        {
            if (scopesCount <= 0)
                throw new Exception("Engine exception 0x0A02: Cannot pop variables scope");

            scopesCount--;
            var t = scopes.Pop();
            bool b = deepSave.Pop();

            lock (localVariables)
            {
                for (int i = 0; i < localVariables.Count; i++)
                {
                    if (t.Keys.Contains(localVariables.Keys.ElementAt(i)))
                    {
                        if (b)
                            localVariables[localVariables.Keys.ElementAt(i)] = t[localVariables.Keys.ElementAt(i)];
                    }
                    else
                    {
                        localVariables.Remove(localVariables.Keys.ElementAt(i));
                        i--;
                    }
                }
                for (int i = 0; i < t.Count; i++)
                {
                    if (!localVariables.Keys.Contains(t.Keys.ElementAt(i)))
                    {
                        localVariables.Add(t.Keys.ElementAt(i), t.Values.ElementAt(i));
                    }
                }
            }
        }
    }
}
