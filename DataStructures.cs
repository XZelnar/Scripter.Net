using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ScripterNet
{
    interface IParameters
    {
        List<CallTreeNode> parameters
        {
            get;
            set;
        }
    }



    [Serializable]
    class Command
    {
        public String text, origText;
        public int lineStart, posStart;

        public Command() { }

        public Command(String txt, int line, int pos)
        {
            text = txt;
            lineStart = line;
            posStart = pos;
        }

        public String errorOutput(int del = 0)
        {
            return "[" + (lineStart + 1) + ";" + (posStart + del) + "] ";
        }



        #region StringImplements
        public int Length
        {
            get { return text.Length; }
        }

        public char this[int index]
        {
            get { return text[index]; }
        }

        public String Trim()
        {
            return text.Trim();
        }

        public String Substring(int start)
        {
            return text.Substring(start);
        }

        public String Substring(int start, int length)
        {
            return text.Substring(start, length);
        }

        public int IndexOf(char c)
        {
            return text.IndexOf(c);
        }

        public int IndexOf(string c)
        {
            return text.IndexOf(c);
        }

        public int IndexOf(char c, int start)
        {
            return text.IndexOf(c, start);
        }

        public int IndexOf(string c, int start)
        {
            return text.IndexOf(c, start);
        }

        public int IndexOfAny(char[] c)
        {
            return text.IndexOfAny(c);
        }

        public int IndexOfAny(char[] c, int start)
        {
            return text.IndexOfAny(c, start);
        }

        public int LastIndexOf(char c)
        {
            return text.LastIndexOf(c);
        }

        public int LastIndexOf(char c, int start)
        {
            return text.LastIndexOf(c, start);
        }
        #endregion
    }

    [Serializable]
    class CallTree
    {
        internal List<CallTreeNode> nodes;
        List<UnaryNodePost> postNodes;
        CallTreeNode trunk;
        internal int lineStart = 0, posStart = 0;

        public int NodesCount
        {
            get { return nodes.Count + postNodes.Count; }
        }

        public CallTree()
        {
            nodes = new List<CallTreeNode>();
            postNodes = new List<UnaryNodePost>();
            trunk = null;
        }

        public void Add(CallTreeNode node)
        {
            nodes.Add(node);
        }

        public void AddPostNode(UnaryNodePost node)
        {
            postNodes.Add(node);
        }

        public CallTreeNode GetNode(int index)
        {
            return nodes[index];
        }

        public void FindTrunk()
        {
            trunk = nodes[0];
            while (trunk.parent != null)
                trunk = trunk.parent;
        }

        public object Execute()
        {
            var a = trunk.Execute();
            postExecute();
            return a;
        }

        public void PostLoad(ScripterVM vm)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is NodeFunction)
                    (nodes[i] as NodeFunction).VM = vm;
                else if (nodes[i] is NodeIdentifyerObject)
                    (nodes[i] as NodeIdentifyerObject).vars = vm._variables;
            }
        }

        private void postExecute()
        {
            for (int i = 0; i < postNodes.Count; i++)
            {
                postNodes[i].PostExecute();
            }
        }
    }



    #region AbstractNodes
    [Serializable]
    abstract class CallTreeNode
    {
        public CallTreeNode parent = null;
        public int id;

        public abstract object Execute();
    }

    [Serializable]
    abstract class UnaryNode : CallTreeNode
    {
        public CallTreeNode child1;
    }

    [Serializable]
    abstract class BinaryNode : CallTreeNode
    {
        public CallTreeNode child1, child2;
    }

    [Serializable]
    abstract class UnaryNodePost : UnaryNode
    {
        public abstract void PostExecute();
    }

    [Serializable]
    abstract class NodeParameters : CallTreeNode, IParameters
    {
        private List<CallTreeNode> _parameters = new List<CallTreeNode>();

        public List<CallTreeNode> parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }
    }

    [Serializable]
    abstract class NodeIdentifyer : CallTreeNode
    {
        public abstract Type GetObjectType();
    }
    #endregion

    #region Identifyers
    [Serializable]
    class NodeIdentifyerString : NodeIdentifyer
    {
        public String identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(String);
        }
    }

    [Serializable]
    class NodeIdentifyerNull : NodeIdentifyer
    {
        public override object Execute()
        {
            return null;
        }

        public override Type GetObjectType()
        {
            return typeof(Nullable);
        }
    }

    [Serializable]
    class NodeIdentifyerInt32 : NodeIdentifyer
    {
        public int identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(int);
        }
    }

    [Serializable]
    class NodeIdentifyerInt64 : NodeIdentifyer
    {
        public long identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(long);
        }
    }

    [Serializable]
    class NodeIdentifyerFloat : NodeIdentifyer
    {
        public float identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(float);
        }
    }

    [Serializable]
    class NodeIdentifyerDouble : NodeIdentifyer
    {
        public double identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(double);
        }
    }

    [Serializable]
    class NodeIdentifyerChar : NodeIdentifyer
    {
        public char identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(char);
        }
    }

    [Serializable]
    class NodeIdentifyerBool : NodeIdentifyer
    {
        public bool identifyer;

        public override object Execute()
        {
            return identifyer;
        }

        public override Type GetObjectType()
        {
            return typeof(bool);
        }
    }

    [Serializable]
    class NodeIdentifyerObject : NodeIdentifyer
    {
        public String identifyer;
        public object value;
        public object objParent;
        public object info;
        public bool _VMVar = false;//false if gotten from reflection. true if gotten from internal VM var storage
        public virtual bool VMVar
        {
            get { return _VMVar; }
            set { _VMVar = value; }
        }
        public Type VMDeclareType = null;

        [NonSerialized]
        public VariablesEngine vars;

        public override object Execute()
        {
            info = ReflectionHelper.GetFieldOrPropertyFromFullPath(identifyer, out value, out objParent);
            if (info == null)
            {
                VMVar = true;

                var a = identifyer.Split('.');
                if (a.Length > 1)
                {
                    if (vars.Contains(a[0].Trim()))
                    {
                        object p = vars.GetVariable(a[0].Trim());
                        try
                        {
                            info = ReflectionHelper.GetObjectFieldOrProperty(identifyer.Substring(a[0].Length + 1).Trim(), p, out value, out objParent);
                        }
                        catch (NullReferenceException)
                        {
                            throw new Exception("Trying to access \"" + identifyer + "\", which is null");
                        }
                        return value;
                    }
                }

                if (vars.Contains(identifyer))
                    if (VMDeclareType == null)
                        value = vars.GetVariable(identifyer);
                    else
                        throw new Exception("Variable named \"" + identifyer + "\" already exists in the current scope");
                else if (VMDeclareType != null)
                {
                    if (VMDeclareType.IsArray || VMDeclareType.IsClass)//accepts null
                        vars.Create(identifyer, VMDeclareType, null);
                    else
                        vars.Create(identifyer, VMDeclareType, Activator.CreateInstance(VMDeclareType));
                    value = vars.GetVariable(identifyer);
                }
                else
                    throw new Exception("Unknown identifyer: " + identifyer);
            }
            return value;
        }

        public override Type GetObjectType()
        {
            if (VMVar)
            {
                if (vars.Contains(identifyer))
                {
                    var a = vars.GetType(identifyer);
                    return (a == typeof(VariablesEngine.Var) || vars.IsDynamic(identifyer)) ? typeof(object) : a;
                }
                else
                    if (info is FieldInfo)
                        return (info as FieldInfo).FieldType;
                    else
                        return (info as PropertyInfo).PropertyType;
                    //if (value != null)
                    //    return value.GetType();
                    //else
                    //    return null;
            }
            else
                if (info is System.Reflection.FieldInfo)
                    return (info as System.Reflection.FieldInfo).FieldType;
                else if (info is System.Reflection.PropertyInfo)
                    return (info as System.Reflection.PropertyInfo).PropertyType;
                else if (info is Type)
                    return info as Type;
                else
                    return null;
        }

        public virtual void SetValue(object value)
        {
            if (info == null)
            {
                if (VMVar)
                {
                    vars.SetVariable(identifyer, value);
                    this.value = value;
                }
                return;
            }
            if (info is System.Reflection.FieldInfo)
            {
                (info as System.Reflection.FieldInfo).SetValue(objParent, value);
                this.value = value;
            }
            if (info is System.Reflection.PropertyInfo)
            {
                (info as System.Reflection.PropertyInfo).SetValue(objParent, value, null);
                this.value = value;
            }
        }
    }

    [Serializable]
    class NodeChainedIdentifyerObject : NodeIdentifyerObject
    {
        public CallTreeNode parentNode;

        public override object Execute()
        {
            var a = parentNode.Execute();
            info = ReflectionHelper.GetObjectFieldOrProperty(identifyer, a, out value, out objParent);
            return value;
        }

        public override Type GetObjectType()
        {
            if (info is System.Reflection.FieldInfo)
                return (info as System.Reflection.FieldInfo).FieldType;
            else if (info is System.Reflection.PropertyInfo)
                return (info as System.Reflection.PropertyInfo).PropertyType;
            else
                return null;
        }

        public override void SetValue(object value)
        {
            if (info is System.Reflection.FieldInfo)
            {
                (info as System.Reflection.FieldInfo).SetValue(objParent, value);
                this.value = value;
            }
            if (info is System.Reflection.PropertyInfo)
            {
                (info as System.Reflection.PropertyInfo).SetValue(objParent, value, null);
                this.value = value;
            }
        }
    }

    [Serializable]
    class NodeArrayIndexer : NodeIdentifyerObject, IParameters
    {
        private List<CallTreeNode> _parameters = new List<CallTreeNode>();
        public List<CallTreeNode> parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }
        public dynamic[] par;
        dynamic p;

        public override bool VMVar
        {
            get
            {
                return (child1 as NodeIdentifyerObject).VMVar;
            }
        }

        public NodeIdentifyer child1;
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Cannot use non-identifyer as an array");
            if (parameters.Count == 0)
                throw new Exception("Cannot access array without any indices");
            if (parameters.Count > 8)
                throw new Exception("Cannot use arrays with more than 8 dimentions");
            p = child1.Execute();
            if (p == null)
                throw new NullReferenceException("Cannot use \"null\" as an array");
            par = new dynamic[parameters.Count];
            for (int i = 0; i < par.Length; i++)
            {
                par[i] = parameters[i].Execute();
                if (par[i] == null)
                    throw new Exception("Array index cannot be \"null\"");
            }
            try
            {
                value = null;
                if (par.Length == 1)
                    value = p[par[0]];
                if (par.Length == 2)
                    value = p[par[0], par[1]];
                if (par.Length == 3)
                    value = p[par[0], par[1], par[1]];
                if (par.Length == 4)
                    value = p[par[0], par[1], par[2], par[3]];
                if (par.Length == 5)
                    value = p[par[0], par[1], par[2], par[3], par[4]];
                if (par.Length == 6)
                    value = p[par[0], par[1], par[2], par[3], par[4], par[5]];
                if (par.Length == 7)
                    value = p[par[0], par[1], par[2], par[3], par[4], par[5], par[6]];
                if (par.Length == 8)
                    value = p[par[0], par[1], par[2], par[3], par[4], par[5], par[6], par[7]];
                return value;
            }
            catch (Exception e)
            {
                String s;
                if (e is IndexOutOfRangeException)
                {
                    s = par[0] == null ? "null" : par[0].GetType().ToString();
                    for (int i = 1; i < par.Length; i++)
                        s += ", " + par[i] == null ? "null" : par[i].ToString();
                    throw new Exception("Index out of range: [" + s + "]");
                }
                s = par[0] == null ? "null" : par[0].GetType().ToString();
                for (int i = 1; i < par.Length; i++)
                    s += ", " + par[i] == null ? "null" : par[i].GetType().ToString();
                throw new Exception("Error accessing array of type \"" + p.GetType() + "\" with parameters [" + s + "]");
            }
        }

        public override Type GetObjectType()
        {
            if (value == null)
                if ((child1 as NodeIdentifyerObject).VMVar)
                    return (child1 as NodeIdentifyerObject).GetObjectType().GetElementType();
                else
                    return null;
            else
                return value.GetType();
        }

        public override void SetValue(dynamic value)
        {
            if (par.Length == 1)
                p[par[0]] = value;
            if (par.Length == 2)
                p[par[0], par[1]] = value;
            if (par.Length == 3)
                p[par[0], par[1], par[2]] = value;
            if (par.Length == 4)
                p[par[0], par[1], par[2], par[3]] = value;
            if (par.Length == 5)
                p[par[0], par[1], par[2], par[3], par[4]] = value;
            if (par.Length == 6)
                p[par[0], par[1], par[2], par[3], par[4], par[5]] = value;
            if (par.Length == 7)
                p[par[0], par[1], par[2], par[3], par[4], par[5], par[6]] = value;
            if (par.Length == 8)
                p[par[0], par[1], par[2], par[3], par[4], par[5], par[6], par[7]] = value;
        }
    }
    #endregion

    #region General
    [Serializable]
    class NodeConstructor : NodeParameters
    {
        public Type type;

        public override object Execute()
        {
            object[] par = new object[parameters.Count];
            Type[] parT = new Type[par.Length];
            for (int i = 0; i < par.Length; i++)
            {
                par[i] = parameters[i].Execute();
                parT[i] = par[i] == null ? typeof(Nullable) : par[i].GetType();
            }
            if (!type.IsClass && par.Length == 0)
                return Activator.CreateInstance(type);
            var c = type.GetConstructor(parT);
            if (c == null)
            {
                String p = "";
                if (par.Length > 0)
                    p = (par[0] == null ? "null" : par[0].GetType().ToString());
                for (int i = 1; i < par.Length; i++)
                    p += ", " + (par[i] == null ? "null" : par[i].GetType().ToString());
                throw new Exception("Error executing constructor " + type.ToString() + "(" + p + ")");
            }
            return c.Invoke(par);
            
        }
    }

    [Serializable]
    class NodeArrayConstructor : NodeConstructor
    {
        public List<int> dimentions = new List<int>();
        int[][] par;
        List<Type> ts;

        public override object Execute()
        {
            par = new int[dimentions.Count][];
            object a;
            Type t = type;
            ts = new List<Type>();
            ts.Add(t);
            int cur = parameters.Count - 1;

            #region GetRecursiveTypes
            for (int i = dimentions.Count - 1; i >= 1; i--)
            {
                t = Array.CreateInstance(t, new int[dimentions[i]]).GetType();
                ts.Add(t);
            }
            #endregion

            #region GetParameters
            for (int i = dimentions.Count - 1; i >= 0; i--)
            {
                par[i] = new int[dimentions[i]];
                for (int j = dimentions[i] - 1; j >= 0; j--)
                {
                    if (parameters[cur] == null)
                    {
                        par[i][j] = 0;
                        cur--;
                        continue;
                    }
                    a = parameters[cur].Execute();
                    if (a == null)
                        throw new Exception("Array dimension cannot be null");
                    if (!ReflectionHelper.IsAssignable(typeof(int), a.GetType()))
                        throw new Exception("Array dimension must be a numbers");
                    par[i][j] = (int)a;
                    cur--;
                }
            }
            #endregion

            if (par[0][0] == 0)
                throw new Exception("Cannot declare array with no dimensions specified");

            try
            {
                return Array.CreateInstance(ts[ts.Count - 1], par[0]);
            }
            catch
            {
                throw new Exception("Error creating array");
            }
        }
    }

    [Serializable]
    class NodeFunction : NodeParameters
    {
        public string funcName = "";
        public Type funcType;
        public CallTreeNode funcParent;
        public bool isInternal = false;
        [NonSerialized]
        public ScripterVM VM;

        public override object Execute()
        {
            object[] pars = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
                pars[i] = parameters[i].Execute();

            if (isInternal)
            {
                return VM._functions.Invoke(funcName, pars);
            }
            else
            {
                if (funcParent == null)
                {
                    var m = ReflectionHelper.GetMethod(funcName, funcType, pars);
                    object r = m.Invoke(null, pars);
                    return r;
                }
                else
                {
                    object a = funcParent.Execute();
                    Type t;
                    if (a == null)
                        if (funcParent is NodeIdentifyerObject)
                            throw new Exception("Use of unassigned wariable: \"" + (funcParent as NodeIdentifyerObject).identifyer + "\"");
                        else
                            throw new NullReferenceException("\"null\" contains no functions");
                    else
                        t = a.GetType();
                    var m = ReflectionHelper.GetMethod(funcName, t, pars);
                    object r = m.Invoke(a, pars);
                    return r;
                }
            }
        }
    }

    [Serializable]
    class NodeMultipleDeclarations : CallTreeNode
    {
        public List<CallTreeNode> children = new List<CallTreeNode>();

        public override object Execute()
        {
            for (int i = 0; i < children.Count; i++)
                children[i].Execute();
            return null;
        }
    }

    [Serializable]
    class NodeConditional : CallTreeNode
    {
        public CallTreeNode condition, nodeTrue, nodeFalse;

        public override object Execute()
        {
            object a = condition.Execute();
            if (!(a is bool))
                throw new Exception("Only bool is accepted as condition for \"?\" operator");
            if ((bool)a)
                return nodeTrue.Execute();
            else
                return nodeFalse.Execute();
        }
    }

    [Serializable]
    class NodeNOP : CallTreeNode
    {
        public override object Execute()
        {
            return null;
        }
    }

    [Serializable]
    class NodeTypeOf : CallTreeNode
    {
        public Type type;

        public override object Execute()
        {
            return type;
        }
    }
    #endregion

    #region Unary
    [Serializable]
    class NodeExplicitConversion : UnaryNode
    {
        public Type toType;

        public override object Execute()
        {
            object a = child1.Execute();
            if (a == null)
                return null;
            return ReflectionHelper.DoConvert(a, toType);
        }
    }

    [Serializable]
    class NodeIncrement : UnaryNodePost
    {
        public bool pre;

        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Increment can only be made to objects");
            dynamic a = child1.Execute();
            if (a == null)
                throw new NullReferenceException("Cannot increment \"null\"");
            if (!pre)
                return a;
            try
            {
                (child1 as NodeIdentifyerObject).SetValue(a + 1);
            }
            catch
            {
                throw new Exception("Increment is not defined for type \"" + a.GetType().ToString() + "\"");
            }
            return a + 1;
        }

        public override void PostExecute()
        {
            if (pre)
                return;
            if ((child1 as NodeIdentifyerObject).value == null)
                throw new NullReferenceException("Cannot increment \"null\"");
            dynamic a = child1.Execute();
            try
            {
                (child1 as NodeIdentifyerObject).SetValue(a + 1);
            }
            catch
            {
                throw new Exception("Increment is not defined for type \"" + a.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeDecrement : UnaryNodePost
    {
        public bool pre;

        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Decrement can only be made to objects");
            dynamic a = child1.Execute();
            if (a == null)
                throw new NullReferenceException("Cannot decrement \"null\"");
            if (!pre)
                return a;
            try
            {
                (child1 as NodeIdentifyerObject).SetValue(a - 1);
            }
            catch
            {
                throw new Exception("Decrement is not defined for type \"" + a.GetType().ToString() + "\"");
            }
            return a - 1;
        }

        public override void PostExecute()
        {
            if (pre)
                return;
            if ((child1 as NodeIdentifyerObject).value == null)
                throw new NullReferenceException("Cannot decrement \"null\"");
            dynamic a = child1.Execute();
            try
            {
                (child1 as NodeIdentifyerObject).SetValue(a - 1);
            }
            catch
            {
                throw new Exception("Decrement is not defined for type \"" + a.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeNOT : UnaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            if (a == null)
                throw new NullReferenceException("\"null\" is not a suitable argument for a \"!\" operator");
            try
            {
                return !a;
            }
            catch
            {
                throw new Exception("\"!\" operator is not defined for type \"" + a.GetType() + "\"");
            }
        }
    }

    [Serializable]
    class NodeBitwiseNOT : UnaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            if (a == null)
                throw new NullReferenceException("\"null\" is not a suitable argument for a \"~\" operation");
            try
            {
                return ~a;
            }
            catch
            {
                throw new Exception("\"~\" operator is not defined for type \"" + a.GetType() + "\"");
            }
        }
    }

    [Serializable]
    class NodeUnaryPlus : UnaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            if (a == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for unary \"+\" operation");
            try
            {
                return +a;
            }
            catch
            {
                throw new Exception("Unary \"+\" operator is not defined for type \"" + a.GetType() + "\"");
            }
        }
    }

    [Serializable]
    class NodeUnaryMinus : UnaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            if (a == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for unary \"-\" operation");
            try
            {
                return -a;
            }
            catch
            {
                throw new Exception("Unary \"-\" operator is not defined for type \"" + a.GetType() + "\"");
            }
        }
    }
    #endregion

    #region Binary
    [Serializable]
    class NodeMultiplication : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" cannot be multiplication factor");
            try
            {
                return a * b;
            }
            catch
            {
                throw new Exception("\"*\" oprator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeDivision : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a division operation");
            try
            {
                return a / b;
            }
            catch
            {
                throw new Exception("\"/\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeRemainder : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"%\" operation");
            try
            {
                return a % b;
            }
            catch
            {
                throw new Exception("\"%\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeAddition : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a binary \"+\" operation");
            try
            {
                return a + b;
            }
            catch
            {
                throw new Exception("Binary \"+\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeSubtraction : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a binary \"-\" operation");
            try
            {
                return a - b;
            }
            catch
            {
                throw new Exception("Binary \"-\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeLeftShift: BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"<<\" operation");
            try
            {
                return a << b;
            }
            catch
            {
                throw new Exception("\"<<\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeRightShift : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \">>\" operation");
            try
            {
                return a >> b;
            }
            catch
            {
                throw new Exception("\">>\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeIsLess : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"<\" operation");
            try
            {
                return a < b;
            }
            catch
            {
                throw new Exception("\"<\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeIsMore : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \">\" operation");
            try
            {
                return a > b;
            }
            catch
            {
                throw new Exception("\">\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeIsLessOrEqual : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"<=\" operation");
            try
            {
                return a <= b;
            }
            catch
            {
                throw new Exception("\"<=\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeIsMoreOrEqual : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \">=\" operation");
            try
            {
                return a >= b;
            }
            catch
            {
                throw new Exception("\">=\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeIsEqual : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            return a == b;
        }
    }

    [Serializable]
    class NodeIsNotEqual : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            return a != b;
        }
    }

    [Serializable]
    class NodeLogicalAND : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"&\" operation");
            try
            {
                return a & b;
            }
            catch
            {
                throw new Exception("\"&\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeLogicalXOR : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"^\" operation");
            try
            {
                return a ^ b;
            }
            catch
            {
                throw new Exception("\"^\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeLogicalOR : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"|\" operation");
            try
            {
                return a | b;
            }
            catch
            {
                throw new Exception("\"|\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeConditionalAND : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"&&\" operation");
            try
            {
                return a && b;
            }
            catch
            {
                throw new Exception("\"&&\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeConditionalOR : BinaryNode
    {
        public override object Execute()
        {
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (a == null || b == null)
                throw new NullReferenceException("\"null\" is not a suitable parameter for a \"||\" operation");
            try
            {
                return a || b;
            }
            catch
            {
                throw new Exception("\"||\" operator is not defined for types \"" + a.GetType().ToString() + "\" and \"" + b.GetType().ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeAs : BinaryNode
    {
        public override object Execute()
        {
            if (!(child2 is NodeIdentifyerObject))
                throw new Exception("Type expected as a parameter for a \"as\" operator");
            dynamic a = child1.Execute();
            child2.Execute();
            object b = (child2 as NodeIdentifyerObject).info;
            if (!(b is Type))
                throw new Exception("Type expected as a parameter for a \"as\" operator");
            if (a == null)
                return null;
            try
            {
                return ReflectionHelper.DoConvert(a, (Type)b);
            }
            catch
            {
                throw new Exception("Cannot convert \"" + a.GetType().ToString() + "\" to \"" + ((Type)b).ToString() + "\"");
            }
        }
    }

    [Serializable]
    class NodeIs : BinaryNode
    {
        public override object Execute()
        {
            if (!(child2 is NodeIdentifyerObject))
                throw new Exception("Type expected as a parameter for a \"is\" operator");
            dynamic a = child1.Execute();
            child2.Execute();
            object b = (child2 as NodeIdentifyerObject).info;
            if (!(b is Type))
                throw new Exception("Type expected as a parameter for a \"is\" operator");
            if (a == null)
                throw new Exception("\"is\" operator is not applicable to null");
            try
            {
                object t = ReflectionHelper.DoConvert(a, (Type)b);
                return t != null;
            }
            catch
            {
                return false;
            }
        }
    }
    #endregion

    #region Assignments
    [Serializable]
    class NodeAssignment : BinaryNode
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            //var t = (child1 as NodeIdentifyer).GetObjectType();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())) && !(b == null && (child1 as NodeIdentifyer).GetObjectType().IsClass))
                throw new Exception("Cannot implicitly convert \"" + (b == null ? typeof(object) : b.GetType()).ToString() + 
                    "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            (child1 as NodeIdentifyerObject).SetValue(b);
            return b;
        }
    }

    [Serializable]
    class NodeAssignmentMul : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a * b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentDiv : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a / b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentRemainder : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a % b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentPlus : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a + b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentMinus : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a - b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentLeftShift : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a << b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentRightShift : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a - b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentAND : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a & b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentXOR : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a ^ b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }

    [Serializable]
    class NodeAssignmentOR : NodeAssignment
    {
        public override object Execute()
        {
            if (!(child1 is NodeIdentifyerObject))
                throw new Exception("Assignment can only be made to objects");
            dynamic a = child1.Execute();
            dynamic b = child2.Execute();
            if (!ReflectionHelper.IsAssignable((child1 as NodeIdentifyer).GetObjectType(), (b == null ? typeof(object) : b.GetType())))
                throw new Exception("Cannot implicitly convert \"" + b.GetType().ToString() + "\" to \"" + (child1 as NodeIdentifyer).GetObjectType().ToString() + "\"");
            object t = a | b;
            (child1 as NodeIdentifyerObject).SetValue(t);
            return t;
        }
    }
    #endregion
}
