using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ScripterNet
{
    [Serializable]
    class Processor
    {
        public const int SEPARATOR = ';';

        [NonSerialized]
        //List<Structure.StructureCommands> commands;
        Structure.StructureCommands __SerCom;

        [NonSerialized]
        public ScripterVM parent;



        public Processor()
        {
            //commands = new List<Structure.StructureCommands>();
        }

        public void Compile(System.IO.StringReader sr, String fn)
        {
            __SerCom = parent._tokenizer.Tokenize(sr);
            __SerCom.Parse(parent);

            if (System.IO.File.Exists(fn))
                throw new Exception("File " + fn + " already exists and override is set to false");

            try
            {
                System.IO.FileStream s = new FileStream(fn, FileMode.Create);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bf.Serialize(s, this);
                s.Close();
                s.Dispose();
            }
            catch
            {
                throw new Exception("IO error during compilation");
            }

            __SerCom = null;
        }

        public void Load(System.IO.Stream s)
        {
            try
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter f = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                Structure.StructureCommands t;
                //commands.Add(t = (f.Deserialize(s) as Processor).__SerCom);
                t = (f.Deserialize(s) as Processor).__SerCom;
                t.PostLoad(parent);
                t.Execute(0, true);
            }
            catch
            {
                throw new Exception("Provided file is not a valid compiled script");
            }
        }

        public void Execute(System.IO.StringReader sr)
        {
            parent.State = ScripterVM.VMState.Parsing;
            var c = parent._tokenizer.Tokenize(sr);
            _parseCommands(c);
            parent.State = ScripterVM.VMState.Executing;
            c.Execute(0, true);
            parent.State = ScripterVM.VMState.Idle;
        }

        private void _parseCommands(Structure.StructureCommands commands)
        {
            commands.Parse(parent);
        }

        private void checkNodesForCompletieness(List<CallTreeNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is UnaryNode)
                {
                    if ((nodes[i] as UnaryNode).child1 == null)
                        throw new Exception("Engine error 0xE71: Node parameter not initialized!");
                }
                else if (nodes[i] is BinaryNode)
                {
                    if ((nodes[i] as BinaryNode).child1 == null)
                        throw new Exception("Engine error 0xE72: Node parameter not initialized!");
                    if ((nodes[i] as BinaryNode).child2 == null)
                        throw new Exception("Engine error 0xE73: Node parameter not initialized!");
                }
            }
        }
    }
}
