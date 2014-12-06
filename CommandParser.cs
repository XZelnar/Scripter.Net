using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet
{
    class CommandParser
    {
        public ScripterVM parent;

        public CallTree Parse(Command c)
        {
            if (c == null)
                return null;
            c.origText = c.text;
            CallTree tree = new CallTree();
            tree.posStart = c.posStart;
            tree.lineStart = c.posStart;
            if (c.text.Trim() == "")
                tree.Add(new NodeNOP());
            else
                processOperations(c, null, tree);
            tree.FindTrunk();
            return tree;
        }

        #region GlobalFunctions
        private NodeFunction processCall(String funcName, String parameters, Command command, CallTree tree, bool checkFirstSymbol = false)
        {
            char c;
            int parentheses = 0;
            //check parameters integrity
            for (int i = checkFirstSymbol ? 0 : 1; i < parameters.Length; i++)
            {
                c = parameters[i];
                if (c == '(')
                    parentheses++;
                else if (c == ')')
                {
                    parentheses--;
                    if (parentheses < 0)
                        throw new Exception(command.errorOutput() + "\";\" expected but \")\" found");
                }
            }
            if (parentheses > 0)
                throw new Exception(command.errorOutput() + "\")\" expected but \";\" found");
            if (parentheses < 0)
                throw new Exception(command.errorOutput() + "\";\" expected but \")\" found");



            NodeFunction r = new NodeFunction();
            int lastDot = funcName.LastIndexOf('.');
            if (lastDot == -1)//internal command???
            {
                r.isInternal = true;
                r.funcName = funcName;
                r.VM = parent;
            }
            else
            {
                String name = funcName.Substring(lastDot + 1);
                String objectName = funcName.Substring(0, lastDot);
                r.funcName = name;
                Type _ttype;
                if ((_ttype = ReflectionHelper.GetType(objectName)) != null)
                {
                    r.funcType = _ttype;
                }
                else
                {
                    if (isCorrectObjectName(objectName))
                        r.funcParent = getNodeFromIdentifyer(objectName, tree, command, true);
                    else
                        r.funcParent = processOperations(new Command(objectName, command.lineStart, command.posStart), r, tree);
                    r.funcParent.parent = r;
                }
            }
            //parse parameters
            parseParameters(parameters, command, tree, r);
            return r;
        }

        private void parseParameters(String s, Command command, CallTree tree, IParameters r)
        {
            s = s.Trim();
            int parentheses = 0, brackets = 0;
            String t = "";
            char c;
            bool node = false;
            bool emptyPars = false;
            int semicolonCount = 0;
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c == '(')
                {
                    parentheses++;
                    t += c;
                }
                else if (c == ';')
                {
                    if (i == 0)
                        node = true;
                    t += c;
                }
                else if (c == ')')
                {
                    parentheses--;
                    if (parentheses == -1)
                        break;
                    semicolonCount++;
                    t += c;
                }
                else if (c == '[')
                {
                    brackets++;
                    t += c;
                }
                else if (c == ']')
                {
                    brackets--;
                    t += c;
                }
                else if (c == ',' && parentheses == 0 && brackets == 0)
                {
                    if (emptyPars)
                    {
                        if (t.Trim() != "")
                            throw new Exception(command.errorOutput() + "Cannot mix empty and non-empty parameters");
                    }
                    else
                    {
                        emptyPars = t.Trim() == "";
                        if (emptyPars)
                        {
                            r.parameters.Add(null);
                        }
                    }
                    r.parameters.Add(node ? getNode(t, tree) : processOperations(new Command(t.Trim(), command.lineStart, command.posStart), r as CallTreeNode, tree));
                    node = false;
                    t = "";
                }
                else
                    t += c;
            }
            if (brackets != 0)
                throw new Exception(command.errorOutput() + "\"]\" expected but \")\" found");
            node = node && (semicolonCount == 2);
            if (t.Length > 0)
                r.parameters.Add(node ? getNode(t, tree) : processOperations(new Command(t.Trim(), command.lineStart, command.posStart), r as CallTreeNode, tree));
            for (int i = 0; i < r.parameters.Count; i++)
                if (r.parameters[i] != null)
                    r.parameters[i].parent = r as CallTreeNode;
        }
        #endregion

        #region OperationsParser
        private CallTreeNode processOperations(Command command, CallTreeNode parent, CallTree tree)
        {
            command.text = command.text.Trim();
            if (command.text == "")
                return null;

            int cur = tree.NodesCount;
            processTier0(ref command, tree, parent);

            Type declarationType = getDeclarationType(ref command);
            command.text = command.text.Trim();
            if (isCorrectVarName(command.text))
            {
                var a = getNodeFromIdentifyer(command.text, tree, command);
                if (declarationType != null)
                    if (a is NodeIdentifyerObject)
                        (a as NodeIdentifyerObject).VMDeclareType = declarationType;
                    else
                        throw new Exception(command.errorOutput() + "Identifyer expected but \"" + command.text + "\" found");
                return a;
            }

            processTier1(ref command, tree, parent);//functions, parentheses, new
            processTier2(ref command, tree, parent);
            processTier3(ref command, tree, parent);
            processTier4(ref command, tree, parent);
            processTier5(ref command, tree, parent);
            processTier6(ref command, tree, parent);
            processTier7(ref command, tree, parent);
            processTier8(ref command, tree, parent);
            processTier9(ref command, tree, parent);
            processTier10(ref command, tree, parent);
            processTier11(ref command, tree, parent);
            processTier12(ref command, tree, parent);
            processTier13(ref command, tree, parent);
            processTier14(ref command, tree, parent);
            processTier15(ref command, tree, parent);
            processTier16(ref command, tree, parent);
            List<NodeAssignment> vars = processTier17(ref command, tree, parent);

            if (declarationType != null)
            {
                splitDeclarations(ref command.text, tree);
                processVMDeclaration(vars, declarationType);
            }

            if (!isParsedCompletely(ref command, declarationType))
                return null;

            if (tree.NodesCount == cur)
                return null;
            else
            {
                CallTreeNode n = tree.GetNode(cur);
                while (n.parent != null && n.parent != parent)
                    n = n.parent;
                return n;
            }
        }

        #region DeclarationTools
        private void splitDeclarations(ref String command, CallTree tree)
        {
            var a = command.Split(',');
            if (a.Length == 1)
                return;
            else
            {
                NodeMultipleDeclarations n = new NodeMultipleDeclarations();
                for (int i = 0; i < a.Length; i++)
                {
                    n.children.Add(getNode(a[i].Trim(), tree));
                    n.children[i].parent = n;
                }
                n.id = tree.NodesCount;
                tree.Add(n);
                command = ";" + n.id.ToString() + ";";
            }
        }

        private bool isParsedCompletely(ref Command command, Type decType)
        {
            command.text = command.text.Trim();
            int t;
            String em = decType == null ? "expression" : "identifyer";
            if (command.text.Length == 0)
                throw new Exception(command.errorOutput() + "Invalid expression: \"" + command.origText + "\"");
            if (command.text[0] != ';')//smth b4 1st command
            {
                if ((t = command.text.IndexOf(';')) == 0)
                    throw new Exception(command.errorOutput() + "Unknown " + em + ": \"" + command + "\"");
                else
                    throw new Exception(command.errorOutput() + "Unknown " + em + ": \"" + (t == -1 ? command.text : command.text.Substring(0, t).Trim()) + "\"");
            }
            t = command.text.IndexOf(';', 1);
            if (command.text.Length > t + 1)//smth aftes 1st command
                if (command.text.IndexOf(';', t + 1) > -1)
                    throw new Exception(command.errorOutput() + "Missing operator between expressions");//TODO location
                else
                    throw new Exception(command.errorOutput() + "Unknown " + em + ": \"" + (t == -1 ? command.text : command.text.Substring(t + 1).Trim()) + "\"");
            return true;
        }

        private void processVMDeclaration(List<NodeAssignment> assignments, Type decType)
        {
            for (int i = 0; i < assignments.Count; i++)
                if (assignments[i].child1 is NodeIdentifyerObject)
                    (assignments[i].child1 as NodeIdentifyerObject).VMDeclareType = decType;
        }

        private Type getDeclarationType(ref Command s)
        {
            String t = getNextWord(s.text, 0, true, true, true);
            if (t.Trim() == "var")
            {
                s.text = s.Substring(t.Length);
                return typeof(VariablesEngine.Var);
            }
            if (t.Trim() == "dynamic")
            {
                s.text = s.Substring(t.Length);
                return typeof(VariablesEngine.Dynamic);
            }
            int ind = t.IndexOf('[');
            Type type;
            if (!(ind != -1 && t.Trim().EndsWith("]")))
            {
                type = ReflectionHelper.GetType(t);
                if (type != null)
                    s.text = s.Substring(t.Length);
                return type;
            }
            String _t = t.Substring(0, ind);
            type = ReflectionHelper.GetType(_t);
            if (type == null)
            {
                return null;
            }
            s.text = s.Substring(t.Length);
            bool b = false;
            char c;
            List<int> dims = new List<int>();
            int curdim = 1;
            for (int i = ind; i < t.Length; i++)
            {
                c = t[i];
                if (c == '[')
                    if (!b)
                        b = true;
                    else
                        throw new Exception(s.errorOutput() + "Unexpected \"[\" inside another \"[\" in type");
                else if (c == ',')
                    if (b)
                        curdim++;
                    else
                        throw new Exception(s.errorOutput() + "Unexpected \",\" in type");
                else if (c == ']')
                    if (b)
                    {
                        b = false;
                        dims.Add(curdim);
                        curdim = 1;
                    }
                    else
                        throw new Exception(s.errorOutput() + "Unexpected \"]\" in type");
                else if (c != ' ')
                    throw new Exception(s.errorOutput() + "Unexpected symbol in type: " + c);
            }

            for (int i = dims.Count - 1; i >= 0; i--)
                type = Array.CreateInstance(type, new int[dims[i]]).GetType();

            return type;
        }
        #endregion

        #region OperatorsProcessors
        private void processTier0(ref Command s, CallTree tree, CallTreeNode parent = null)//constants
        {
            int ind, ind2;
            #region strings
            while ((ind = s.IndexOf('\"')) > 0)//Strings
            {
                ind2 = -1;
                char c =  '\"', cprev;
                String r = "";
                for (int i = ind + 1; i < s.Length; i++)
                {
                    cprev = c;
                    c = s[i];
                    if (c == '"' && cprev != '\\')
                    {
                        ind2 = i - 1;
                        break;
                    }
                    r += c;
                }
                //ind2 = s.IndexOf('\"', ind + 1);
                if (ind2 == -1)//didn't find the end
                    throw new Exception(s.errorOutput() + "\"\"\" expected but \";\" found");
                NodeIdentifyerString rs = new NodeIdentifyerString();
                rs.id = tree.NodesCount;
                rs.parent = parent;
                rs.identifyer = r;
                ProcessLiterals(ref rs.identifyer);
                tree.Add(rs);

                s.text = s.Substring(0, ind) + ";" + rs.id.ToString() + ";" + s.Substring(ind2 + 2);
            }
            #endregion

            #region chars
            while ((ind = s.IndexOf('\'')) > 0)//Chars
            {
                ind2 = -1;
                char c = '\'', cprev;
                String r = "";
                for (int i = ind + 1; i < s.Length; i++)
                {
                    cprev = c;
                    c = s[i];
                    if (c == '\'' && cprev != '\\')
                    {
                        ind2 = i - 1;
                        break;
                    }
                    r += c;
                }
                //ind2 = s.IndexOf('\'', ind + 1);
                if (ind2 == -1)//didn't find the end
                    throw new Exception(s.errorOutput() + "\"\'\" expected but \";\" found");
                ProcessLiterals(ref r);
                if (r.Length > 1)
                    throw new Exception(s.errorOutput() + "Too many characters in character literal");
                NodeIdentifyerChar rc = new NodeIdentifyerChar();
                rc.id = tree.NodesCount;
                rc.parent = parent;
                rc.identifyer = r[0];
                tree.Add(rc);

                s.text = s.Substring(0, ind) + ";" + rc.id.ToString() + ";" + s.Substring(ind2 + 2);
            }
            #endregion

            #region bools
            while ((ind = indexOfWord(s.text, "true")) > -1)//Trues
            {
                NodeIdentifyerBool rb = new NodeIdentifyerBool();
                rb.identifyer = true;
                rb.id = tree.NodesCount;
                tree.Add(rb);
                s.text = s.Substring(0, ind) + ";" + rb.id.ToString() + ";" + s.Substring(ind + 4);
            }

            while ((ind = indexOfWord(s.text, "false")) > -1)//Falses
            {
                NodeIdentifyerBool rb = new NodeIdentifyerBool();
                rb.identifyer = false;
                rb.id = tree.NodesCount;
                tree.Add(rb);
                s.text = s.Substring(0, ind) + ";" + rb.id.ToString() + ";" + s.Substring(ind + 5);
            }
            #endregion

            #region null
            while ((ind = indexOfWord(s.text, "null")) > -1)//Nulls
            {
                NodeIdentifyerNull rb = new NodeIdentifyerNull();
                rb.id = tree.NodesCount;
                tree.Add(rb);
                s.text = s.Substring(0, ind) + ";" + rb.id.ToString() + ";" + s.Substring(ind + 4);
            }
            #endregion

            #region numbers
            String w, ww;
            ind = 0;
            //long i = 0;
            dynamic t;
            double d;
            bool f = false;
            bool dd = false;
            bool h = false;
            bool bin = false;
            bool L = false;
            while ((ww = w = getNextWord(s.text, ind)) != "")//ints and doubles
            {
                f = w.Trim().EndsWith("f");
                dd = w.Trim().EndsWith("d");
                L = w.Trim().EndsWith("L");
                h = w.Trim().StartsWith("0x");
                bin = w.Trim().StartsWith("0b");
                if ((h || bin) && (f || dd))
                    throw new Exception(s.errorOutput() + "Invalid number format: " + ww);
                if (f || dd || L)
                    ww = ww.Trim().Substring(0, ww.Length - 1);
                if (h || bin)
                    ww = ww.Trim().Substring(2);

                try
                {
                    if (ConstantsParser.TryParseAnyInt(ww.Trim(), bin, h, L, out t))
                    {
                        ConstantsParser.TryParseAnyInt(ww.Trim(), bin, h, L, out t);
                        NodeIdentifyer rb;
                        if (L)
                        {
                            rb = new NodeIdentifyerInt64();
                            (rb as NodeIdentifyerInt64).identifyer = (long)t;
                        }
                        else
                        {
                            rb = new NodeIdentifyerInt32();
                            (rb as NodeIdentifyerInt32).identifyer = (int)t;
                        }
                        rb.id = tree.NodesCount;
                        tree.Add(rb);
                        s.text = s.Substring(0, ind) + ";" + rb.id.ToString() + ";" + s.Substring(ind + w.Length);
                        ind += 2 + rb.id.ToString().Length;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(s.errorOutput() + e.Message, e);
                }
                /*
                if (bin || h || long.TryParse(ww, out i))
                {
                    try
                    {
                        if (bin)
                            i = Convert.ToInt64(ww, 2);
                        if (h)
                            i = Convert.ToInt64(ww, 16);
                    }
                    catch
                    {
                        throw new Exception(s.errorOutput() + "Invalid number: " + ww);
                    }
                    NodeIdentifyer rb;
                    if (L)
                    {
                        rb = new NodeIdentifyerInt64();
                        (rb as NodeIdentifyerInt64).identifyer = i;
                    }
                    else
                    {
                        rb = new NodeIdentifyerInt32();
                        (rb as NodeIdentifyerInt32).identifyer = (int)i;
                    }
                    rb.id = tree.NodesCount;
                    tree.Add(rb);
                    s.text = s.Substring(0, ind) + ";" + rb.id.ToString() + ";" + s.Substring(ind + w.Length);
                    ind += 2 + rb.id.ToString().Length;
                    continue;
                }//*/
                if ((f || dd || ww.IndexOf('.') > -1) && double.TryParse(ww, out d))
                {
                    NodeIdentifyer rb;
                    if (f)
                    {
                        rb = new NodeIdentifyerFloat();
                        (rb as NodeIdentifyerFloat).identifyer = (float)d;
                    }
                    else
                    {
                        rb = new NodeIdentifyerDouble();
                        (rb as NodeIdentifyerDouble).identifyer = d;
                    }
                    rb.id = tree.NodesCount;
                    tree.Add(rb);
                    s.text = s.Substring(0, ind) + ";" + rb.id.ToString() + ";" + s.Substring(ind + w.Length);
                    ind += 2 + rb.id.ToString().Length;
                    continue;
                }
                if ((ww[0] >= '0' && ww[0] <= '9') || h || bin)
                    throw new Exception("Invalid number format: \"" + ww + "\"");
                ind += w.Length;
            }
            #endregion
        }

        private void processTier1(ref Command s, CallTree tree, CallTreeNode parent = null)//funcions, parentheses, new, a.x, typeof
        {
            CallTreeNode r = null;
            int ind, prevInd = 0;

            #region typeof
            while ((ind = indexOfWord(s.text, "typeof")) > -1)
            {
                if (getNextNonSpaceCharacter(s.text, ind + 6) != '(')
                    throw new Exception(s.errorOutput() + "\"(\" expected after \"typeof\" operator");
                prevInd = s.IndexOf('(', ind + 6);
                String tt;
                try
                {
                    tt = getParentheses(s.text, prevInd);
                }
                catch (Exception e)
                {
                    throw new Exception(s.errorOutput() + e.Message);
                }
                Type type = ReflectionHelper.GetType(tt.Trim());
                if (type == null)
                    throw new Exception(s.errorOutput() + "Type expected but \"" + tt.Trim() + "\" found");
                r = new NodeTypeOf() { type = type, parent = parent, id = tree.NodesCount };
                tree.Add(r);
                s.text = s.Substring(0, ind) + ";" + r.id.ToString() + ";" + s.Substring(prevInd + 2 + tt.Length);
            }
            #endregion

            #region new
            while ((ind = indexOfWord(s.text, "new ")) > -1)
            {
                String t = getNextWord(s.text, ind + 4, false, true);
                if (getNextNonSpaceCharacter(s.text, ind + 4 + t.Length) == '<' && !t.Contains('<'))
                {
                    String tpr;
                    IsTypeBrackets(s.text, ind + 4 + t.Length + 1, out tpr, false);
                    throw new Exception(s.errorOutput() + "Valid type expected but \"" + t.Trim() + "<" + tpr + ">" +"\" found");
                }
                if (t.Trim() == "")
                    throw new Exception(s.errorOutput() + "\"new\" requires a type");
                Type type = ReflectionHelper.GetType(t.Trim());
                if (type == null)
                    throw new Exception(s.errorOutput() + "Valid type expected but \"" + t.Trim() + "\" found");
                char c;
                if ((c = getNextNonSpaceCharacter(s.text, ind + 4 + t.Length)) != '(' && c != '[')
                    throw new Exception(s.errorOutput() + "\"(\" or \"[\" expected");
                int end;
                String par;
                if (c == '(')
                {
                    r = new NodeConstructor() { parent = parent, type = type, id = tree.NodesCount };
                    prevInd = s.IndexOf('(', ind + 4 + t.Length);
                    try
                    {
                        par = getParentheses(s.text, prevInd);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(s.errorOutput() + e.Message);
                    }
                    parseParameters(par.Trim(), s, tree, r as NodeConstructor);
                    end = prevInd + par.Length + 2;
                }
                else//c == '['
                {
                    r = new NodeArrayConstructor() { parent = parent, type = type, id = tree.NodesCount };
                    int curdim = 1, prevpar = 0;
                    bool emptyDim = false;
                    end = ind + t.Length + 4;
                    while (getNextNonSpaceCharacter(s.text, end) == '[')
                    {
                        prevInd = s.IndexOf('[', end);
                        try
                        {
                            par = getBrackets(s.text, prevInd);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(s.errorOutput() + e.Message);
                        }
                        prevpar = (r as NodeConstructor).parameters.Count;
                        parseParameters(par.Trim(), s, tree, r as NodeConstructor);
                        if (prevpar == (r as NodeConstructor).parameters.Count)
                            (r as NodeConstructor).parameters.Add(null);
                        curdim = (r as NodeConstructor).parameters.Count - prevpar;

                        if (curdim == 0)
                            emptyDim = true;
                        else
                        {
                            //bool ie = true;
                            emptyDim = true;
                            for (int i = (r as NodeArrayConstructor).parameters.Count - 1; i < (r as NodeArrayConstructor).parameters.Count; i++)
                                if ((r as NodeArrayConstructor).parameters[i] != null)
                                {
                                    emptyDim = false;
                                    break;
                                }
                            if (prevpar != 0 && !emptyDim)
                                throw new Exception(s.errorOutput() + "Canont recursively declare arrays");
                        }

                        (r as NodeArrayConstructor).dimentions.Add(curdim);
                        end = prevInd + par.Length + 2;
                    }
                    if (getNextNonSpaceCharacter(s.text, end) == '{')
                        throw new Exception(s.errorOutput() + "Cannot initialize array upon creation");
                }
                r.id = tree.NodesCount;
                tree.nodes.Add(r);
                s.text = s.Substring(0, ind) + ";" + r.id.ToString() + ";" + s.Substring(end);
            }
            #endregion

            #region (
            prevInd = 0;
            while ((ind = s.IndexOf('(', prevInd)) > -1)
            {
                r = null;
                String p;
                try
                {
                    p = getParentheses(s.text, ind);
                }
                catch (Exception e)
                {
                    throw new Exception(s.errorOutput() + e.Message);
                }
                String t = getWordArrayBefore(s.text, ind);
                if (t.Trim() != "" && isCorrectObjectOrArrayName(t.Trim()))//func call
                {
                    r = processCall(t.Trim(), p.Trim(), s, tree, true);
                    s.text = s.Substring(0, ind - t.Length) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + 2 + p.Length);
                }
                else if (ReflectionHelper.IsType(p))
                {
                    prevInd = ind + 1;
                    continue;
                }
                else//parentheses operations
                {
                    r = processOperations(new Command(p.Trim(), s.lineStart, s.posStart), null, tree);
                    s.text = s.Substring(0, ind) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + 2 + p.Length);
                }

                r.id = tree.NodesCount;
                r.parent = parent;
                tree.Add(r);
            }
            #endregion

            #region .
            prevInd = 0;
            while ((ind = s.IndexOf('.', prevInd)) > -1)
            {
                String p = getWordBefore(s.text, ind);
                if (p.Trim() == "" || !isCorrectObjectName(p.Trim()))
                    if (ind > 0 && s[ind - 1] == ']')//digging into array
                    {
                        prevInd = ind + 1;
                        int ld = s.LastIndexOf('[', ind);
                        if (ld == -1)
                            throw new Exception(s.errorOutput() + "Unexpected \"]\"");
                        String l = getWordBefore(s.text, ld);
                        if (l.Trim() == "")
                            throw new Exception(s.errorOutput() + "\"[\" is only applicable to objects");

                        String n2 = getNextWord(s.text, ind + 1);
                        if (n2 == "" || n2[0] == ';' || !isCorrectVarName(n2.Trim()))
                            throw new Exception(s.errorOutput() + "Unexpected \".\"");

                        r = new NodeChainedIdentifyerObject();
                        (r as NodeChainedIdentifyerObject).parentNode = processOperations(
                            new Command(s.Substring(ld - l.Length, l.Length + (ind - ld)), s.lineStart, s.posStart), r, tree);
                        (r as NodeChainedIdentifyerObject).parentNode.parent = r;
                        (r as NodeChainedIdentifyerObject).identifyer = n2.Trim();
                        (r as NodeIdentifyerObject).id = tree.NodesCount;
                        r.parent = parent;
                        tree.Add(r);

                        s.text = s.Substring(0, ld - l.Length) + ";" + r.id.ToString() + ";" + s.Substring(ind + 1 + n2.Length);
                        prevInd = ld - l.Length + 2 + r.id.ToString().Length;

                        continue;
                    }
                String n = getNextWord(s.text, ind + 1);
                int postLength = n.Length + 1;
                n = n.Trim();

                if (n == "" || n[0] == ';' || !isCorrectObjectName(n))
                    if (ind + 2 + n.Length < s.Length && getNextNonSpaceCharacter(s.text, ind + 1 + n.Length) == '[')
                    {
                        prevInd = ind + 1;
                        continue;
                    }
                    else
                        throw new Exception(s.errorOutput() + "Unexpected \".\"");

                String res = n;
                int tl;
                while (true)
                {
                    if (ind + postLength + 1 >= s.Length || s[ind + postLength] != '.' || s[ind + postLength] != ';')
                        break;
                    n = getNextWord(s.text, ind + postLength + 1);
                    tl = n.Length + 1;
                    n = n.Trim();
                    if (n == "" || !isCorrectVarName(n))
                        break;
                    postLength += tl;
                    res += "." + n;
                }

                if (p[0] == ';')
                {
                    r = new NodeChainedIdentifyerObject();
                    (r as NodeChainedIdentifyerObject).parentNode = getNode(p, tree);
                    (r as NodeChainedIdentifyerObject).parentNode.parent = r;
                    (r as NodeChainedIdentifyerObject).identifyer = res;
                }
                else
                {
                    r = new NodeIdentifyerObject();
                    (r as NodeIdentifyerObject).identifyer = p.Trim() + "." + res;
                    (r as NodeIdentifyerObject).vars = this.parent._variables;
                }
                (r as NodeIdentifyerObject).id = tree.NodesCount;
                r.parent = parent;
                tree.Add(r);

                s.text = s.Substring(0, ind - p.Length) + ";" + r.id.ToString() + ";" + s.Substring(ind + postLength);
                prevInd = ind - p.Length + 2 + r.id.ToString().Length;
            }
            #endregion
        }

        private void processTier2(ref Command s, CallTree tree, CallTreeNode parent = null)//[]
        {
            int ind, i, prevInd = 0;
            int brackets = 1;
            char c;
            while ((ind = s.IndexOf('[', prevInd)) > -1)
            {
                String wordLeft = getWordBefore(s.text, ind);
                if (!isCorrectTypeName(wordLeft.Trim()))
                    throw new Exception(s.errorOutput() + "Invalid object name for given operation (\"[\")");
                if (ReflectionHelper.IsType(wordLeft.Trim()))
                {
                    prevInd = ind + 1;
                    continue;
                }

                brackets = 1;
                for (i = ind + 1; i < s.Length; i++)
			    {
                    c = s[i];
                    if (c == '[')
                        brackets++;
                    else if (c == ']')
                    {
                        brackets--;
                        if (brackets == 0)
                            break;
                    }
                }

                if (brackets != 0)
                    throw new Exception(s.errorOutput() + "\"[\" expected but \";\" found");
                if (i == ind + 1)
                    throw new Exception(s.errorOutput() + "Value expected but \"]\" found");

                NodeArrayIndexer r = new NodeArrayIndexer();
                r.id = tree.NodesCount;
                tree.Add(r);
                r.child1 = getNodeFromIdentifyer(wordLeft.Trim(), tree, s) as NodeIdentifyer;

                //String par = s.Substring(ind + 1, i - ind - 1).Trim();
                String par;
                try
                {
                    par = getBrackets(s.text, ind);
                }
                catch (Exception e)
                {
                    throw new Exception(s.errorOutput() + e.Message);
                }
                //if (par[0] == ';' || isCorrectObjectName(par))
                //    r.child2 = getNodeFromIdentifyer(par);
                //else
                //    r.child2 = processOperations(par, r);
                if (par.Trim() != "")
                    parseParameters(par, s, tree, r);

                s.text = s.Substring(0, ind - wordLeft.Length) + ";" + r.id.ToString() + ";" + s.Substring(i + 1);

                //r.child2.parent = r;
                r.child1.parent = r;
                r.parent = parent;
            }
        }

        private void processTier3(ref Command s, CallTree tree, CallTreeNode parent = null)//explicit conversion
        {
            NodeExplicitConversion r;
            int ind;
            while ((ind = s.IndexOf('(')) > -1)
            {
                String p;
                try
                {
                    p = getParentheses(s.text, ind);
                }
                catch (Exception e)
                {
                    throw new Exception(s.errorOutput() + e.Message);
                }
                Type t;
                if (p.StartsWith(";"))
                    t = ReflectionHelper.GetType((getNode(p, tree) as NodeIdentifyerObject).identifyer);
                else
                    t = ReflectionHelper.GetType(p.Trim());
                if (t == null)
                    throw new Exception(s.errorOutput() + "Type expected but \"" + p + "\" found");
                String n = getNextWord(s.text, ind + 2 + p.Length);
                if (!isCorrectObjectName(n.Trim()))
                    throw new Exception(s.errorOutput() + "Explicit conversion is only applicable to objects");
                r = new NodeExplicitConversion();
                r.parent = parent;
                if (p.StartsWith(";"))
                    getNode(p, tree).parent = r;
                r.id = tree.NodesCount;
                r.toType = t;
                r.child1 = getNodeFromIdentifyer(n.Trim(), tree, s);
                r.child1.parent = r;
                tree.Add(r);
                s.text = s.Substring(0, ind) + ";" + r.id.ToString() + ";" + s.Substring(ind + 2 + p.Length + n.Length);
            }
        }

        private void processTier4(ref Command s, CallTree tree, CallTreeNode parent = null)//++, --, +a, -a
        {
            CallTreeNode r;
            int ind;
            bool? pre = null;
            String word = "", t;

            #region Inc
            while ((ind = s.IndexOf("++")) > -1)
            {
                r = null;
                word = "";
                pre = null;
                if (ind > 0)
                    if (isCorrectTypeName((t = getWordBefore(s.text, ind)).Trim()))
                    {
                        pre = false;
                        word = t;
                    }
                if (pre == null)
                {
                    if (isCorrectTypeName((t = getNextWord(s.text, ind + 2)).Trim()))
                    {
                        pre = true;
                        word = t;
                    }
                }

                if (pre != null)
                {
                    r = new NodeIncrement() { pre = pre.Value, child1 = getNodeFromIdentifyer(word.Trim(), tree, s) };
                    if (pre.Value)
                        s.text = s.Substring(0, ind) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + 2 + word.Length);
                    else
                    {
                        s.text = s.Substring(0, ind - word.Length) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + 2);
                        tree.AddPostNode(r as NodeIncrement);
                    }

                    r.id = tree.NodesCount;
                    (r as NodeIncrement).child1.parent = r;
                    r.parent = parent;
                    tree.Add(r);
                }
                else
                    throw new Exception(s.errorOutput() + "Increment can only be called for variables");
            }
            #endregion

            #region Dec
            while ((ind = s.IndexOf("--")) > -1)
            {
                r = null;
                word = "";
                pre = null;
                if (ind > 0)
                    if (isCorrectTypeName((t = getWordBefore(s.text, ind)).Trim()))
                    {
                        pre = false;
                        word = t;
                    }
                if (pre == null)
                {
                    if (isCorrectTypeName((t = getNextWord(s.text, ind + 2)).Trim()))
                    {
                        pre = true;
                        word = t;
                    }
                }

                if (pre != null)
                {
                    r = new NodeDecrement() { pre = pre.Value, child1 = getNodeFromIdentifyer(word.Trim(), tree, s) };
                    if (pre.Value)
                        s.text = s.Substring(0, ind) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + 2 + word.Length);
                    else
                    {
                        s.text = s.Substring(0, ind - word.Length) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + 2);
                        tree.AddPostNode(r as NodeDecrement);
                    }

                    r.id = tree.NodesCount;
                    (r as NodeDecrement).child1.parent = r;
                    r.parent = parent;
                    tree.Add(r);
                }
                else
                    throw new Exception(s.errorOutput() + "Decrement can only be called for variables");
            }
            #endregion

            #region Positive / Negative
            int prev = 0;
            String pw;
            while ((ind = s.IndexOfAny(new char[] { '+', '-' }, prev)) > -1)
            {
                pw = getWordBefore(s.text, ind).Trim();
                if (isCorrectObjectName(pw))
                {
                    prev = ind + 1;
                    continue;
                }

                pw = getNextWord(s.text, ind + 1);
                if (!isCorrectObjectName(pw.Trim()))
                    throw new Exception(s.errorOutput() + "Invalid syntacsis near \"+\": \"" + pw.Trim() + "\" is not a correct object name");

                r = s[ind] == '+' ? (UnaryNode)new NodeUnaryPlus() : (UnaryNode)new NodeUnaryMinus();
                r.id = tree.NodesCount;
                (r as UnaryNode).child1 = getNodeFromIdentifyer(pw.Trim(), tree, s);
                (r as UnaryNode).child1.parent = r;
                r.parent = parent;
                tree.Add(r);

                s.text = s.Substring(0, ind) + ";" + r.id.ToString() + ";" + s.Substring(ind + 1 + pw.Length);
            }
            #endregion
        }

        private void processTier5(ref Command s, CallTree tree, CallTreeNode parent = null)//!, ~
        {
            int ind, prevInd = 0;
            char c;
            while ((ind = s.IndexOfAny(new char[] { '!', '~' }, prevInd)) > -1)
            {
                c = s[ind];
                //if (ind == 0)
                //    throw new Exception(s.errorOutput() + "Cannot start expression with \"" + s[0] + "\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"" + c + "\"");
                if (s[ind + 1] == '=')
                {
                    prevInd = ind + 2;
                    continue;
                }

                UnaryNode r;
                if (c == '!')
                    r = new NodeNOT();
                else
                    r = new NodeBitwiseNOT();
                r.id = tree.NodesCount;
                tree.Add(r);

                String wordRight = getNextWord(s.text, ind + 1);
                if (isCorrectObjectName(wordRight.Trim()))
                    r.child1 = getNodeFromIdentifyer(wordRight, tree, s);
                else
                    if (wordRight.Trim()[0] == ';')
                        r.child1 = getNode(wordRight.Trim(), tree);
                    else
                        throw new Exception(s.errorOutput() + "Expression expected but \"" + wordRight + "\" found");

                s.text = s.Substring(0, ind) + ";" + r.id.ToString() + ";" + s.Substring(ind + 1 + wordRight.Length);

                r.child1.parent = r;
                r.parent = parent;
            }
        }

        private void processTier6(ref Command s, CallTree tree, CallTreeNode parent = null)//*, /, %
        {
            int ind = -1, prevInd = 0;
            BinaryNode r;
            char c, c2;
            while ((ind = s.IndexOfAny(new char[] { '*', '/', '%' }, prevInd)) > -1)
            {
                c = s[ind];
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"" + c + "\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"" + c + "\"");
                c2 = s[ind + 1];
                if (c2 == '=')//ignore if part of a lower-priority operation
                {
                    ind += 2;
                    prevInd = ind;
                    continue;
                }

                if (c == '*')
                    r = new NodeMultiplication();
                else if (c == '/')
                    r = new NodeDivision();
                else
                    r = new NodeRemainder();

                processBinaryOperation(ref s, c.ToString(), ind, r, parent, tree);
            }
        }

        private void processTier7(ref Command s, CallTree tree, CallTreeNode parent = null)//+, -
        {
            int ind = -1, prevInd = 0;
            BinaryNode r;
            char c, c2;
            while ((ind = s.IndexOfAny(new char[] { '+', '-' }, prevInd)) > -1)
            {
                c = s[ind];
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"" + c + "\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"" + c + "\"");
                c2 = s[ind + 1];
                if (c2 == '=')//ignore if part of a lower-priority operation
                {
                    ind += 2;
                    prevInd = ind;
                    continue;
                }

                if (c == '+')
                    r = new NodeAddition();
                else
                    r = new NodeSubtraction();

                processBinaryOperation(ref s, c.ToString(), ind, r, parent, tree);
            }
        }

        private void processTier8(ref Command s, CallTree tree, CallTreeNode parent = null)//<<, >>
        {
            int ind, ind1 = -1, ind2 = -1, prevInd = 0;
            BinaryNode r;
            char c;
            while ((ind1 = s.IndexOf("<<", prevInd)) > -1 || (ind2 = s.IndexOf(">>", prevInd)) > -1)
            {
                if (ind1 == -1)
                    ind = ind2;
                else if (ind2 == -1)
                    ind = ind1;
                else
                    ind = Math.Min(ind1, ind2);
                c = s[ind];
                ind1 = ind2 = -1;
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"" + c.ToString() + c.ToString() + "\"");
                if (ind == s.Length - 2)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"" + c.ToString() + c.ToString() + "\"");
                if (s[ind + 2] == '=')//part of lower-tier operation
                {
                    prevInd = ind + 2;
                    continue;
                }

                if (c == '<')
                    r = new NodeLeftShift();
                else
                    r = new NodeRightShift();

                processBinaryOperation(ref s, c.ToString() + c.ToString(), ind, r, parent, tree);
            }
        }

        private void processTier9(ref Command s, CallTree tree, CallTreeNode parent = null)//<, >, <=, >=, is, as
        {
            int ind, prev = 0;
            BinaryNode r;
            char c, c2;

            while((ind = indexOfWord(s.text, "is")) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"is\"");
                if (ind == s.Length - 3)
                    throw new Exception(s.errorOutput() + "Type expected but \";\" found");
                r = new NodeIs();
                processBinaryOperation(ref s, "is", ind, r, parent, tree);
            }

            while ((ind = indexOfWord(s.text, "as")) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"is\"");
                if (ind == s.Length - 3)
                    throw new Exception(s.errorOutput() + "Type expected but \";\" found");
                r = new NodeAs();
                processBinaryOperation(ref s, "as", ind, r, parent, tree);
            }

            while ((ind = s.IndexOfAny(new char[] { '<', '>' }, prev)) > -1)
            {
                c = s[ind];
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Unexpected end of command");
                c2 = s[ind + 1];
                if (c2 == '>' || c2 == '<')
                {
                    prev = ind + 2;
                    continue;
                }
                else if (c2 != '=')
                    c2 = ' ';
                String op = (c.ToString() + c2.ToString()).Trim();
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"" + op + "\"");

                if (op == "<")
                    r = new NodeIsLess();
                else if (op == ">")
                    r = new NodeIsMore();
                else if (op == "<=")
                    r = new NodeIsLessOrEqual();
                else
                    r = new NodeIsMoreOrEqual();

                processBinaryOperation(ref s, op, ind, r, parent, tree);
            }
        }

        private void processTier10(ref Command s, CallTree tree, CallTreeNode parent = null)//==, !=
        {
            int ind, ind1 = -1, ind2 = -1;
            BinaryNode r;
            char c;
            while ((ind1 = s.IndexOf("==")) > -1 || (ind2 = s.IndexOf("!=")) > -1)
            {
                ind = -1;
                if (ind1 != -1)
                    ind = ind1;
                if (ind2 != -1)
                    if (ind == -1 || ind2 < ind)
                        ind = ind2;
                c = s[ind];
                ind1 = ind2 = -1;
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"" + c + "=\"");

                if (c == '=')
                    r = new NodeIsEqual();
                else
                    r = new NodeIsNotEqual();

                processBinaryOperation(ref s, c.ToString() + "=", ind, r, parent, tree);
            }
        }

        private void processTier11(ref Command s, CallTree tree, CallTreeNode parent = null)//&
        {
            int ind = -1, prevInd = 0;
            BinaryNode r;
            char c;
            while ((ind = s.IndexOf('&', prevInd)) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"&\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"&\"");
                c = s[ind + 1];
                if (c == '=' || c == '&')//ignore if part of a lower-priority operation
                {
                    ind += 2;
                    prevInd = ind;
                    continue;
                }

                r = new NodeLogicalAND();

                processBinaryOperation(ref s, "&", ind, r, parent, tree);
            }
        }

        private void processTier12(ref Command s, CallTree tree, CallTreeNode parent = null)//^
        {
            int ind = -1, prevInd = 0;
            BinaryNode r;
            char c;
            while ((ind = s.IndexOf('^', prevInd)) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"^\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"^\"");
                c = s[ind + 1];
                if (c == '=')//ignore if part of a lower-priority operation
                {
                    ind += 2;
                    prevInd = ind;
                    continue;
                }

                r = new NodeLogicalXOR();

                processBinaryOperation(ref s, "^", ind, r, parent, tree);
            }
        }

        private void processTier13(ref Command s, CallTree tree, CallTreeNode parent = null)//|
        {
            int ind = -1, prevInd = 0;
            BinaryNode r;
            char c;
            while ((ind = s.IndexOf('|', prevInd)) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"|\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"|\"");
                c = s[ind + 1];
                if (c == '=' || c == '|')//ignore if part of a lower-priority operation
                {
                    ind += 2;
                    prevInd = ind;
                    continue;
                }

                r = new NodeLogicalOR();

                processBinaryOperation(ref s, "|", ind, r, parent, tree);
            }
        }

        private void processTier14(ref Command s, CallTree tree, CallTreeNode parent = null)//&&
        {
            int ind = -1;
            BinaryNode r;
            char c;
            while ((ind = s.IndexOf("&&")) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"&&\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"&&\"");
                c = s[ind + 1];

                r = new NodeConditionalAND();

                processBinaryOperation(ref s, "&&", ind, r, parent, tree);
            }
        }

        private void processTier15(ref Command s, CallTree tree, CallTreeNode parent = null)//||
        {
            int ind = -1;
            BinaryNode r;
            char c;
            while ((ind = s.IndexOf("||")) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"||\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"||\"");
                c = s[ind + 1];

                r = new NodeConditionalOR();

                processBinaryOperation(ref s, "||", ind, r, parent, tree);
            }
        }

        private void processTier16(ref Command s, CallTree tree, CallTreeNode parent = null)//?, :
        {
            int ind = -1;
            NodeConditional r;
            while ((ind = s.IndexOf("?")) > -1)
            {
                if (ind == 0)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"?\"");
                if (ind == s.Length - 1)
                    throw new Exception(s.errorOutput() + "Cannot end expression with \"?\"");

                int cind = s.IndexOf(':');
                if (cind == -1)
                    throw new Exception(s.errorOutput() + "\":\" expected after \"?\"");

                String wordLeft = getWordBefore(s.text, ind);
                String wordRightTrue = s.Substring(ind + 1, cind - ind - 1);
                String wordRightFalse = getNextWord(s.text, cind + 1);
                if (wordLeft.Trim() == "" || wordRightTrue.Trim() == "" || wordRightFalse.Trim() == "")
                    throw new Exception(s.errorOutput() + "\"?\" takes three arguments");

                if (!isCorrectObjectName(wordLeft.Trim()))
                    throw new Exception(s.errorOutput() + "Invalid operand: \"" + wordLeft.Trim() + "\"");
                if (!isCorrectObjectName(wordRightTrue.Trim()))
                    throw new Exception(s.errorOutput() + "Invalid operand: \"" + wordRightTrue.Trim() + "\"");
                if (!isCorrectObjectName(wordRightFalse.Trim()))
                    throw new Exception(s.errorOutput() + "Invalid operand: \"" + wordRightFalse.Trim() + "\"");

                s.text = s.Substring(0, ind - wordLeft.Length) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(cind + 1 + wordRightFalse.Length);

                r = new NodeConditional();
                r.id = tree.NodesCount;
                tree.Add(r);
                wordLeft = wordLeft.Trim();
                wordRightTrue = wordRightTrue.Trim();
                wordRightFalse = wordRightFalse.Trim();

                r.condition = getNodeFromIdentifyer(wordLeft, tree, s);
                r.nodeTrue = getNodeFromIdentifyer(wordRightTrue, tree, s);
                r.nodeFalse = getNodeFromIdentifyer(wordRightFalse, tree, s);

                r.condition.parent = r;
                r.nodeTrue.parent = r;
                r.nodeFalse.parent = r;
                r.parent = parent;
            }
        }

        private List<NodeAssignment> processTier17(ref Command s, CallTree tree, CallTreeNode parent = null)//=, *=, /=, %=, +=, -=, <<=, >>=, &=, ^=, |=
        {
            List<NodeAssignment> res = new List<NodeAssignment>();
            int ind;
            NodeAssignment r;
            char c;
            while ((ind = s.LastIndexOf('=')) > -1)//right to left to allow chaining
            {
                c = s[ind];
                if (ind < 1)
                    throw new Exception(s.errorOutput() + "Cannot start expression with \"=\"");
                c = s[ind - 1];
                if (c == '*')
                {
                    r = new NodeAssignmentMul();
                    processBinaryOperation(ref s, "*=", ind - 1, r, parent, tree);
                }
                else if (c == '/')
                {
                    r = new NodeAssignmentDiv();
                    processBinaryOperation(ref s, "/=", ind - 1, r, parent, tree);
                }
                else if (c == '%')
                {
                    r = new NodeAssignmentRemainder();
                    processBinaryOperation(ref s, "%=", ind - 1, r, parent, tree);
                }
                else if (c == '+')
                {
                    r = new NodeAssignmentPlus();
                    processBinaryOperation(ref s, "+=", ind - 1, r, parent, tree);
                }
                else if (c == '-')
                {
                    r = new NodeAssignmentMinus();
                    processBinaryOperation(ref s, "-=", ind - 1, r, parent, tree);
                }
                else if (c == '<')
                {
                    if (ind < 2)
                        throw new Exception(s.errorOutput() + "Cannot start expression with \"<<=\"");
                    if ((c = s[ind - 2]) != '<')
                        throw new Exception(s.errorOutput() + "Unknown operator: " + c + "<=");
                    r = new NodeAssignmentLeftShift();
                    processBinaryOperation(ref s, "<<=", ind - 2, r, parent, tree);
                }
                else if (c == '>')
                {
                    if (ind < 2)
                        throw new Exception(s.errorOutput() + "Cannot start expression with \">>=\"");
                    if ((c = s[ind - 2]) != '>')
                        throw new Exception(s.errorOutput() + "Unknown operator: " + c + ">=");
                    r = new NodeAssignmentRightShift();
                    processBinaryOperation(ref s, ">>=", ind - 2, r, parent, tree);
                }
                else if (c == '&')
                {
                    r = new NodeAssignmentAND();
                    processBinaryOperation(ref s, "&=", ind - 1, r, parent, tree);
                }
                else if (c == '^')
                {
                    r = new NodeAssignmentXOR();
                    processBinaryOperation(ref s, "^=", ind - 1, r, parent, tree);
                }
                else if (c == '|')
                {
                    r = new NodeAssignmentOR();
                    processBinaryOperation(ref s, "|=", ind - 1, r, parent, tree);
                }
                else
                {
                    r = new NodeAssignment();
                    processBinaryOperation(ref s, "=", ind, r, parent, tree);
                }
                res.Add(r);
            }
            return res;
        }
        #endregion

        #region Tools
        private void ProcessLiterals(ref String identifyer)
        {
            identifyer = identifyer.Replace("\\'", "\'");
            identifyer = identifyer.Replace("\\\"", "\"");
            identifyer = identifyer.Replace("\\\\", "\\");
            identifyer = identifyer.Replace("\\0", "\0");
            identifyer = identifyer.Replace("\\a", "\a");
            identifyer = identifyer.Replace("\\b", "\b");
            identifyer = identifyer.Replace("\\f", "\f");
            identifyer = identifyer.Replace("\\n", "\n");
            identifyer = identifyer.Replace("\\r", "\r");
            identifyer = identifyer.Replace("\\t", "\t");
            identifyer = identifyer.Replace("\\v", "\v");
        }

        private void processBinaryOperation(ref Command s, String op, int ind, BinaryNode r, CallTreeNode parent, CallTree tree)
        {
            String wordLeft = getWordBefore(s.text, ind);
            String wordRight = getNextWord(s.text, ind + op.Length);
            if (wordLeft.Trim() == "" || wordRight.Trim() == "")
                throw new Exception(s.errorOutput() + "\"" + op + "\" takes two arguments");
            if (!isCorrectObjectName(wordLeft.Trim()))
                throw new Exception(s.errorOutput() + "Invalid operand: \"" + wordRight.Trim() + "\"");
            if (!isCorrectObjectName(wordRight.Trim()))
                throw new Exception(s.errorOutput() + "Invalid operand: \"" + wordRight.Trim() + "\"");
            s.text = s.Substring(0, ind - wordLeft.Length) + ";" + tree.NodesCount.ToString() + ";" + s.Substring(ind + op.Length + wordRight.Length);
            r.id = tree.NodesCount;
            tree.Add(r);
            wordLeft = wordLeft.Trim();
            wordRight = wordRight.Trim();

            r.child1 = getNodeFromIdentifyer(wordLeft, tree, s);
            r.child2 = getNodeFromIdentifyer(wordRight, tree, s);

            r.child1.parent = r;
            r.child2.parent = r;
            r.parent = parent;
        }

        private CallTreeNode getNodeFromIdentifyer(String t, CallTree tree, Command c, bool canBeType = false)
        {
            if (t[0] == ';')
                return tree.GetNode(Convert.ToInt32(t.Substring(1, t.Length - 2)));
            else
            {
                if (!canBeType && ReflectionHelper.GetType(t) != null)
                    throw new Exception(c.errorOutput() + "\"" + t + "\" is a type, but is used as identifyer");
                var a = new NodeIdentifyerObject() { identifyer = t, vars = parent._variables };
                tree.nodes.Add(a);
                return a;
            }
        }

        internal String getNextWord(String s, int start, bool array = false, bool typeBrackets = false, bool b_brackets = false)
        {
            if (start > s.Length - 1)
                return "";
            String t = "", tb;
            bool word = false;
            char c;
            int brackets = 0;
            for (int i = start; i < s.Length; i++)
            {
                c = s[i];
                if (c == ' ')
                {
                    if (t.Trim() == "" || (b_brackets && brackets != 0))
                    {
                        t += ' ';
                        continue;
                    }
                    else
                        return t;
                }
                if (typeBrackets && c == '<' && IsTypeBrackets(s, i + 1, out tb, false))
                {
                    t += "<" + tb + ">";
                    i += tb.Length + 1;
                }
                else if (b_brackets && c == '[')
                {
                    brackets++;
                    t += c;
                }
                else if (b_brackets && c == ']')
                {
                    brackets--;
                    t += c;
                }
                else if (array ? isSymbolOrArray(c) : isSymbol(c))
                {
                    if (word || t.Trim() == "")//inc word
                    {
                        word = true;
                        t += c;
                    }
                    else if (!b_brackets || brackets != 0)//character after symbol
                        return t;
                }
                else
                {
                    if (word)//symbol after name
                        return t;
                    else
                        t += c;
                }
            }
            return t;
        }

        internal char getNextNonSpaceCharacter(String s, int start)
        {
            for (int i = start; i < s.Length; i++)
                if (s[i] != ' ' && s[i] != '\r' && s[i] != '\n')
                    return s[i];
            return (char)0;
        }

        internal char getPrevNonSpaceCharacter(String s, int start)
        {
            for (int i = start; i >= 0; i--)
                if (s[i] != ' ' && s[i] != '\r' && s[i] != '\n')
                    return s[i];
            return (char)0;
        }

        internal static bool IsTypeBrackets(String s, int start, out string br, bool checkType = true)//TODO check
        {
            br = "";
            int count = 1;
            char c;
            for (int i = start; i < s.Length; i++)
            {
                c = s[i];
                if (c == '<')
                    count++;
                else if (c == '>')
                {
                    count--;
                    if (count == 0)
                        break;
                }
                else if (!char.IsLetterOrDigit(c) && c != ' ' && c != ',' && c != '.' && c != '[' && c != ']')
                    return false;

                br += c;
            }
            if (count > 0)
                return false;

            return !checkType || ReflectionHelper.GetType(br) != null;
        }

        internal String getWordBefore(String s, int pos)
        {
            String r = "";
            char c;
            pos--;
            for (; pos >= 0; pos--)
            {
                c = s[pos];
                if (isSymbol(c) || (r.Trim() == "" && (c == ' ' || c == '\r' || c == '\n')))
                    r = c + r;
                else
                    break;
            }
            return r;
        }

        private String getWordArrayBefore(String s, int pos)
        {
            String r = "";
            char c;
            pos--;
            int brackets = 0;
            for (; pos >= 0; pos--)
            {
                c = s[pos];
                if (isSymbolOrArray(c) || (r.Trim() == "" && (c == ' ' || c == '\r' || c == '\n')) || brackets != 0)
                {
                    r = c + r;
                    if (c == '[')
                        brackets++;
                    else if (c == ']')
                        brackets--;
                }
                else
                    break;
            }
            return r;
        }

        internal String getParentheses(String s, int start)
        {
            String r = "";
            char c;
            int parentheses = 0;
            for (int i = start + 1; i < s.Length; i++)
            {
                c = s[i];
                if (c == '(')
                    parentheses++;
                else if (c == ')')
                    parentheses--;
                if (parentheses == -1)
                    return r;
                else
                    r += c;
            }
            throw new Exception("\")\" expected but \";\" found");
        }

        private String getBrackets(String s, int start)
        {
            String r = "";
            char c;
            int brackets = 0;
            for (int i = start + 1; i < s.Length; i++)
            {
                c = s[i];
                if (c == '[')
                    brackets++;
                else if (c == ']')
                    brackets--;
                if (brackets == -1)
                    return r;
                else
                    r += c;
            }
            throw new Exception("Unexpected end of statement");
        }

        internal int indexOfWord(String cmd, String word)
        {
            bool var = isCorrectTypeName(word);
            bool prev = !var;
            int t = 0;
            char c = ' ';
            while ((t = cmd.IndexOf(word, t)) > -1)
            {
                if (t > 0)
                    if ((var && isSymbol((c = cmd[t - 1]))) || (!var && !isSymbol(c) && c != ' '))//part of another word
                        continue;
                if (t + word.Length < cmd.Length)
                {
                    if ((var && isSymbol((c = cmd[t + word.Length]))) || (!var && !isSymbol(c) && c != ' '))//part of another word
                        continue;
                }
                return t;
            }
            return -1;
        }

        private CallTreeNode getNode(String s, CallTree tree)
        {
            return tree.GetNode(Convert.ToInt32(s.Substring(1, s.Length - 2)));
        }
        #endregion
        #endregion

        #region Tools
        internal bool isSymbol(char _c)
        {
            return (_c >= 'a' && _c <= 'z') || (_c >= 'A' && _c <= 'Z') || (_c >= '0' && _c <= '9') || _c == '_' || _c == '.' || _c == ';';
        }

        internal bool isSymbolOrArray(char _c)
        {
            return (_c >= 'a' && _c <= 'z') || (_c >= 'A' && _c <= 'Z') || (_c >= '0' && _c <= '9') || _c == '_' || _c == '.' || _c == ';' || _c == '[' || 
                _c == ']' || _c == ',';
        }

        internal bool isNameSymbol(char _c)
        {
            return (_c >= 'a' && _c <= 'z') || (_c >= 'A' && _c <= 'Z') || (_c >= '0' && _c <= '9') || _c == '_' || _c == ';';
        }

        internal bool isStartSymbol(char _c)
        {
            return (_c >= 'a' && _c <= 'z') || (_c >= 'A' && _c <= 'Z') || _c == '_' || _c == ';';
        }

        private bool isCorrectTypeName(String s)
        {
            return isCorrectTypeName(s, 0, s.Length);
        }

        private bool isCorrectTypeName(String s, int start, int length)
        {
            if (length == 0)
                return false;
            if (!isStartSymbol(s[start]))//starts wrong
                return false;
            for (int i = start + 1; i < start + length; i++)
            {
                if (!isSymbol(s[i]))
                    return false;
            }
            return true;
        }

        internal bool isCorrectVarName(String s)
        {
            return isCorrectVarName(s, 0, s.Length);
        }

        private bool isCorrectVarName(String s, int start, int length)
        {
            if (length == 0)
                return false;
            if (!isStartSymbol(s[start]))//starts wrong
                return false;
            for (int i = start + 1; i < start + length; i++)
            {
                if (!isNameSymbol(s[i]))
                    return false;
            }
            return true;
        }

        private bool isCorrectObjectName(String s)
        {
            return isCorrectObjectName(s, 0, s.Length);
        }

        private bool isCorrectObjectName(String s, int start, int length)
        {
            if (length == 0)
                return false;
            for (int i = start; i < start + length; i++)
            {
                if (!isSymbol(s[i]))
                    return false;
            }
            return true;
        }

        private bool isCorrectObjectOrArrayName(String s)
        {
            return isCorrectObjectOrArrayName(s, 0, s.Length);
        }

        private bool isCorrectObjectOrArrayName(String s, int start, int length)
        {
            if (length == 0)
                return false;
            int brackets = 0;
            char c;
            for (int i = start; i < start + length; i++)
            {
                c = s[i];
                if (!isSymbolOrArray(s[i]) && brackets == 0)
                    return false;
                if (c == '[')
                    brackets++;
                else if (c == ']')
                    brackets--;
            }
            return brackets == 0;
        }
        #endregion
    }
}
