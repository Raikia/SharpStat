using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Management;
using System.Threading;
using System.IO;
using System.Net.Sockets;

namespace SharpStat
{
    class Program
    {
        static void Main(string[] cargs)
        {
            Console.WriteLine();
            Console.WriteLine("         SharpStat");
            Console.WriteLine("       By @raikiasec");
            Console.WriteLine("----------------------------");
            Dictionary<string, string> args = ArgParser.Parse(cargs);
            if (args["computers"] == "")
            {
                args["computers"] = GetDomainComputers(args);
                if (args["computers"] == "")
                {
                    Console.WriteLine("Error: You must specify target computers to run this against");
                    Environment.Exit(1);
                }
            }
            if (args["file"] == "")
            {
                Console.WriteLine("Error: You must specify a file location to temporarily save the output");
                Environment.Exit(1);
            }
            else if (args["file"].IndexOf("C:\\") == -1)
            {
                Console.WriteLine("Error: File location must begin with C:\\");
                Environment.Exit(1);
            }
            else
            {
                args["file"] = args["file"].Replace("C:\\", "");
            }

            foreach (string target in args["computers"].Split(','))
            {
                Console.WriteLine("Starting on " + target + "...");
                if (PortScan(target))
                {
                    if (RunWmiNetstat(target, args["file"]))
                    {
                        Thread.Sleep(1000);
                        ReadAndDeleteResults(target, args["file"]);
                    }
                }
            }
            Console.WriteLine("--------------------");
            Console.WriteLine("SharpStat is done!");
        }


        static string GetDomainComputers(Dictionary<string, string> args)
        {
            if (args["dc"] == "" || args["domain"] == "")
            {
                Console.WriteLine("Error: Domain and/or DomainController could not be automatically identified. Try specifying with -domain and -dc");
                return "";
            }
            string ldapString = "LDAP://" + args["dc"] + "/" + BuildDN(args["domain"]);
            DirectorySearcher searcher;
            SearchResultCollection collection;
            try
            {
                searcher = new DirectorySearcher(new DirectoryEntry(ldapString));
                searcher.ServerTimeLimit = new TimeSpan(0, 20, 0);
                searcher.PageSize = 0;
                searcher.Filter = "(&(samAccountType=805306369)(dnshostname=*))";
                collection = searcher.FindAll();
                List<string> hosts = new List<string>();
                foreach (SearchResult sr in collection)
                {
                    string hostname = sr.Properties["dnshostname"][0].ToString();
                    hosts.Add(hostname);
                }
                return string.Join(",", hosts.ToArray());
            }
            catch (DirectoryServicesCOMException)
            {
                Console.WriteLine(" [ADA] Error during domain search: A referral was returned from the server");
                Console.WriteLine(" [ADA] This typically means you put in the wrong domain or domain controller.");
                Console.WriteLine(" [ADA] Try giving the full FQDN domain name instead of the netbios abbreviation");
            }
            catch (Exception)
            {
            }
            return "";
        }
        public static string BuildDN(string domain)
        {
            string[] parts = domain.Split('.');
            string returnVal = "";
            foreach (string section in parts)
            {
                if (!returnVal.Equals(""))
                {
                    returnVal += ",";
                }
                returnVal += "DC=" + section;
            }
            return returnVal;
        }

        public static bool RunWmiNetstat(string system, string file_to_save)
        {
            try
            {
                ConnectionOptions options = new ConnectionOptions();
                options.Timeout = new TimeSpan(0, 0, 3);
                ManagementScope scope = new ManagementScope("\\\\" + system + "\\root\\cimv2", options);
                scope.Connect();
                ObjectGetOptions objectGetOptions = new ObjectGetOptions();
                ManagementPath managementPath = new ManagementPath("Win32_Process");
                ManagementClass processClass = new ManagementClass(scope, managementPath, objectGetOptions);
                ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
                inParams["CommandLine"] = "cmd.exe /c netstat -n > C:\\" + file_to_save;
                ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null);
                if (outParams["returnValue"].ToString() == "0")
                {
                    return true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not connect to " + system);
            }
            return false;
        }

        public static bool ReadAndDeleteResults(string system, string file_to_grab)
        {
            System.IO.FileStream stream = System.IO.File.OpenRead("\\\\"+system+"\\C$\\"+file_to_grab);
            string contents = "";
            using (var sr = new StreamReader(stream))
            {
                contents = sr.ReadToEnd();
            }
            ParseNetstat(contents);
            System.IO.File.Delete("\\\\" + system + "\\C$\\" + file_to_grab);
            return true;
        }

        public static void ParseNetstat(string contents)
        {
            foreach (string line in contents.Split('\n'))
            {
                
                string[] parts = System.Text.RegularExpressions.Regex.Split(line, @"\s{1,}");
                if (parts.Length >= 5 && parts[2].IndexOf("[") == -1 && parts[2].IndexOf("Local") == -1)
                {
                    Console.WriteLine(parts[2] + " has " + parts[4] + " connection to " + parts[3]);
                }
            }
        }

        public static bool PortScan(string system)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(system, 445, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(new TimeSpan(0,0,3));
                    client.EndConnect(result);
                    //Console.WriteLine("Connected on 445");
                }
            }
            catch
            {
                Console.WriteLine("Port 445 closed on " + system);
                return false;
            }
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(system, 135, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(new TimeSpan(0, 0, 3));
                    client.EndConnect(result);
                    //Console.WriteLine("Connected on 135");
                }
            }
            catch
            {
                Console.WriteLine("Port 135 closed on " + system);
                return false;
            }
            return true;
        }
    }
}
