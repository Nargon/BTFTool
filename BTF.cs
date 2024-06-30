using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BTFTool
{
    internal class BTF
    {
        public uint Records { get; private set; }
        uint something; // probably a total of bytes
        uint something2; // total string length, but there is something extra
        Dictionary<uint, DataText> content = new Dictionary<uint, DataText>();

        public bool TryParse(Stream s)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                s.CopyTo(ms); //copy file to memory for fast seek
                ms.Seek(0, SeekOrigin.Begin);
                using (BinaryReader br = new BinaryReader(ms, Encoding.BigEndianUnicode, true))
                {
                    //file header
                    byte[] header = br.ReadBytes(12);
                    Records = (uint)header[0] << 24 | (uint)header[1] << 16 | (uint)header[2] << 8 | (uint)header[3];
                    something = (uint)header[4] << 24 | (uint)header[5] << 16 | (uint)header[6] << 8 | (uint)header[7];
                    something2 = (uint)header[8] << 24 | (uint)header[9] << 16 | (uint)header[10] << 8 | (uint)header[11];

                    content = new Dictionary<uint, DataText>((int)Records);
                    
                    Trace.WriteLine("BTF header parsed");                    

                    for (int i = 0; i < Records; i++)
                    {
                        //read attributes for each string
                        var t = new DataText();
                        t.ID = ((UInt32)br.ReadByte() << 24) | ((UInt32)br.ReadByte() << 16) | ((UInt32)br.ReadByte() << 8) | (UInt32)br.ReadByte();
                        t.Location = ((UInt32)br.ReadByte() << 24) | ((UInt32)br.ReadByte() << 16) | ((UInt32)br.ReadByte() << 8) | (UInt32)br.ReadByte();
                        t.Length = (UInt16)(((UInt16)br.ReadByte() << 8) | (UInt16)br.ReadByte());
                        content[t.ID] = t;
                    }

                    Trace.WriteLine("BTF string map parsed");

                    long textStartLocation = br.BaseStream.Position;
                    foreach (var v in content.Values)                    
                    {
                        //read text of the string
                        br.BaseStream.Seek(textStartLocation + (v.Location * 2), SeekOrigin.Begin);
                        var chars = br.ReadChars(v.Length);
                        v.Text = new string(chars);
                    }
                    br.Close();

                    Trace.WriteLine("BTF string text parsed");
                }
                return true;
            } catch (Exception e)
            {
                Console.WriteLine("Error in BTF Parse: " + e.ToString());
                return false;
            }
        }

        public void WriteTo(Stream s)
        {
            //Calculation of location, length and other values
            var order = content.Values.OrderBy(x => x.Location).ToArray(); //order by original position in the file
            uint loc = 0;
            Records = (uint)order.Length;
            something = 12 + Records * 10; //Size in bytes?? 4+4+4=12B header, 4+4+2=10B each record, +bytes from string
            something2 = Records; //number of records + number of characters in all strings
            foreach (var v in order)
            {
                v.Location = loc; //set start location for the string
                v.Length = (ushort)(v.Text.Length); //set the length of the string in number of characters

                loc += (uint)(v.Length + 1); //increment location for next string, number of characters + 1 (end of string)                
                something += (uint)(v.Length + 1) * 2; //+bytest from string, number of characters + 1 (end of string), each character is 2B.
                something2 += v.Length; //increment number of characters                 
            }

            using (BinaryWriter bw = new BinaryWriter(s, Encoding.BigEndianUnicode, true))
            {
                //header
                bw.Write((byte)(Records >> 24 & 0xFF)); bw.Write((byte)(Records >> 16 & 0xFF)); bw.Write((byte)(Records >> 8 & 0xFF)); bw.Write((byte)(Records & 0xFF));
                bw.Write((byte)(something >> 24 & 0xFF)); bw.Write((byte)(something >> 16 & 0xFF)); bw.Write((byte)(something >> 8 & 0xFF)); bw.Write((byte)(something & 0xFF));
                bw.Write((byte)(something2 >> 24 & 0xFF)); bw.Write((byte)(something2 >> 16 & 0xFF)); bw.Write((byte)(something2 >> 8 & 0xFF)); bw.Write((byte)(something2 & 0xFF));
                Trace.WriteLine("BTF header written");
                //string map
                foreach (var v in order)
                {
                    bw.Write((byte)(v.ID >> 24 & 0xFF)); bw.Write((byte)(v.ID >> 16 & 0xFF)); bw.Write((byte)(v.ID >> 8 & 0xFF)); bw.Write((byte)(v.ID & 0xFF));
                    bw.Write((byte)(v.Location >> 24 & 0xFF)); bw.Write((byte)(v.Location >> 16 & 0xFF)); bw.Write((byte)(v.Location >> 8 & 0xFF)); bw.Write((byte)(v.Location & 0xFF));
                    bw.Write((byte)(v.Length >> 8 & 0xFF)); bw.Write((byte)(v.Length & 0xFF));
                }
                Trace.WriteLine("BTF string map written");
                //string text
                foreach (var v in order)
                {
                    bw.Write(v.Text.ToCharArray());
                    bw.Write('\0');
                }
                Trace.WriteLine("BTF string text written");
            }
        }

        public Dictionary<uint, string> Export()
        {
            return content.Values.OrderBy(a => a.Location).ToDictionary(a => a.ID, a => a.Text);
        }

        public uint Replaced { get; private set; }
        public uint Created { get; private set; }
        public uint Removed { get; private set; }

        public void Import(Dictionary<uint, string> data)
        {
            Replaced = 0;
            Created = 0;
            Removed = 0;
            foreach (var v in data)
            {
                if (content.ContainsKey(v.Key))
                {
                    if (!String.IsNullOrWhiteSpace(v.Value))
                    {
                        //replace
                        content[v.Key].Text = v.Value;
                        Trace.WriteLine($"String {v.Key} replaced in btf");
                        Replaced++;
                    }
                    else
                    {
                        //remove
                        content.Remove(v.Key);
                        Trace.WriteLine($"String {v.Key} removed from btf");
                        Removed++;
                    }
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(v.Value))
                    {
                        //add
                        content.Add(v.Key, new DataText() { ID = v.Key, Text = v.Value, Length = 0, Location = content.Count == 0 ? 1 : content.Max(a => a.Value.Location) + 1 });
                        Trace.WriteLine($"String {v.Key} does not exist in btf, created");
                        Created++;
                    }
                    else
                    {
                        //nothing
                        Trace.WriteLine($"String {v.Key} does not exist in btf and is empty, ignored");
                    }

                }
            }
        }

        internal class DataText
        {
            internal UInt32 ID;
            internal UInt32 Location;
            internal UInt16 Length;
            internal string Text;

            public override string ToString()
            {
                return string.Format($"{ID:X8} [{Location:X8} {Length:X4}]");
            }
        }
    }
}
