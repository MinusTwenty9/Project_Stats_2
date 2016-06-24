using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Project_Stats_2
{
    public class Csproj
    {
        public List<string> references;
        public List<string> classes;
        public string path;
        public string output_path;
        public string asm_name;

        public string proj_name;

        public Csproj(string path)
        {
            if (!File.Exists(path)) return;

            proj_name = Path.GetFileName(path).Replace(".csproj","");

            this.path = path;
            this.references = new List<string>();
            this.classes = new List<string>();
            load();
        }

        private void load()
        {
            string dir_path = Path.GetDirectoryName(path) + "\\";
            using (FileStream stream = new FileStream(path, FileMode.Open,FileAccess.Read))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(stream);

                #region References
                XmlNodeList refs = xml.GetElementsByTagName("Reference");
                for (int i = 0; i < refs.Count; i++)
                {
                    string dll;
                    // Local Dll (HintPath)
                    if (refs[i].HasChildNodes)
                    {
                        // Ch

                        bool hint_path = false;
                        foreach (XmlNode node in refs[i].ChildNodes)
                            if (node.Name == "HintPath")
                            {
                                dll = Path.GetFullPath(dir_path + refs[i].InnerText);
                                if (!File.Exists(dll))
                                    break;
                                references.Add(dll);
                                hint_path = true;
                                break;
                            }
                        if (hint_path) continue;
                    }

                    // Normal Standart Dll
                    dll = refs[i].Attributes["Include"].Value;
                    if (dll.Split(',').Length != 1)
                        dll = dll.Split(',')[0];
                    references.Add(dll+".dll");
                }
                #endregion

                #region Classes
                XmlNodeList cls = xml.GetElementsByTagName("Compile");
                for (int i = 0; i < cls.Count; i++)
                {
                   
                    // Normal Standart Class
                    classes.Add(dir_path +cls[i].Attributes["Include"].Value);
                }
                #endregion

                #region GeneralInfo
                XmlNodeList pg = xml.GetElementsByTagName("PropertyGroup");
                foreach (XmlNode n in pg)
                {
                    // Check for Debug Property
                    if (n.Attributes.Count == 0) continue;
                    if (n.Attributes["Condition"].Value.Split(new string[] { "Debug" }, StringSplitOptions.None).Length != 2)
                        continue;

                    foreach (XmlNode c in n.ChildNodes)
                        if (c.Name == "OutputPath")
                        {
                            output_path = dir_path + c.InnerText;
                            break;
                        }
                }

                asm_name = xml.GetElementsByTagName("AssemblyName")[0].InnerText;
                #endregion
            }

        }

        public string[] Get_Code()
        {
            List<string> code = new List<string>();
            byte[] buffer;

            for (int i = 0; i < classes.Count; i++)
            {
                using (FileStream stream = new FileStream(classes[i], FileMode.Open, FileAccess.Read))
                {
                    buffer = new byte[(int)stream.Length];
                    stream.Read(buffer,0,buffer.Length);

                    code.Add( Encoding.UTF8.GetString(buffer));

                    stream.Close();
                    stream.Dispose();
                }
            }

            return code.ToArray();
        }

        // lines, avg_line_length, chars (no space)
        public GeneralClass[] Get_Class_Lines()
        {
            List<GeneralClass> c = new List<GeneralClass>();
            string dir_path = Path.GetDirectoryName(path) + "\\";

            foreach (string s in classes)
            {
                GeneralClass g = new GeneralClass();
                
                using (StreamReader reader = new StreamReader(s))
                { 
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        g.lines++;
                        g.avg_line_length += line.Length;
                        g.chars += line.Replace(" ","").Length;
                    }
                    reader.Close();
                }

                g.name = s;
                g.avg_line_length /= g.lines;
                c.Add(g);
            }

            return c.ToArray();
        }
    }

    public struct GeneralClass
    {
        public string name;
        public int lines;
        public double avg_line_length;
        public int chars;
    }
}
