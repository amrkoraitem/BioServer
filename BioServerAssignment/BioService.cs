using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;
namespace BioServerAssignment
{
    public partial class BioService : ServiceBase
    {
        public static ManualResetEvent resetEvent = new ManualResetEvent(false);
        public static int intConnectionCount = 0;
        Thread _thread;
        public static void StartListening()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55555);

            // Create a TCP/IP socket.  
            Socket skListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                skListener.Bind(localEndPoint);
                skListener.Listen(100); // Srting max connections to 100

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    resetEvent.Reset();

                    // Start an asynchronous socket to listen for connections. 

                    //For Monitoring purpose
                    Console.WriteLine("Waiting for a connection...");
                    skListener.BeginAccept(new AsyncCallback(AcceptCallback), skListener);

                    // Wait until a connection is made before continuing.  
                    resetEvent.WaitOne();

                }

            }
            catch (Exception ex)
            {
                //For Monitoring purpose
                Console.WriteLine(ex.ToString());
            }            
        }

        public static void AcceptCallback(IAsyncResult aResult)
        {
            // Signal the main thread to continue.  
            resetEvent.Set();

            // Get the socket that handles the client request.  
            Socket skListener = (Socket)aResult.AsyncState;
            Socket skHandler = skListener.EndAccept(aResult);

            // Create the state object.  
            StateObject clsState = new StateObject();
            clsState.skWorkSocket = skHandler;
            skHandler.BeginReceive(clsState.buffer, 0, StateObject.intBufferSize, 0, new AsyncCallback(ReadCallback), clsState);
        }

        public static void ReadCallback(IAsyncResult aResult)
        {
            String strContent = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject clsState = (StateObject)aResult.AsyncState;
            Socket skHandler = clsState.skWorkSocket;

            // Read data from the client socket.   
            int intBytesRead = skHandler.EndReceive(aResult);

            if (intBytesRead > 0)
            {
                // In case there is more date we store the data received so far.  
                clsState.sb.Append(Encoding.ASCII.GetString(clsState.buffer, 0, intBytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                strContent = clsState.sb.ToString();
                if (strContent.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the   
                    // client. Display it on the console For Monitoring purpose.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", strContent.Length, strContent);
                   
                    // Saving Connection Count In Memory
                    intConnectionCount = intConnectionCount + 1;
                    
                    //Reading the request and reply according to the Command received 
                    switch (strContent.Replace("<EOF>", "").Trim())
                    {
                        case "HELO":
                            Send(skHandler, "HI");
                            break;
                        case "COUNT":
                            Send(skHandler, Convert.ToString(intConnectionCount));
                            break;
                        case "CONNECTIONS":
                            Send(skHandler, ShowActiveTcpConnections());
                            break;
                        case "PRIME":
                            Send(skHandler, GeneratRandomPrimeNumber());
                            break;
                        case "TERMINATE":
                            Send(skHandler, "BYE");
                            break;
                        default:
                            Send(skHandler, "Invalid Command");
                            break;

                    }
                }
                else
                {
                    // Not all data received. Get more.  
                    skHandler.BeginReceive(clsState.buffer, 0, StateObject.intBufferSize, 0, new AsyncCallback(ReadCallback), clsState);
                }
            }

        }
        public static string ShowActiveTcpConnections()
        {
            //For Monitoring purpose
            Console.WriteLine("Active TCP Connections");
            //Geting Active Connections on the server.
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            return Convert.ToString(connections.Length);

        }
        private static string GeneratRandomPrimeNumber()
        {
            //Generating Rundom Number
            Random rnd = new Random();
            int intRandomNumber = rnd.Next(2, 100);
            while (CheckIsPrimeNumber(intRandomNumber) == false) // number generated will be check if is prime,if false regenerate another random number
            {
                intRandomNumber = rnd.Next(2, 100);
            }
            return Convert.ToString(intRandomNumber);
        }
        private static bool CheckIsPrimeNumber(int intNumber)
        {

            if (intNumber == 1) return false;
            if (intNumber == 2) return true;

            if (intNumber % 2 == 0) return false; // Even number     

            for (int i = 2; i < intNumber; i++)
            { // Advance from two to include correct calculation for '4'
                if (intNumber % i == 0) return false;
            }

            return true;

        }
        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult aResult)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket skHandler = (Socket)aResult.AsyncState;

                // Complete sending the data to the remote device.  
                int intBytesSent = skHandler.EndSend(aResult);
                //For Monitoring purpose
                Console.WriteLine("Sent {0} bytes to client.", intBytesSent);

                skHandler.Shutdown(SocketShutdown.Both);
                skHandler.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }  
        public BioService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Create the thread and start it to allow service to start and not wait for a connection attemp.
            _thread = new Thread(StartListening);
            _thread.Start();
            
        }

        protected override void OnStop()
        {
            
        }
        // State object for reading client data asynchronously  
        public class StateObject
        {
            // Client  socket.  
            public Socket skWorkSocket = null;
            // Size of receive buffer.  
            public const int intBufferSize = 1024;
            // Receive buffer.  
            public byte[] buffer = new byte[intBufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();

        }  
    }
}
