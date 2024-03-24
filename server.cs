using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.IO;

namespace WindowsFormsApp6
{
    public class ClientConnection
    {
        public Socket ClientSocket { get; private set; }
        public NetworkStream Stream { get; private set; }
        public StreamWriter Writer { get; private set; }
        public StreamReader Reader { get; private set; }

        public ClientConnection(Socket clientSocket)
        {
            ClientSocket = clientSocket;
            Stream = new NetworkStream(clientSocket);
            Writer = new StreamWriter(Stream);
            Reader = new StreamReader(Stream);
        }

        public void Close()
        {
            Writer.Close();
            Reader.Close();
            Stream.Close();
            ClientSocket.Close();
        }
    }

    public partial class Form1 : Form
    {
        private Dictionary<string, ClientConnection> clientConnections = new Dictionary<string, ClientConnection>();
        private Socket socketServer;
        private byte[] buffer = new byte[1024]; // הגדרת buffer

        public Form1()
        {
            InitializeComponent();
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private void ListenButton_Click(object sender, EventArgs e)
        {
            ListenButton.Enabled = false;
            ListenButton.Text = "Listening...";

            string serverIP = "10.0.0.33"; // Replace with your server's IP address
            int serverPort = 4444;

            // יצירת נקודת הקצה
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            socketServer.Bind(ipEndPoint);
            socketServer.Listen(10); // רואים עד 10 מחברים כאשר המספר הינו עד 10

            Console.WriteLine("Listener began on " + ipEndPoint);

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        // מקבלים את הלקוח החדש
                        Socket client = socketServer.Accept();
                        string ip = client.RemoteEndPoint.ToString();
                        ClientConnection connection = new ClientConnection(client);
                        clientConnections[ip] = connection;

                        // Add the client's IP to the DataGridView
                        dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(ip)));

                        // ביצוע עיבוד נוסף ללקוח החדש באמצעות אירועים
                        ProcessClient(connection);
                    }
                    catch { }
                }
            });
        }

        private void ProcessClient(ClientConnection connection)
        {
            try
            {
                // קבלת נתונים מהלקוח
                connection.ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine("לקוח יתנתק: " + ex.Message);
            }
        }

        private void OnReceive(IAsyncResult result)
        {
            ClientConnection connection = (ClientConnection)result.AsyncState;
            try
            {
                int bytesRead = connection.ClientSocket.EndReceive(result);
                if (bytesRead > 0)
                {
                    // קבלת נתונים והמשך קבלה
                    connection.ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), connection);
                }
                else
                {
                    // הלקוח מנתק את עצמו
                    string ip = connection.ClientSocket.RemoteEndPoint.ToString();
                    connection.Close();
                    clientConnections.Remove(ip);
                    dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Remove(dataGridView1.Rows.Cast<DataGridViewRow>()
                        .Where(r => r.Cells[0].Value != null && r.Cells[0].Value.ToString() == ip).First())));
                    MessageBox.Show("לקוח יתנתק: " + ip);
                }
            }
            catch (SocketException ex)
            {
                // הלקוח מנתק את עצמו
                string ip = connection.ClientSocket.RemoteEndPoint.ToString();
                connection.Close();
                clientConnections.Remove(ip);
                dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Remove(dataGridView1.Rows.Cast<DataGridViewRow>()
                    .Where(r => r.Cells[0].Value != null && r.Cells[0].Value.ToString() == ip).First())));
                MessageBox.Show("לקוח יתנתק: " + ip);
            }
        }


        private void CmdOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private void Start_Cmd_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedCells.Count > 0)
                {
                    int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                    string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                    Stop_Cmd.Enabled = true;
                    Start_Cmd.Enabled = false;
                    if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                    {
                        connection.Writer.WriteLine("cmd-start");
                        connection.Writer.Flush();
                        MessageBox.Show(connection.Reader.ReadToEnd());
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("בחר לקוח מהרשימה.");
                Console.WriteLine("Error: " + ex.Message);
            }
        }


        private void Stop_Cmd_Click(object sender, EventArgs e)
        {
            try
            {

                if (dataGridView1.SelectedCells.Count > 0)
                {
                    int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                    string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                    Stop_Cmd.Enabled = false;
                    Start_Cmd.Enabled = true;
                    if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                    {
                        connection.Writer.WriteLine("cmd-stop");
                        connection.Writer.Flush();


                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("בחר לקוח מהרשימה.");
            }
        }

    }
}






