using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet
{
    class Tokenizer
    {
        public const int SEPARATOR = ';';

        public ScripterVM parent;

        public Structure.StructureCommands Tokenize(System.IO.StringReader sr)
        {
            Stack<Structure.StructureCommands> structureStack = new Stack<Structure.StructureCommands>();

            int line = 0, pos = 0;
            int lineStart = 0, posStart = 0;
            int braces = 0;

            structureStack.Clear();
            //List<Command> commands = new List<Command>();
            var a = new Structure.StructureCommands();
            structureStack.Push(a);
            String _com = "";
            int _c = 0, _cprev;
            line = 0;
            pos = 0;
            lineStart = 0;
            posStart = 0;
            bool singleQuotation = false, doubleQuotation = false;
            bool comments = false;

            while (sr.Peek() > -1)//eof
            {
                _cprev = _c;
                _c = sr.Read();
                pos++;
                if (comments && _c != '\n')
                    continue;

                if (_c == SEPARATOR && !singleQuotation && !doubleQuotation)//separator. save command
                {
                    processCommand(ref _com, ref structureStack, ref lineStart, ref posStart, ref braces);
                    //commands.Add(new Command(_com.Trim(), lineStart, posStart));
                    //structureStack.Pop().
                    _com = "";
                    lineStart = line;
                    posStart = pos;
                }
                else if (_c == '\n')//inc cur line
                {
                    line++;
                    pos = 0;
                    _com += " \r\n";
                    comments = false;
                }
                else if (_c == '\'')
                {
                    if (!doubleQuotation)
                        if (!singleQuotation || _cprev != '\\')
                            singleQuotation = !singleQuotation;
                    _com += (char)_c;
                }
                else if (_c == '\"')
                {
                    if (!singleQuotation)
                        if (!doubleQuotation || _cprev != '\\')
                            doubleQuotation = !doubleQuotation;
                    _com += (char)_c;
                }
                else if (_c == '/')
                {
                    if (singleQuotation || doubleQuotation)
                        _com += (char)_c;
                    else
                    {
                        if (sr.Peek() == '/')
                        {
                            comments = true;
                            sr.Read();
                        }
                        else
                            _com += "/";
                        //if (prevBackSlash)
                        //    comments = true;
                        //else
                        //    prevBackSlash = true;
                    }
                }
                else if (_c != '\r')//ignore \n
                {
                    _com += (char)_c;
                }
            }

            _com = Trim(_com, ref lineStart, ref posStart);
            if (_com.EndsWith("}"))
                processCommand(ref _com, ref structureStack, ref lineStart, ref posStart, ref braces);

            if (_com != "")//unfinished command
                throw new Exception(errorOutput(ref lineStart, ref posStart, _com.Length) + "\";\" expected");

            if (structureStack.Count > 1)
                if (structureStack.Peek() is Structure.StructureFor)
                    throw new Exception(errorOutput(ref lineStart, ref posStart) + "Unexpected end of \"for\"");
                else
                    throw new Exception(errorOutput(ref lineStart, ref posStart) + "\"}\" expected");

            return a;
        }



        private void processCommand(ref String s, ref Stack<Structure.StructureCommands> structureStack, ref int lineStart, ref int posStart, ref int braces)
        {
            s = Trim(s, ref lineStart, ref posStart);
            Structure.StructureCommands popped = null;
            Structure.CommandStructure lastCommand = null;
            while (s != "")
            {
                #region for parentheses check
                popped = structureStack.Peek();
                if (popped is Structure.StructureFor)
                {
                    if ((popped as Structure.StructureFor).cur  < 2)//TODO check command integrity
                    {
                        popped.AddCommand(new Structure.CommandStructure() { c_command = new Command(s, lineStart, posStart) });
                        return;
                    }
                    else if ((popped as Structure.StructureFor).cur == 2)
                    {
                        if (s.IndexOf(')') == -1)
                            throw new Exception(errorOutput(ref lineStart, ref posStart) + "\")\" expected");
                        String pp;
                        try
                        {
                            pp = parent._parser.getParentheses(s, -1);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(errorOutput(ref lineStart, ref posStart) + e.Message);
                        }
                        popped.multiLine = false;
                        popped.AddCommand(new Structure.CommandStructure() { c_command = new Command(pp, lineStart, posStart) });
                        s = Substring(s, pp.Length + 1, ref lineStart, ref posStart);
                        s = Trim(s, ref lineStart, ref posStart);
                        continue;
                    }
                }
                #endregion

                #region {
                if (s.StartsWith("{"))
                {
                    braces++;
                    s = Substring(s, 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);
                    popped = structureStack.Peek();
                    var a = new Structure.StructureCommands();
                    a.multiLine = true;
                    popped.AddCommand(a);
                    structureStack.Push(a);
                    continue;
                }
                #endregion
                #region }
                else if (s.StartsWith("}"))
                {
                    braces--;
                    s = Substring(s, 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);
                    if (braces < 0)
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + "Unexpected \"}\"");
                    removeExcess(ref structureStack);
                    popped = structureStack.Pop();
                    removeExcess(ref structureStack);
                    continue;
                }
                #endregion
                #region elseif
                else if (s.StartsWith("elseif"))
                {
                    if (s.Length > 6 && parent._parser.isNameSymbol(s[6]))
                        break;
                    s = "else if" + s.Substring(6);
                    continue;
                }
                #endregion
                #region if
                else if (s.StartsWith("if"))
                {
                    if (s.Length > 2 && parent._parser.isNameSymbol(s[2]))
                        break;

                    if (parent._parser.getNextNonSpaceCharacter(s, 2) != '(')
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 2) + "\"(\" expected after \"if\"");
                    String p;
                    try
                    {
                        p = parent._parser.getParentheses(s, s.IndexOf('('));
                    }
                    catch (Exception e)
                    {
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + e.Message);
                    }
                    Structure.StructureIf a = new Structure.StructureIf();
                    a.c_command = new Command(p, lineStart, posStart);

                    s = Substring(s, s.IndexOf('(') + 2 + p.Length, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);
                    popped = structureStack.Peek();
                    popped.AddCommand(a);

                    //a.multiLine = parent._parser.getNextNonSpaceCharacter(s, 0) == '{';
                    a.multiLine = false;
                    structureStack.Push(a);

                    continue;
                }
                #endregion
                #region else
                else if (s.StartsWith("else"))
                {
                    if (s.Length > 4 && parent._parser.isNameSymbol(s[4]))
                        break;
                    //popped = structureStack.Peek();
                    //lastCommand = popped.GetLastCommand();
                    lastCommand = GetLastIfStatement(structureStack);
                    if (lastCommand == null || !(lastCommand is Structure.StructureIf))
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + "\"if\" expected before \"else\"");
                    s = s.Substring(4);
                    posStart += 4;
                    s = Trim(s, ref lineStart, ref posStart);

                    (lastCommand as Structure.StructureIf).curTrue = false;
                    //(lastCommand as Structure.StructureIf).multiLine = parent._parser.getNextNonSpaceCharacter(s, 0) == '{';
                    (lastCommand as Structure.StructureIf).multiLine = false;
                    structureStack.Push(lastCommand as Structure.StructureIf);

                    continue;
                }
                #endregion
                #region while
                else if (s.StartsWith("while"))
                {
                    if (s.Length > 5 && parent._parser.isNameSymbol(s[5]))
                        break;
                    if (parent._parser.getNextNonSpaceCharacter(s, 5) != '(')
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 5) + "\"(\" expected after \"while\"");
                    String p;
                    try
                    {
                        p = parent._parser.getParentheses(s, s.IndexOf('('));
                    }
                    catch (Exception e)
                    {
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + e.Message);
                    }
                    lastCommand = structureStack.Peek().GetLastCommand();
                    Structure.StructureCommands a = (lastCommand != null && lastCommand is Structure.StructureDoWhile) ?
                        (lastCommand as Structure.StructureCommands) : new Structure.StructureWhile();
                    //Structure.StructureWhile a = new Structure.StructureWhile();
                    a.c_command = new Command(p, lineStart, posStart);

                    s = Substring(s, s.IndexOf('(') + 2 + p.Length, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);

                    if (a is Structure.StructureWhile)
                    {
                        popped = structureStack.Peek();
                        popped.AddCommand(a);
                        //a.multiLine = parent._parser.getNextNonSpaceCharacter(s, 0) == '{';
                        a.multiLine = false;
                        structureStack.Push(a);
                    }
                    continue;
                }
                #endregion
                #region do
                else if (s.StartsWith("do"))
                {
                    if (s.Length > 3 && parent._parser.isNameSymbol(s[3]))
                        break;
                    s = s.Substring(2);
                    posStart += 2;
                    s = Trim(s, ref lineStart, ref posStart);
                    Structure.StructureDoWhile a = new Structure.StructureDoWhile();
                    a.multiLine = false;
                    structureStack.Peek().AddCommand(a);
                    structureStack.Push(a);
                    continue;
                }
                #endregion
                #region for
                else if (s.StartsWith("for"))
                {
                    if (s.Length > 3 && parent._parser.isNameSymbol(s[3]))
                        break;
                    if (parent._parser.getNextNonSpaceCharacter(s, 3) != '(')
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 3) + "\"(\" expected after \"for\"");
                    Structure.StructureFor a = new Structure.StructureFor();
                    //a.multiLine = false;

                    s = Substring(s, s.IndexOf('(') + 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);
                    popped = structureStack.Peek();
                    popped.AddCommand(a);

                    structureStack.Push(a);
                    continue;
                }
                #endregion
                #region break
                else if (s.StartsWith("break"))
                {
                    if (s.Length > 5 && parent._parser.isNameSymbol(s[5]))
                        break;
                    if (s.Length < 6 || s[5] == ' ' || s[5] == '(')
                    {
                        var cbr = new Structure.CommandBreak();
                        s = s.Substring(5);
                        posStart += 5;
                        s = Trim(s, ref lineStart, ref posStart);
                        if (s.Length > 0)
                            if (s[0] == '(')
                            {
                                String t;
                                try
                                {
                                    t = parent._parser.getParentheses(s, 0);
                                }
                                catch (Exception e)
                                {
                                    throw new Exception(errorOutput(ref lineStart, ref posStart) + e.Message);
                                }
                                if (!int.TryParse(t, out cbr.number))
                                    throw new Exception(errorOutput(ref lineStart, ref posStart, 1) + "Only numbers are accepted as parameter for \"break\"");
                                if (cbr.number < 1)
                                    throw new Exception(errorOutput(ref lineStart, ref posStart, 1) + "Only numbers greater than zero are accepted as \"break\" parameter");
                            }
                            else
                                throw new Exception(errorOutput(ref lineStart, ref posStart) + "Unexpected symbol: \"" + s[0] + "\"");
                        if (!HasEnoughCycles(structureStack, cbr.number, true))
                            throw new Exception(errorOutput(ref lineStart, ref posStart) + "Not enough whiles / fors / switches detected to break");
                        structureStack.Peek().AddCommand(cbr);
                        removeExcess(ref structureStack);

                        return;
                    }
                }
                #endregion
                #region continue
                else if (s.StartsWith("continue"))
                {
                    if (s.Length > 8 && parent._parser.isNameSymbol(s[5]))
                        break;
                    if (s.Length < 9 || s[8] == ' ')
                    {
                        var cbr = new Structure.CommandContinue();
                        s = s.Substring(8);
                        posStart += 8;
                        s = Trim(s, ref lineStart, ref posStart);
                        if (s.Length > 0)
                            throw new Exception(errorOutput(ref lineStart, ref posStart) + "Unexpected symbol: \"" + s[0] + "\"");
                        if (!HasEnoughCycles(structureStack, 1, true))
                            throw new Exception(errorOutput(ref lineStart, ref posStart) + "While / for / not detected to continue");
                        structureStack.Peek().AddCommand(cbr);
                        removeExcess(ref structureStack);

                        return;
                    }
                }
                #endregion
                #region switch
                else if (s.StartsWith("switch"))
                {
                    if (s.Length > 6 && parent._parser.isNameSymbol(s[6]))
                        break;
                    if (parent._parser.getNextNonSpaceCharacter(s, 6) != '(')
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + "\"(\" expected after \"switch\"");
                    s = Substring(s, s.IndexOf('(') + 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);

                    String p;
                    try
                    {
                        p = parent._parser.getParentheses(s, -1);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + e.Message);
                    }
                    Structure.StructureSwitch a = new Structure.StructureSwitch();
                    a.c_command = new Command(p, lineStart, posStart);
                    a.multiLine = false;
                    structureStack.Peek().AddCommand(a);
                    structureStack.Push(a);

                    s = Substring(s, p.Length + 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);
                    if (s[0] != '{')
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + "\"{\" expected after \"switch\"");

                    continue;
                }
                #endregion
                #region case
                else if (s.StartsWith("case"))
                {
                    if (s.Length > 4 && parent._parser.isNameSymbol(s[4]))
                        break;
                    popped = structureStack.Pop();
                    var a = structureStack.Peek();
                    structureStack.Push(popped);
                    popped = a;
                    if (!(popped is Structure.StructureSwitch))
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + "\"case\" can only be used inside \"switch\" statement");
                    int ind = s.IndexOf(':', 4);
                    if (ind == -1)
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 4) + "\":\" expected");

                    String t = s.Substring(4, ind - 4).Trim();
                    if (t == "")
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 4) + "Identifyer expected");

                    (popped as Structure.StructureSwitch).s_caseIndices.Add(new Command(t, lineStart, posStart), structureStack.Peek().commands.Count);
                    s = Substring(s, ind + 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);

                    continue;
                }
                #endregion
                #region default
                else if (s.StartsWith("default"))
                {
                    if (s.Length > 7 && parent._parser.isNameSymbol(s[7]))
                        break;
                    popped = structureStack.Pop();
                    var a = structureStack.Peek();
                    structureStack.Push(popped);
                    popped = a;
                    if (!(popped is Structure.StructureSwitch))
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + "\"default\" can only be used inside \"switch\" statement");
                    int ind = s.IndexOf(':');
                    if (ind == -1)
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 7) + "\":\" expected");

                    String t = s.Substring(7, ind - 7).Trim();
                    if (t != "")
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 7) + "\"default\" does not accept arguments");
                    s = Substring(s, ind + 1, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);

                    (popped as Structure.StructureSwitch).def = structureStack.Peek().commands.Count;
                    continue;
                }
                #endregion
                #region function
                else if (s.StartsWith("function"))
                {
                    if (s.Length > 8 && parent._parser.isNameSymbol(s[8]))
                        break;
                    Structure.StructureFunction a = new Structure.StructureFunction();
                    a.c_command = new Command("", lineStart, posStart);
                    a.multiLine = false;

                    String t = parent._parser.getNextWord(s, 8, true, true, true);
                    Type type;
                    if (t.Trim() == "void")
                        type = null;
                    else if ((type = ReflectionHelper.GetType(t.Trim())) == null)
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 9) + "Return type expected");
                    a.returnType = type;

                    String fn = parent._parser.getNextWord(s, 9 + t.Length);
                    if (!parent._parser.isCorrectVarName(fn.Trim()))
                        if (fn.Trim().Length == 0)
                            throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length) + "Function name expected");
                        else
                            throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length) + "\"" + fn + "\" is not a correct function name");
                    a.functionName = fn;

                    if (parent._parser.getNextNonSpaceCharacter(s, 9 + t.Length + fn.Length) != '(')
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length + fn.Length) + "\"(\" expected");
                    String par;
                    try
                    {
                        par = parent._parser.getParentheses(s, 9 + t.Length + fn.Length);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(errorOutput(ref lineStart, ref posStart) + e.Message);
                    }
                    var arr = par.Split(',');
                    String[] words;
                    if (par.Trim() != "")
                        for (int i = 0; i < arr.Length; i++)
                        {
                            words = arr[i].Trim().Split(' ');
                            if (words.Length != 2)
                                if (words.Length == 1 && arr.Length >= i + 1 && arr[i].Contains('<') && arr[i + 1].Contains('>'))//TODO improve for more then 2 pars
                                {
                                    var ts = arr[i + 1].Trim().Split(' ');
                                    if (ts.Length != 2)
                                        throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length + fn.Length) + "Invalid parameter declaration: \"" + arr[i].Trim() + "\"");
                                    words = new string[] { words[0] + "," + ts[0], ts[1] };
                                    i++;
                                }
                                else
                                    throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length + fn.Length) + "Invalid parameter declaration: \"" + arr[i].Trim() + "\"");
                            type = ReflectionHelper.GetType(words[0].Trim());
                            if (type == null)
                                throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length + fn.Length) + "\"" + words[0] + "\" is not a valid type");
                            if (!parent._parser.isCorrectVarName(words[1]))
                                throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length + fn.Length) + "\"" + words[1] + "\" is not a valid variable name");
                            if (ReflectionHelper.GetType(words[1]) != null)
                                throw new Exception(errorOutput(ref lineStart, ref posStart, 9 + t.Length + fn.Length) + "\"" + words[1] + "\" is a type, but is used an identifyer");
                            a.parameters.Add(words[1].Trim(), type);
                        }

                    if (parent._parser.getNextNonSpaceCharacter(s, 11 + t.Length + fn.Length + par.Length) != '{')
                        throw new Exception(errorOutput(ref lineStart, ref posStart, 11 + t.Length + fn.Length + par.Length) + "\"{\" expected");
                    s = Substring(s, 11 + t.Length + fn.Length + par.Length, ref lineStart, ref posStart);
                    s = Trim(s, ref lineStart, ref posStart);

                    structureStack.Peek().AddCommand(a);
                    structureStack.Push(a);

                    parent._functions.AddScriptedFunction(a.functionName, a);
                    continue;
                }
                #endregion
                #region return
                else if (s.StartsWith("return"))
                {
                    if (s.Length > 6 && parent._parser.isNameSymbol(s[6]))
                        break;

                    if (s.Length == 6)
                        structureStack.Peek().AddCommand(new Structure.CommandReturn());
                    else
                    {
                        s = Substring(s, 6, ref lineStart, ref posStart);
                        s = Trim(s, ref lineStart, ref posStart);
                        structureStack.Peek().AddCommand(new Structure.CommandReturn() { c_command = new Command(s, lineStart, posStart) });
                    }
                    removeExcess(ref structureStack);
                    return;
                }
                #endregion
                break;
            }

            var r = structureStack.Peek();
            r.AddCommand(new Structure.CommandStructure() { c_command = new Command(s, lineStart, posStart) });
            if (!r.multiLine)
                removeExcess(ref structureStack);
        }



        private Structure.StructureIf GetLastIfStatement(Stack<Structure.StructureCommands> structureStack)
        {
            var a = structureStack.Peek();
            var b = a.GetLastCommand();
            while (true)
            {
                if (b is Structure.StructureWhile && !(b as Structure.StructureWhile).multiLine)
                    b = (b as Structure.StructureWhile).GetLastCommand();
                else if (b is Structure.StructureDoWhile && !(b as Structure.StructureDoWhile).multiLine)
                    b = (b as Structure.StructureDoWhile).GetLastCommand();
                else if (b is Structure.StructureFor && !(b as Structure.StructureFor).multiLine)
                    b = (b as Structure.StructureFor).GetLastCommand();
                else if (b is Structure.StructureIf)
                    if ((b as Structure.StructureIf).IfFalse.commands.Count != 0)
                        b = (b as Structure.StructureIf).IfFalse.GetLastCommand();
                    else
                        return b as Structure.StructureIf;
                else
                    return null;
            }
        }

        private bool HasEnoughCycles(Stack<Structure.StructureCommands> structureStack, int n, bool countSwitch = false)
        {
            return true;

            //checks runtinme because it's hard to detect single-line cycles
            
            /*
            Structure.StructureCommands[] a = new Structure.StructureCommands[structureStack.Count];
            structureStack.CopyTo(a, 0);
            for (int i = 0; i < a.Length; i++)
                if (a[i] is Structure.StructureDoWhile || a[i] is Structure.StructureFor || a[i] is Structure.StructureWhile || 
                    (countSwitch && a[i] is Structure.StructureSwitch))
                        n--;
            bool c = false;
            if (a[a.Length - 1] is Structure.StructureCommands)
            {
                var b = a[a.Length - 1].GetLastCommand();
                if (b is Structure.StructureIf)
                    if ((b as Structure.StructureIf).IfTrue
            }
            if (c)
                n--;
            return n == 0;//*/
        }

        private String errorOutput(ref int lineStart, ref int posStart, int del = 0)
        {
            return "[" + (lineStart + 1) + ";" + (posStart + del) + "] ";
        }

        private String Substring(String s, int start, ref int lineStart, ref int posStart)
        {
            int t = -1;
            int del = start;
            while ((t = s.IndexOf('\n', t + 1)) != -1 && t < start)
            {
                del = start - t + 1;
                lineStart++;
                posStart = 0;
            }
            posStart += del;
            return s.Substring(start);
        }

        private String Trim(String s, ref int lineStart, ref int posStart)
        {
            int t = 0;
            char c;
            while (t < s.Length)
            {
                c = s[t];
                if (c == ' ')
                    posStart++;
                else if (c == '\n')
                {
                    lineStart++;
                    posStart = 0;
                }
                else if (c != '\r')
                    break;
                t++;
            }
            return s.Trim();
        }

        private void removeExcess(ref Stack<Structure.StructureCommands> structureStack)
        {
            while (!structureStack.Peek().multiLine)
                structureStack.Pop();
        }
    }
}
