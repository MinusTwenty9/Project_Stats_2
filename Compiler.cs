using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;

namespace Project_Stats_2
{
    public static class Compiler
    {
        public static Assembly Load_Code(string[] code, List<string> references)
        {
            Assembly asm = compile_code(code,references);

            if (asm == null)
            {
                Console.WriteLine("err: compile_code");
                return null;
            }
            return asm;
            //Type[] classes = asm.GetExportedTypes();

            //for (int i = 0; i < classes.Length; i++)
            //{
            //    Console.WriteLine(classes[i].Name);
            //    Console.WriteLine((classes[i].IsClass ? "IsClass":"NoClass"));
                
            //    FieldInfo[] fi = classes[i].GetFields( BindingFlags.NonPublic | BindingFlags.Static| BindingFlags.Instance);
            //    for (int y = 0; y < fi.Length;y++ )
            //        Console.WriteLine(fi[y].Name);

            //        Console.WriteLine();
            //}
        }

        private static Assembly compile_code(string[] code, List<string> references)
        {
            string code_dom_lang = "CSharp";

            CodeDomProvider code_dom_provider = CodeDomProvider.CreateProvider(code_dom_lang);

            System.CodeDom.Compiler.CompilerParameters compiler_parameters = new System.CodeDom.Compiler.CompilerParameters();

            for (int i = 0; i < references.Count; i++)
                compiler_parameters.ReferencedAssemblies.Add(references[i]);

            compiler_parameters.CompilerOptions = "/t:library";
            compiler_parameters.GenerateInMemory = true;

            System.CodeDom.Compiler.CompilerResults compiler_rules = code_dom_provider.CompileAssemblyFromSource(compiler_parameters, code);

            //Auf CompilerFehler prüfen
            if (compiler_rules.Errors.Count > 0)
            {
                for (int i = 0; i < compiler_rules.Errors.Count; i++)
                    Console.WriteLine(compiler_rules.Errors[i].ErrorText, "\n\n");
                return null;
            }

            return compiler_rules.CompiledAssembly;
        }
    }
}
