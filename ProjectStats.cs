using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace Project_Stats_2
{
    public class ProjectStats
    {
        public string path;
        public Assembly asm;
        public Csproj cp;

        // Methods
        public int m_count;
        public int m_static;
        public int m_public;
        public int m_private;
        public int m_params;
        public Dictionary<string, int> m_types = new Dictionary<string, int>();

        // Global Variables
        public int gv_count;
        public int gv_static;
        public int gv_public;
        public int gv_private;
        public Dictionary<string, int> gv_types = new Dictionary<string, int>();

        // Local Variables
        public int lv_count;
        public Dictionary<string,int> lv_types = new Dictionary<string,int>();

        // Classes
        public int c_count;
        public int c_static;
        public int c_public;
        public int c_private;
        public int c_abstract;

        // General
        public string g_max_name;
        public int g_max;
        public double g_pages;
        public int g_lines;
        public double g_avg_lines;
        public double g_avg_line_length;
        public int g_chars;

        private List<Assembly> asm_dll;
        public string asm_name;

        public ProjectStats(string path)
        {
            this.path = path;
            load_asm();
            create_project_stats();
        }

        private void load_asm()
        {
            cp = new Csproj(path);

            if (File.Exists(cp.output_path+"/"+cp.asm_name + ".exe")) asm_name = cp.asm_name + ".exe";
            else if (File.Exists(cp.output_path+"/" + cp.asm_name + ".dll")) asm_name = cp.asm_name + ".dll";
            else return;

            asm = Assembly.LoadFile(cp.output_path + "/" + asm_name);
            asm_dll = new List<Assembly>();

            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(cp.output_path));
            foreach (FileInfo fi in di.GetFiles())
                if (fi.Name.Split(new string[] { ".dll" }, StringSplitOptions.None).Length == 2)
                    asm_dll.Add(Assembly.UnsafeLoadFrom(fi.FullName));

            AppDomain ad = AppDomain.CurrentDomain;
            ad.AssemblyResolve += ((object sender, ResolveEventArgs args) => {
                Assembly _asm = null;
                string name;
                for (int i = 0; i < asm_dll.Count; i++)
                    if ((name = asm_dll[i].GetName().FullName) == args.Name)
                    {
                        _asm = asm_dll[i];
                        break;
                    }
                return _asm;
            });
            
        }

        private void create_project_stats()
        {
            Type[] types = asm.GetTypes();

            foreach (Type t in types)
            {
                if (t.IsEnum) continue;
                if (!t.IsClass) continue;

                // Classes
                c_count++;
                c_static += (t.IsAbstract && t.IsSealed ? 1 : 0);
                c_public += (t.IsPublic ? 1 : 0);
                c_private += (t.IsNotPublic ? 1 : 0);
                c_abstract += (t.IsAbstract && !t.IsSealed ? 1 : 0);

                BindingFlags[] binding_flags = new BindingFlags[] { 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
                    //BindingFlags.Instance | BindingFlags.NonPublic| BindingFlags.DeclaredOnly,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static| BindingFlags.DeclaredOnly,
                    // BindingFlags.Instance | BindingFlags.Public| BindingFlags.DeclaredOnly
                };

                // Methods
                for (int i = 0; i < binding_flags.Length; i++)
                {
                    MethodInfo[] mi = t.GetMethods(binding_flags[i]);
                    local_variables(mi);
                }

                // Global Variables
                for (int i = 0; i < binding_flags.Length; i++)
                {
                    FieldInfo[] fi = t.GetFields(binding_flags[i]);
                    gv_count += fi.Length;

                    foreach (FieldInfo f in fi)
                    {
                        string f_type = f.FieldType.Name;
                        if (gv_types.Keys.Any(k => k == f_type))
                            gv_types[f_type]++;
                        else gv_types.Add(f_type, 1);

                        gv_static += (f.IsStatic ? 1 : 0);
                        gv_private += (f.IsPrivate ? 1 : 0);
                        gv_public += (f.IsPublic ? 1 : 0);

                    }
                }
            }
            general_classes();
        }

        private void local_variables(MethodInfo[] mi)
        {
            foreach (MethodInfo m in mi)
            {
                if (m.IsFamily ||m.IsVirtual || m.IsAbstract) continue;

                m_count ++;

                string m_type = m.ReturnType.Name; 
                if (m_types.Keys.Any(k => k == m_type))
                    m_types[m_type]++;
                else m_types.Add(m_type, 1);

                m_static += (m.IsStatic ? 1 : 0);
                m_public += (m.IsPublic ? 1 : 0);
                m_private += (m.IsPrivate ? 1 : 0);

                m_params += m.GetParameters().Length;
                try
                {
                    MethodBody mb = m.GetMethodBody();
                    if (mb != null)
                    {
                        lv_count += mb.LocalVariables.Count;
                        foreach (LocalVariableInfo lvi in mb.LocalVariables)
                            if (lv_types.Keys.Any(k => k == lvi.LocalType.Name))
                                lv_types[lvi.LocalType.Name]++;
                            else lv_types.Add(lvi.LocalType.Name, 1);
                    }
                    else Console.WriteLine("MethodBody nulll");
                }
                catch
                { Console.WriteLine("MethodBody Assembly err"); }
            }
        }

        private void general_classes()
        {
            GeneralClass[] gc = cp.Get_Class_Lines();

            foreach (GeneralClass g in gc)
            {
                g_chars += g.chars;
                g_avg_lines += g.lines;
                g_avg_line_length += g.avg_line_length;
                g_lines += g.lines;

                if (g.lines > g_max)
                {
                    g_max = g.lines;
                    g_max_name = g.name;
                }
            }

            g_avg_lines /= gc.Length;
            g_avg_line_length /= gc.Length;
            g_pages = g_chars / 1475.0;

            string[] max_name_split = g_max_name.Replace("\\", "/").Split('/');
            g_max_name = max_name_split[max_name_split.Length - 1];
        }

        public string To_JSON()
        {
            StringBuilder s = new StringBuilder();

            // Name
            s.Append("{\"ProjectName\":\"" + cp.proj_name + "\",");
            s.Append("\"Date\":\"" + DateTime.Now.ToString() + "\",");


            // General
            s.Append("\"General\":{");
            s.Append("\"Max Lines (" + g_max_name + ")\":" + g_max + ",");
            s.Append("\"Lines\":" + g_lines + ",");
            s.Append("\"Avg. Lines\":" + g_avg_lines.ToString("G1").Replace(",",".") + ",");
            s.Append("\"Avg. Line Length\":" + g_avg_line_length.ToString("G1").Replace(",", ".") + ",");
            s.Append("\"Total Chars\":" + g_chars + ",");
            s.Append("\"Book Pages\":" + g_pages.ToString("G1").Replace(",", "."));
            s.Append("},");

            // Methods
            s.Append("\"Methods\":{");
            s.Append("\"Count\":" + m_count + ",");
            s.Append("\"Public\":" + m_public + ",");
            s.Append("\"Private\":" + m_private + ",");
            s.Append("\"Static\":" + m_static + ",");
            s.Append("\"Params\":" + m_params + ",");
            s.Append("\"Types\":{");

            foreach (string key in m_types.Keys)
                s.Append("\"" + key + "\":"+m_types[key]+",");
            if (m_types.Keys.Count > 0) s.Length--;
            s.Append("}},");


            // Global Variables
            s.Append("\"GlobalVariables\":{");
            s.Append("\"Count\":" + gv_count + ",");
            s.Append("\"Public\":" + gv_public + ",");
            s.Append("\"Private\":" + gv_private + ",");
            s.Append("\"Static\":" + gv_static + ",");
            s.Append("\"Types\":{");

            foreach (string key in gv_types.Keys)
                s.Append("\"" + key + "\":" + gv_types[key] + ",");
            if (gv_types.Keys.Count > 0) s.Length--;
            s.Append("}},");


            // Local Variables
            s.Append("\"LocalVariables\":{");
            s.Append("\"Count\":" + lv_count + ",");
            s.Append("\"Types\":{");

            foreach (string key in lv_types.Keys)
                s.Append("\"" + key + "\":" + lv_types[key] + ",");
            if (lv_types.Keys.Count > 0) s.Length--;
            s.Append("}},");


            // Global Variables
            s.Append("\"Classes\":{");
            s.Append("\"Count\":" + c_count + ",");
            s.Append("\"Public\":" + c_public + ",");
            s.Append("\"Private\":" + c_private + ",");
            s.Append("\"Static\":" + c_static + ",");
            s.Append("\"Abstract\":" + c_abstract);
            s.Append("}}");

            return s.ToString();
        }

        public string To_HTML()
        {
            string json = To_JSON();
            string html = "";

            //using (StreamReader reader = new StreamReader("./project_stats.html"))
            //    html = reader.ReadToEnd();
            html = Project_Stats_2.Properties.Resources.project_stats;

            html = html.Replace("'[JSON]'", "'" + json + "'");
            html = html.Replace("'[TITLE]'", "'" + cp.proj_name + "'");
            return html;
        }
    }
}
