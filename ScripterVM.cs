using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace ScripterNet
{
    public class ScripterVM
    {
        public enum VMState
        {
            Idle = 0,
            Parsing = 1,
            Executing = 2
        }

        internal Processor _processor;
        internal VariablesEngine _variables;
        internal Tokenizer _tokenizer;
        internal CommandParser _parser;
        internal FunctionsEngine _functions;
        internal CallStack _callStack;

        internal int infiniteLoopControl = 100000;

        private Thread executingThread;
        private object lockObject = 0, funcParam;
        private bool terminate = false;
        private Exception innerException = null;
        private VMState state = VMState.Idle;

        /// <summary>
        /// Controls how much iterations are made before loop is terminated
        /// </summary>
        public int InfiniteLoopControl
        {
            get { return infiniteLoopControl; }
            set
            {
                if (value < 100)
                    value = 100;
                else if (value > 1000000)
                    value = 1000000;
                infiniteLoopControl = value;
            }
        }

        /// <summary>
        /// Indicates wether VM is executing any scripts or not
        /// </summary>
        public bool IsExecuting
        {
            get
            {
                return (int)lockObject != 0;
            }
        }

        /// <summary>
        /// Returns current state of the VM
        /// </summary>
        public VMState State
        {
            get { return state; }
            internal set
            {
                if (state == value)
                    return;
                var old = state;
                state = value;
                e_InvokeStateChanged(value, old);
            }
        }

        #region Events
        public delegate void VariableChangedEventHandler(String variableName, object v_old, object v_new, Type varType, int line, int position);
        /// <summary>
        /// Invoked when a variable value is changed in any way
        /// </summary>
        public event VariableChangedEventHandler onVariableChanged;

        public delegate void DebugEventHandler(int line, int pos, String command);
        /// <summary>
        /// Invoked upon every command execution
        /// </summary>
        public event DebugEventHandler onDebug;

        public delegate void VMStateEventHandler(VMState v_new, VMState v_old);
        /// <summary>
        /// Invoked upon every command execution
        /// </summary>
        public event VMStateEventHandler onVMStateChanged;

        internal void e_InvokeDebug(Command c)
        {
            if (onDebug != null)
                onDebug.Invoke(c.lineStart, c.posStart, c.origText);
        }

        internal void e_InvokeVarChanged(String name, object v_old, object v_new, Type type)
        {
            if (onVariableChanged != null)
            {
                var a = _callStack.GetExecutingPos();
                onVariableChanged.Invoke(name, v_old, v_new, type, a[0], a[1]);
            }
        }

        internal void e_InvokeStateChanged(VMState v_new, VMState v_old)
        {
            if (onVMStateChanged != null)
            {
                onVMStateChanged.Invoke(v_new, v_old);
            }
        }
        #endregion

        /// <summary>
        /// Creates a new instance of scripting engine
        /// </summary>
        public ScripterVM()
        {
            _processor = new Processor();
            _processor.parent = this;
            _tokenizer = new Tokenizer();
            _tokenizer.parent = this;
            _parser = new CommandParser();
            _parser.parent = this;
            _variables = new VariablesEngine();
            _variables.parent = this;
            _functions = new FunctionsEngine();
            _callStack = new CallStack();

            lock (ReflectionHelper.assemblies)
            {
                ReflectionHelper.assemblies.Add(System.Reflection.Assembly.GetExecutingAssembly());
                ReflectionHelper.assemblies.Add(System.Reflection.Assembly.GetCallingAssembly());
                ReflectionHelper.assemblies.Add(System.Reflection.Assembly.GetAssembly(typeof(int)));
            }
        }

        /// <summary>
        /// Terminates any active scripts and resets any changes to the engine made after initialization
        /// </summary>
        public void Reset()
        {
            Terminate();
            _variables.Clear();
            _functions.Clear();
            _callStack.Clear();
        }

        /// <summary>
        /// Terminates any active scripts and clears pending scripts queue
        /// </summary>
        public void Terminate()
        {
            terminate = true;
            while (executingThread != null)
            {
                System.Threading.Thread.Sleep(1);
                executingThread.Abort();
                while (executingThread.ThreadState != ThreadState.Aborted && executingThread.ThreadState != ThreadState.Stopped)
                    System.Threading.Thread.Sleep(1);
                executingThread = null;
                System.Threading.Thread.Sleep(1);
            }
            terminate = false;
        }

        /// <summary>
        /// Returns value of specified variable. If variable doesn't exist, an exception is thrown.
        /// </summary>
        /// <param name="name">Name of internal variable</param>
        /// <returns>Value of the specified variable</returns>
        public object GetVariable(String name)
        {
            if (name == null || name.Length == 0)
                throw new Exception("Invalid variable name");
            return _variables.GetVariable(name);
        }

        /// <summary>
        /// Sets value of specified variable. Variable is created if it doesn't exist.
        /// </summary>
        /// <param name="name">Name of internal variable</param>
        /// <param name="value">New value</param>
        public void SetVariable(String name, object value)
        {
            if (name == null || name.Length == 0)
                throw new Exception("Invalid variable name");
            if (_variables.Contains(name))
                _variables.SetVariable(name, value);
            else
                _variables.Create(name, value == null ? typeof(object) : value.GetType(), value);
        }

        /// <summary>
        /// Invokes a specified function with specified parameters. If no such function is found, an error is thrown.
        /// </summary>
        /// <param name="name">Name of function</param>
        /// <param name="parameters">Array of parameters. Null is acceptable if function has no parameters</param>
        /// <returns>Result of function exection. If function return type is void, null is returned</returns>
        public object InvokeFunction(String name, object[] parameters)
        {
            if (name == null || name.Length == 0)
                throw new Exception("Invalid function name");
            if (parameters == null)
                parameters = new object[0];
            return _functions.Invoke(name, parameters);
        }

        /// <summary>
        /// Registers a specified function to be called from scripts
        /// </summary>
        /// <param name="name">Name that is associated with the function. </param>
        /// <param name="function"></param>
        public void RegisterFunction(String name, System.Reflection.MethodBase function)
        {
            if (name == null || name.Length == 0)
                throw new Exception("Invalid name");
            if (function == null)
                throw new Exception("Invalid function");
            if (!function.IsStatic)
                throw new Exception("Only static functions are registerable");

            _functions.RegisterFunction(name, function);
        }

        /// <summary>
        /// Removes registered function
        /// </summary>
        /// <param name="name">Function name</param>
        public void RemoveFunction(String name)
        {
            if (name == null || name.Length == 0)
                throw new Exception("Invalid function name");

            _functions.RemoveFunction(name);
        }

        /// <summary>
        /// Registers assembly that contains specified type inside engine for reference of all its containing types
        /// </summary>
        /// <param name="type">Type to register</param>
        public void RegisterAssembly(Type type)
        {
            if (type == null)
                throw new Exception("Type cannot be \"null\"");

            lock (ReflectionHelper.assemblies)
            {
                var a = System.Reflection.Assembly.GetAssembly(type);
                if (a == null)//???
                    throw new Exception("Error getting assembly");
                if (!ReflectionHelper.assemblies.Contains(a))
                    ReflectionHelper.assemblies.Add(System.Reflection.Assembly.GetAssembly(type));
            }
        }

        /// <summary>
        /// Registers specified assembly inside engine for reference of all its containing types
        /// </summary>
        /// <param name="assembly">Assembly to register</param>
        public void RegisterAssembly(System.Reflection.Assembly assembly)
        {
            if (assembly == null)
                throw new Exception("Assembly cannot be null");

            lock (ReflectionHelper.assemblies)
                if (!ReflectionHelper.assemblies.Contains(assembly))
                    ReflectionHelper.assemblies.Add(assembly);
        }

        /// <summary>
        /// Executes provided code
        /// </summary>
        /// <param name="code">Code to execute</param>
        public void Execute(String code)
        {
            if (code == null)
                throw new Exception("Parameter cannot be null");
            if (code.Length == 0)
                return;
            lock (lockObject)
            {
                if (terminate)
                    return;
                lockObject = 1;
                innerException = null;
                funcParam = code;
                executingThread = new Thread(new ThreadStart(_execute));
                executingThread.IsBackground = true;
                //executingThread.SetApartmentState(ApartmentState.STA);
                executingThread.Start();
                while (executingThread != null)
                    System.Threading.Thread.Sleep(1);
                lockObject = 0;
                if (innerException != null && !terminate)
                    throw innerException;
            }
        }

        private void _execute()
        {
            String text = funcParam as String;
            funcParam = null;
            if (text != null && text.Length > 0)
            {
                System.IO.StringReader sr = new StringReader(text);
                try
                {
                    _processor.Execute(sr);
                }
                catch (ThreadAbortException)
                {
                    State = VMState.Idle;
                    return;
                }
                catch (Exception e)
                {
                    innerException = new Exception(e.Message, e);//TODO rm e
                }
                State = VMState.Idle;
                sr.Close();
                sr.Dispose();
            }

            executingThread = null;
        }

        /// <summary>
        /// Executes code from a file
        /// </summary>
        /// <param name="fileName">File name</param>
        public void ExecuteFile(String fileName)
        {
            if (fileName == null || fileName.Length == 0)
                throw new Exception("Invalid file name");
            lock (lockObject)
            {
                if (terminate)
                    return;
                lockObject = 2;
                innerException = null;
                funcParam = fileName;
                executingThread = new Thread(new ThreadStart(_executeFile));
                executingThread.IsBackground = true;
                //executingThread.SetApartmentState(ApartmentState.STA);
                executingThread.Start();
                while (executingThread != null)
                    System.Threading.Thread.Sleep(1);
                lockObject = 0;
                if (innerException != null && !terminate)
                    throw innerException;
            }
        }

        private void _executeFile()
        {
            String fn = funcParam as String;
            funcParam = null;

            if (!File.Exists(fn))
                throw new FileNotFoundException("File \"" + fn + "\" doesn't exist");
            System.IO.StreamReader sr = new System.IO.StreamReader(fn);

            try
            {
                _processor.Load(sr.BaseStream);
            }
            catch
            {
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                String s = sr.ReadToEnd();
                Execute(s);
            }
            State = VMState.Idle;

            sr.Close();
            sr.Dispose();

            executingThread = null;
        }

        /// <summary>
        /// Compiles provided code into a file. Compiled code does not need to be parsed when loaded.
        /// </summary>
        /// <param name="code">Code to compile</param>
        /// <param name="fileName">File to save compiled code to</param>
        /// <param name="overrideIfExists">Specifies wether to override file if it already exists</param>
        public void Compile(String code, String fileName, bool overrideIfExists = true)
        {
            if (code == null)
                throw new Exception("Code cannot be null");
            if (fileName == null || fileName == "")
                throw new Exception("Invalid file name");

            if (System.IO.File.Exists(fileName))
                if (overrideIfExists)
                    System.IO.File.Delete(fileName);
                else
                    throw new Exception("File \"" + fileName + "\" alerady exists and overrideIfExists is set to false");

            _processor.Compile(new StringReader(code), fileName);
        }
    }
}
