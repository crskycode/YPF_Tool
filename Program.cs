using System;
using System.IO;
using System.Text;

namespace YPF_Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length != 3)
            {
                Console.WriteLine("Yu-Ris YPF Tool");
                Console.WriteLine("Usage:");
                Console.WriteLine("  Create YPF archive : YPF_Tool -c [output.arc] [root directory path]");
                Console.WriteLine("  Extract YPF archive : YPF_Tool -e [input.ypf] [extract directory path]");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var mode = args[0];
            var outputPath = Path.GetFullPath(args[1]);
            var rootPath = Path.GetFullPath(args[2]);

            if (mode == "-c")
            {
                try
                {
                    YPF.Create(outputPath, rootPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }

            if (mode == "-e")
            {
                try
                {
                    YPF.Extract(outputPath, rootPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
