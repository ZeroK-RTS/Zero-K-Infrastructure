using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using LuaManagerLib;
using System.IO;

namespace LuaAdminCmd
{
    class Program
    {
        static void printHelp(string errorMsg)
        {
            System.Console.WriteLine("LuaAdminCmd 0.4");
            System.Console.WriteLine("===============");
            System.Console.WriteLine("Command Line: LuaAdminCmd.exe [username] [password] [command] [command-params ...]");
            System.Console.WriteLine("Command \"add\": Command-Params: [widgetName] [newVersionNumber] [*filenames]");
            System.Console.WriteLine("Command \"addmerged\": Command-Params: [newVersionNumber] [changelog] [svn-basepath] [local-basepath]");
            System.Console.WriteLine("Example: LuaAdminCmd.exe username password addmerged 2405 \"All new stuff\" /trunk/mods/ca d:\\svn\\ca\\");
            
            if (errorMsg != null)
            {
                System.Console.WriteLine("\nError: " + errorMsg);
            }
        }

        static private ArrayList getFileListFromStdIn(string svnWidgetFolder, string localWidgetFolder)
        {
            ArrayList list = new ArrayList();
            string line;
            while ((line = System.Console.In.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                if (line[0] == 'U')
                {
                    string svnPath = line.Substring(5);
                    int idx = svnPath.IndexOf(svnWidgetFolder);
                    if (idx != 0)
                    {
                        throw new Exception("SVN Base Path not found at beginning of std input line");
                    }
                    svnPath = svnPath.Substring(svnWidgetFolder.Length);

                    list.Add(/*localWidgetFolder + */svnPath);
                }
            }
            return list;
        }

        static void Main(string[] args)
        {
#if DEBUG
            //VS2008 Debugger-Bug Workaround
            Console.SetIn( new StreamReader(  "..\\..\\svninput.txt" ) );
#endif

            if (args.Length < 3)
            {
                Program.printHelp("Too few arguments!");
                return;
            }

            string name = args[0];
            string password = args[1];
            string command = args[2];

            WidgetBrowserVcs fetcher = new WidgetBrowserVcs();
            fetcher.setLoginData( name, password );
            try
            {
                fetcher.getUserId();
            }
            catch (Exception)
            {
                Program.printHelp("Login failed!");
                return;
            }

            if (command == "add")
            {
                if (args.Length < 7)
                {
                    Program.printHelp("Too few arguments for this command!");
                    return;
                }

                string widgetName = args[3];
                string version = args[4];
                string changelog = args[5];

                ArrayList files = new ArrayList();
                for (int i = 6; i < args.Length; i++)
                {
                    files.Add(args[i]);
                }

                fetcher.addLuaVersionAndFiles(decimal.Parse(Utils.commaToDot(version)), widgetName, changelog, files);
            }
            else if (command == "addmerged")
            {
                if (args.Length < 7)
                {
                    Program.printHelp("Too few arguments for this command!");
                    return;
                }

                string version = args[3];
                string changelog = args[4];
                string svnWidgetFolder = args[5];
                string localWidgetFolder = Utils.normalizePathname( args[6] );

                ArrayList files = Program.getFileListFromStdIn(svnWidgetFolder, localWidgetFolder);
                /*new ArrayList();
                for (int i = 5; i < args.Length; i++)
                {
                    files.Add(args[i]);
                }*/

                fetcher.addLuaVersionByFiles(decimal.Parse(Utils.commaToDot(version)), changelog, localWidgetFolder + svnWidgetFolder, files);
            }
            else
            {
                Program.printHelp("Unknown Command!");
                return;
            }
        }
    }
}
