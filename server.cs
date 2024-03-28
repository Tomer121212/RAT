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
using System.Windows.Forms.VisualStyles;
using System.Net.Mail;

namespace WindowsFormsApp6
{

    public partial class Form1 : Form
    {
        private Dictionary<string, ClientConnection> clientConnections = new Dictionary<string, ClientConnection>();
        private Socket socketServer;
        private byte[] buffer = new byte[1024]; // הגדרת buffer
        private static bool HasCmdCommand = false;
        private static bool HasTaskManger = false;
        private static bool screenOn = false;
        private bool screenOff = false;
        public Form1()
        {
            InitializeComponent();
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CmdInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SendCommandToClient();
                    e.Handled = true; // זה מונע כפילויות בהפעלת האירוע
                }
            };

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

                        // ביצוע עיבוד נוסף ללקוח החדש באמצעות ProcessClientAsync
                        ProcessClientAsync(connection);
                    }
                    catch { }
                }
            });
        }

        private async Task ProcessClientAsync(ClientConnection connection)
        {
            try
            {
                string line;
                while ((line = await connection.Reader.ReadLineAsync()) != null)
                {
                    if (HasCmdCommand)
                    {
                        CmdOutput.Text += line;
                    }
                    if (HasTaskManger)
                    {
                        string name = line;
                        string path;
                        while (name != "END_OF_DATA")
                        {
                            path = connection.Reader.ReadLine();
                            listView2.Invoke((MethodInvoker)delegate
                            {
                                ListViewItem item = new ListViewItem(name);
                                item.SubItems.Add(path);
                                listView2.Items.Add(item);
                            });
                            name = connection.Reader.ReadLine();
                        }
                        HasTaskManger = false;
                    }
                    //if (screenOn)
                    //{
                    //   pictureBox1.Image = ConvertFromBase64(line);
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                DisconnectClient(connection);
            }
        }

        private void DisconnectClient(ClientConnection connection)
        {
            string ip = connection.ClientSocket.RemoteEndPoint.ToString();
            connection.Close();
            clientConnections.Remove(ip);
            dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Remove(dataGridView1.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells[0].Value != null && r.Cells[0].Value.ToString() == ip).First())));
            MessageBox.Show("לקוח יתנתק: " + ip);
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
                        HasCmdCommand = true;
                        connection.Writer.WriteLine("cmd-start");
                        connection.Writer.Flush();

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
                        HasCmdCommand = false;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("בחר לקוח מהרשימה.");
                Console.WriteLine(ex);
            }
        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != tabControl1.TabPages["cmd"]) 
            {
                HasCmdCommand = false;
                Stop_Cmd.Enabled = false;
                Start_Cmd.Enabled = true;
            }
            if (tabControl1.SelectedTab != tabControl1.TabPages["Process"])
            {
                HasTaskManger = false;
            }
            if (tabControl1.SelectedTab != tabControl1.TabPages["Screen"])
            {
                screenOff = true;
            }

        }
        public void SendCommandToClient()
        {
            if (!Start_Cmd.Enabled)
            {
                if (dataGridView1.SelectedCells.Count > 0)
                {
                    int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                    string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                    if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                    {
                        string command = CmdInput.Text;
                        if (command == "exit")
                        {
                            MessageBox.Show("אתה לא יכול לרשום פקודה זאת.");
                        }
                        else

                        {
                            HasCmdCommand = true;
                            connection.Writer.WriteLine("command " + command);
                            connection.Writer.Flush();
                            CmdInput.Text = string.Empty;

                        }

                    }

                }
            }
            else
            {
                MessageBox.Show("תפעיל את ה CMD");
                CmdInput.Text = string.Empty;

            }
        }

        private void CmdInput_TextChanged(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CmdOutput.Text = string.Empty;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (!Start_Cmd.Enabled)
            {
                if (dataGridView1.SelectedCells.Count > 0)
                {
                    int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                    string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                    if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                    {
                        DialogResult result = MessageBox.Show("Are you sure?", "Confirm Shutdown", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            connection.Writer.WriteLine("command " + "shutdown /s");
                            connection.Writer.Flush();
                        }
                    }
                }
            }
        }
        private void button16_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();

            if (dataGridView1.SelectedCells.Count > 0)
            {
                int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                {
                    HasTaskManger = true;
                    string command = "task-manager-data";
                    connection.Writer.WriteLine(command);
                    connection.Writer.Flush();


                }
            }
        }
    

        private void button14_Click_1(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                string selectedProcess = listView2.SelectedItems[0].Text;
                int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                {
                    connection.Writer.WriteLine("kill-process");
                    connection.Writer.WriteLine(selectedProcess);
                    connection.Writer.Flush();
                }
                for (int i = listView2.Items.Count - 1; i >= 0; i--)
                {
                    if (listView2.Items[i].Text == selectedProcess)
                    {   
                        listView2.Items.RemoveAt(i);
                        if (i < listView2.Items.Count)
                        {
                            listView2.Items.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            string searchText = textBox4.Text;

            foreach (ListViewItem item in listView2.Items)
            {
                if (item.Text.Contains(searchText) && searchText != "")
                {
                    item.BackColor = SystemColors.Highlight;
                    item.ForeColor = Color.White;
                }
                else
                {
                    item.BackColor = Color.White;
                    item.ForeColor = Color.Black;
                }
            }
        }
        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Cmd_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {

                if (dataGridView1.SelectedCells.Count > 0)
                {
                    int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
                    string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
                    if (clientConnections.TryGetValue(ip, out ClientConnection connection))
                    {
                        connection.Writer.WriteLine("Screen");
                        connection.Writer.Flush();
                        screenOn = true;
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("בחר לקוח מהרשימה.");
                Console.WriteLine(ex);
            }
        }
    //    public static Image ConvertFromBase64(string base64String)
    //    {
    //        byte[] imageBytes = Convert.FromBase64String(base64String);

    //        using (MemoryStream ms = new MemoryStream(imageBytes))
    //        {
    //            Image image = Image.FromStream(ms);
    //            return image;
    //        }
    //    }

    //    private void button3_Click(object sender, EventArgs e)
    //    {
    //        try
    //        {

    //            if (dataGridView1.SelectedCells.Count > 0)
    //            {
    //                int selectedRowIndex = dataGridView1.SelectedCells[0].RowIndex;
    //                string ip = dataGridView1.Rows[selectedRowIndex].Cells[0].Value.ToString();
    //                if (clientConnections.TryGetValue(ip, out ClientConnection connection))
    //                {
    //                    connection.Writer.WriteLine("Stop");
    //                    connection.Writer.Flush();
    //                    screenOn = false;
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show("בחר לקוח מהרשימה.");
    //            Console.WriteLine(ex);
    //        }
    //    }
    //}
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

}










