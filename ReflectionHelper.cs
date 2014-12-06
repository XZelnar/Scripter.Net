using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ScripterNet
{
    static class ReflectionHelper
    {
        public static int a = 0;
        public static Processor b = null;

        internal static List<Assembly> assemblies = new List<Assembly>();

        public static object GetFieldOrPropertyFromFullPath(String s, out object value, out object parent)//ex. Program.Class.SomeClassInstance.SomeParameter.SomeOtherParameter
        {
            //Split by separator and check word by word for type. Then dig into it.
            var a = s.Split('.');
            String curStr = "", fullStr;
            int curInd = 0;
            Type curType = null;
            value = null;
            parent = null;

            //look for type
            while (curInd < a.Length && curType == null)
            {
                if (curInd == 0)
                    curStr += a[curInd];
                else
                    curStr += "." + a[curInd];
                curInd++;

                curType = GetType(curStr);
            }
            if (curType == null)
            {
                return null;
            }
            if (curInd >= a.Length)//if nothing beyond type
                return curType;
            fullStr = curStr;
            

            //look for top var

            //look for vars
            curStr = "";
            FieldInfo f = null;
            PropertyInfo p = null;
            while (curInd < a.Length)
            {
                fullStr += "." + a[curInd];

                f = GetField(a[curInd], curType);
                if (f == null)
                    p = GetProperty(a[curInd], curType);

                if (f == null && p == null)
                {
                    return null;
                }

                parent = value;
                if (f != null)//field found
                    value = GetValue(f, value);
                else//property found
                    value = GetValue(p, value);

                if (value == null)
                    throw new NullReferenceException();
                curType = value.GetType();

                curInd++;
            }

            return f == null ? (object)p : (object)f;
        }

        public static object GetObjectFieldOrProperty(String s, object initialParent, out object value, out object parent)//ex. Program.Class.SomeClassInstance.SomeParameter.SomeOtherParameter
        {
            //Split by separator and check word by word for type. Then dig into it.
            int curInd = 0;
            Type curType = null;
            value = null;
            parent = null;
            if (initialParent == null)
                return null;
            var a = s.Split('.');

            curType = initialParent.GetType();

            //look for vars
            FieldInfo f = null;
            PropertyInfo p = null;
            value = initialParent;
            while (curInd < a.Length)
            {
                f = GetField(a[curInd], curType);
                if (f == null)
                    p = GetProperty(a[curInd], curType);

                if (f == null && p == null)
                {
                    return null;
                }

                parent = value;
                if (f != null)//field found
                    value = GetValue(f, value);
                else//property found
                    value = GetValue(p, value);

                //if (value == null)//TODO watch
                //    throw new NullReferenceException();
                if (f != null)
                    curType = f.FieldType;
                else
                    curType = p.PropertyType;

                curInd++;
            }

            return f == null ? (object)p : (object)f;
        }

        public static MethodInfo GetMethod(String funcName, Type parent, object[] parameters)
        {
            Type[] parTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                parTypes[i] = parameters[i] == null ? typeof(object) : parameters[i].GetType();

            var funcs = parent.GetMethods();
            MethodInfo a = null;
            bool b = true;
            //search for function
            for (int i = 0; i < funcs.Length; i++)
                if (funcs[i].Name == funcName)
                {
                    var p = funcs[i].GetParameters();
                    if (p.Length == parameters.Length)
                    {
                        b = true;
                        for (int j = 0; j < p.Length; j++)
                        {
                            if (parameters[j] == null)
                            {
                                if (!p[j].ParameterType.IsClass && !p[j].ParameterType.IsArray)
                                {
                                    b = false;
                                    break;
                                }
                            }
                            else
                                if (!IsAssignable(p[j].ParameterType, parTypes[j]))
                                {
                                    b = false;
                                    break;
                                }
                        }
                        if (b)
                        {
                            a = funcs[i];
                            break;
                        }
                    }
                }
            //var a = parent.GetMethod(funcName, parTypes);

            if (a == null)
                throw new Exception("Type \"" + parent.ToString() + "\" does not contain method \"" + funcName + "\"");
            return a;
        }

        public static Type GetTypeReflection(String typeName)
        {
            Type t;
            lock (assemblies)
                for (int i = 0; i < assemblies.Count; i++)
                {
                    if ((t = assemblies[i].GetType(typeName)) != null)
                        return t;
                }
            return null;
        }

        public static FieldInfo GetField(String name, Type parent)
        {
            return parent.GetField(name);
        }

        public static PropertyInfo GetProperty(String name, Type parent)
        {
            return parent.GetProperty(name);
        }

        public static object GetValue(FieldInfo f, object parent)
        {
            return f.GetValue(parent);
        }

        public static object GetValue(PropertyInfo p, object parent, object[] indices = null)
        {
            return p.GetValue(parent, indices);
        }

        public static bool IsType(String path)
        {
            return GetType(path) != null;
        }



        public static bool IsAssignable(Type to, Type what)
        {
            return to == what || to.IsAssignableFrom(what) || IsImplicitlyAssignable(to, what) || IsBuiltInAssignable(to, what);
        }

        public static bool IsImplicitlyAssignable(Type to, Type what)
        {
            return to.GetMethod("op_Implicit", new[] { what }) != null;
        }

        public static object ConvertImplicit(Type to, Object what)
        {
            var a = to.GetMethod("op_Implicit", new[] { what.GetType() });
            if (a == null)
                return null;
            return a.Invoke(null, new object[] { what });
        }

        public static bool IsExplicitlyConvertable(Type to, Type what)
        {
            return to.GetMethod("op_Explicit", new[] { what }) != null;
        }

        public static object ConvertExplicit(Type to, Object what)
        {
            var a = to.GetMethod("op_Explicit", new[] { what.GetType() });
            if (a == null)
                return null;
            return a.Invoke(null, new object[] { what });
        }

        public static bool IsBuiltInAssignable(Type to, Type what)
        {
            //http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-334.pdf
            //13.1.2

            if (what == typeof(sbyte))
                return to == typeof(short) || to == typeof(int) || to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(byte))
                return to == typeof(short) || to == typeof(ushort) || to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) || 
                    to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(short))
                return to == typeof(int) || to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(ushort))
                return to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || 
                    to == typeof(decimal);

            if (what == typeof(int))
                return to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(uint))
                return to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(long))
                return to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(ulong))
                return to == typeof(float) || to == typeof(double) || to == typeof(decimal);

            if (what == typeof(char))
                return to == typeof(ushort) || to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) || to == typeof(float) || 
                    to == typeof(double) || to == typeof(decimal);

            if (what == typeof(float))
                return to == typeof(double);

            return false;
        }

        public static object ConvertBuiltIn(Type to, Object what)
        {
            if (to == typeof(sbyte))
                return Convert.ToSByte(what);

            if (to == typeof(byte))
                return Convert.ToByte(what);

            if (to == typeof(short))
                return Convert.ToInt16(what);

            if (to == typeof(ushort))
                return Convert.ToUInt16(what);

            if (to == typeof(int))
                return Convert.ToInt32(what);

            if (to == typeof(uint))
                return Convert.ToUInt32(what);

            if (to == typeof(long))
                return Convert.ToInt64(what);

            if (to == typeof(ulong))
                return Convert.ToUInt64(what);

            if (to == typeof(char))
                return Convert.ToChar(what);

            if (to == typeof(float))
                return Convert.ToSingle(what);

            if (to == typeof(double))
                return Convert.ToDouble(what);

            return null;
        }

        public static object DoConvert(Object what, Type toWhat)
        {
            Type whatT = what.GetType();

            if (toWhat.IsAssignableFrom(whatT))
                return what;
            try
            {
                var r = ConvertBuiltIn(toWhat, what);
                if (r != null)
                    return r;
            }
            catch { }
            if (IsImplicitlyAssignable(toWhat, whatT))
                return ConvertImplicit(toWhat, what);
            if (IsExplicitlyConvertable(toWhat, whatT))
                return ConvertExplicit(toWhat, what);

            if (what == null)
                throw new Exception("Cannot convert \"null\" to \"" + toWhat.ToString() + "\"");
            else
                throw new Exception("Cannot convert \"" + whatT.ToString() + "\" of type \"" + what.GetType() + "\" to \"" + toWhat.ToString() + "\"");
        }

        public static Type GetType(String s)
        {
            if (s == "")
                return null;

            if (s == "object" || s == "Object" || s == "null" || s == "dynamic")
                return typeof(object);
            if (s == "bool")
                return typeof(bool);
            if (s == "sbyte")
                return typeof(sbyte);
            if (s == "byte")
                return typeof(byte);
            if (s == "short")
                return typeof(short);
            if (s == "ushort")
                return typeof(ushort);
            if (s == "int")
                return typeof(int);
            if (s == "uint")
                return typeof(uint);
            if (s == "long")
                return typeof(long);
            if (s == "ulong")
                return typeof(ulong);
            if (s == "char")
                return typeof(char);
            if (s == "float")
                return typeof(float);
            if (s == "double")
                return typeof(double);
            if (s == "decimal")
                return typeof(decimal);
            if (s == "string" || s == "String")
                return typeof(string);

            CheckTypeArray(ref s, "object", "System.Object");
            CheckTypeArray(ref s, "Object", "System.Object");
            CheckTypeArray(ref s, "null", "System.Object");
            CheckTypeArray(ref s, "dynamic", "System.Object");
            CheckTypeArray(ref s, "bool", "System.Boolean");
            CheckTypeArray(ref s, "sbyte", "System.SByte");
            CheckTypeArray(ref s, "byte", "System.Byte");
            CheckTypeArray(ref s, "short", "System.Int16");
            CheckTypeArray(ref s, "ushort", "System.UInt16");
            CheckTypeArray(ref s, "int", "System.Int32");
            CheckTypeArray(ref s, "uint", "System.UInt32");
            CheckTypeArray(ref s, "long", "System.Int64");
            CheckTypeArray(ref s, "ulong", "System.UInt64");
            CheckTypeArray(ref s, "char", "System.Char");
            CheckTypeArray(ref s, "float", "System.Single");
            CheckTypeArray(ref s, "double", "System.Double");
            CheckTypeArray(ref s, "decimal", "System.Decimal");
            CheckTypeArray(ref s, "string", "System.String");
            CheckTypeArray(ref s, "String", "System.String");

            if (!ReformTypeBrackets(ref s))
                return null;

            return GetTypeReflection(s);
        }

        private static void CheckTypeArray(ref String s, string srcFormat, string dstFormat)
        {
            if (!s.StartsWith(srcFormat))
                return;
            for (int i = srcFormat.Length; i < s.Length; i++)
            {
                if (s[i] == ' ')
                    continue;
                if (s[i] == '[')
                    s = dstFormat + s.Substring(srcFormat.Length);
                break;
            }
        }

        private static bool ReformTypeBrackets(ref String s)
        {
            String t, tr;
            int ind = 0;
            int count = 0;
            char c;
            List<String> st = new List<string>();
            Type _t;
            while ((ind = s.IndexOf('<')) > 0)
            {
                if (!CommandParser.IsTypeBrackets(s, ind + 1, out t, false))
                    return false;//not type in there

                st.Clear();
                st.Add("");
                for (int i = 0; i < t.Length; i++)//count number of subtypes
                {
                    c = t[i];
                    if (c == '<')
                    {
                        count++;
                        st[st.Count - 1] += c;
                    }
                    else if (c == '>')
                    {
                        count--;
                        st[st.Count - 1] += c;
                    }
                    else if (c == ',')
                    {
                        if (count == 0)
                            st.Add("");
                        else
                            st[st.Count - 1] += c;
                    }
                    else
                        st[st.Count - 1] += c;
                }

                tr = "";
                for (int i = 0; i < st.Count; i++)
                {
                    _t = GetType(st[i].Trim());
                    if (_t == null)
                        return false;
                    if (i != 0)
                        tr += ",";
                    tr += "[" + _t.AssemblyQualifiedName + "]";
                }

                s = s.Substring(0, ind) + "`" + st.Count.ToString() + "[" + tr + "]" + s.Substring(ind + 1 + t.Length, s.Length - ind - 2 - t.Length);
            }
            return true;
        }
    }
}
