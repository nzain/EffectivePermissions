using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace EffectivePermissions
{
    /// <summary>
    /// A console app to compute effective permissions on the folder it is executed in, recursing down over all files.
    /// To understand how effective permissions are computed refer to the <see cref="AccessRights"/> class.
    /// </summary>
    class Program
    {
        private static StreamWriter Writer { get; set; }

        static void Main()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (identity == null)
            {
                ExitMessage("WindowsIdentity.GetCurrent() returned null? Unable to identify current user!");
                return;
            }
            WindowsPrincipal user = new WindowsPrincipal(identity);

            // TODO use args here, e.g. single file, path, recursive, etc.
            string path = Directory.GetCurrentDirectory();

            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Effective Permissions.log");

            string userMsg = $"User     : {identity.Name}";
            string pathMsg = $"Directory: {path}";
            string logfMsg = $"Log File : {output}";
            Console.WriteLine(userMsg);
            Console.WriteLine(pathMsg);
            Console.WriteLine(logfMsg);
            if (File.Exists(output))
            {
                Console.WriteLine();
                Console.Write("Log file already exists. Overwrite? [y,n] ");
                var c = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine();
                if (c.KeyChar != 'y')
                {
                    return;
                }
            }
            Console.WriteLine(new string('-', 80));
            using (Writer = new StreamWriter(output) { AutoFlush = true })
            {
                Writer.WriteLine(userMsg);
                Writer.WriteLine(pathMsg);
                Writer.WriteLine(logfMsg);
                Writer.WriteLine(new string('-', 80));

                try
                {
                    AccessRights ar = new AccessRights(path, user);
                    WriteLine(ar.ToString());
                    foreach (string subdir in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        ar = new AccessRights(subdir, user);
                        WriteLine(ar.ToString());
                    }
                }
                catch (Exception e)
                {
                    Writer.WriteLine(e);
                    ExitMessage(e.ToString());
                }
                Console.WriteLine(new string('-', 80));
                ExitMessage($"Output written to: {output}");
            }
        }

        static void WriteLine(string msg)
        {
            Console.WriteLine(msg);
            Writer.WriteLine(msg);
        }

        static void ExitMessage(string msg)
        {
            Console.WriteLine(msg);
            Console.WriteLine();
            Console.WriteLine("-- press any key to quit --");
            Console.ReadKey();
        }
    }
}
