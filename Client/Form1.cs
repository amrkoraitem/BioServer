using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Client
{
    public partial class frmMain : Form
    {
        // The port number for the remote device.  
        public const int intPort = 55555;
        private bool blnHandshake = false;
        private static bool blnConectionStatus = false;
        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent mreConnectDone = new ManualResetEvent(false);
        private static ManualResetEvent mreSendDone = new ManualResetEvent(false);
        private static ManualResetEvent mreReceiveDone = new ManualResetEvent(false);

        // The response from the remote device.  
        private static String strResponse = String.Empty;
        public  void StartClient()
        {
            // Connect to a remote device.  
            try
            {
                mreSendDone.Reset();
                mreReceiveDone.Reset();
                mreConnectDone.Reset();
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), intPort);
                
                // Create a TCP/IP socket.  
                Socket skClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                skClient.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), skClient);

                mreConnectDone.WaitOne(5000, true); //TimeOut 5 Sec
                if (blnConectionStatus == true)
                {
                    // Send  data to the remote device.  
                    Send(skClient, txtDataToSend.Text + "<EOF>");
                    mreSendDone.WaitOne();

                    // Receive the response from the remote device.  
                    Receive(skClient);
                    mreReceiveDone.WaitOne(5000, true); //TimeOut 5 Sec

                    // Show the response.  
                    txtConnectionStream.Text = txtConnectionStream.Text + strResponse + Environment.NewLine;

                    // Release the socket.  
                    skClient.Shutdown(SocketShutdown.Both);
                    skClient.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult arResult)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket skClient = (Socket)arResult.AsyncState;

                // Complete the connection.  
                skClient.EndConnect(arResult);
                //For Monitoring purpose
                Console.WriteLine("Socket connected to {0}", skClient.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                mreConnectDone.Set();
                blnConectionStatus = true;
            }
            catch (Exception e)
            {                                
                MessageBox.Show(e.Message);
                blnConectionStatus = false;     //Connection Faild
                mreConnectDone.Set();
            }
        }

        private static void Receive(Socket skClient)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = skClient;

                // Begin receiving the data from the remote device.  
                skClient.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                //For Monitoring purpose
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult aResult)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)aResult.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(aResult);

                if (bytesRead > 0)
                {
                    // In case there is more date we store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length >= 1)
                    {
                        strResponse = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    mreReceiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                //For Monitoring purpose
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult aResult)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)aResult.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(aResult);
                //For Monitoring purpose
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                mreSendDone.Set();
            }
            catch (Exception ex)
            {
                //For Monitoring purpose
                Console.WriteLine(ex.ToString());
            }
        }  
        public frmMain()
        {
            InitializeComponent();
        }
        // State object for receiving data from remote device.  
        public class StateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 256;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }  
        private void frmMain_Load(object sender, EventArgs e)
        {
           
            
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            // check for correct condition to start handshaking
            if (txtDataToSend.Text.Trim() == "HELO")
            {
                blnHandshake = true;
            }
           
            if (!string.IsNullOrEmpty(txtDataToSend.Text) && blnHandshake == true)
            {
                StartClient();
                if (txtDataToSend.Text.Trim() == "TERMINATE")
                {
                    blnHandshake = false;
                }                
            }
        }
    }
}
