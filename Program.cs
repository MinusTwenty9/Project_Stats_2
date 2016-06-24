using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Project_Stats_2
{
    class Program
    {
        static List<Assembly> asm_dll;

        static void Main(string[] args)
        {
            string path;
            Console.Write(".Csproj-File: ");
            if (args.Length == 0) path = Console.ReadLine().Replace("\"", "");
            else path = args[0];

            ProjectStats ps = new ProjectStats(path);

            DateTime dt = DateTime.Now;
            string name = ps.cp.proj_name+"_"+dt.Year + "_"+dt.Month+"_"+dt.Day+"_-_"+dt.Hour+"_"+dt.Minute+"_"+dt.Second;
            string s_path = Path.GetDirectoryName(ps.path) + "/" + name;
            //File.WriteAllText(name+".json", ps.To_JSON());
            File.WriteAllText(s_path+ ".html", ps.To_HTML());

            if (args.Length >= 0) return;
            Console.ReadLine();
            Main(new string[0]);
        }
    }
}
