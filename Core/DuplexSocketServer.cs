using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
using System.Threading.Tasks;
using Sempiler.Diagnostics;

namespace Sempiler.Core
{
    // State object for reading client data asynchronously  
    public class SocketRequestState {  
        // Client  socket.  
        public Socket workSocket = null;  
        // Size of receive buffer.  
        public const int BufferSize = 1024;  
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];  
    // Received data string.  
        public StringBuilder sb = new StringBuilder();    
    }  
    
    public class DuplexSocketServer {  

        public const string MessageSentinel = "<EOF>";

        public delegate void OnMessageDelegate(Socket handler, string message);

        public event OnMessageDelegate OnMessage;

        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);  
    
        public DuplexSocketServer() { 
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());  

            foreach(var address in ipHostInfo.AddressList)
            {
                if(address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IPAddress = address;
                    break;
                }
            }

            System.Diagnostics.Debug.Assert(IPAddress != default(IPAddress));
        }  
    
        public readonly IPAddress IPAddress;
        public int Port { get; private set; }

        private CancellationTokenSource cancellationTokenSource;

        Socket listener;

        Task<Result<object>> serverTask;
        Result<object> serverResult;

        public Result<object> BindPort(int port)
        {
            var result = new Result<object>();
            
            // Establish the local endpoint for the socket.  
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress, port);  
    
            // Create a TCP/IP socket.  
            listener = new Socket(IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);  
            try
            {
                // listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                listener.Bind(localEndPoint);  
                
                listener.Listen(100); 

                Port = port;
            }
            catch(Exception e)
            {
                result.AddMessages(DiagnosticsHelpers.CreateErrorFromException(e));
            }

            return result;
        }

        private void OnSocketError(Exception e, Socket handler)
        {
            try
            {
                handler.Shutdown(SocketShutdown.Both);  
                handler.Close();    
            }
            catch{}

            serverResult.AddMessages(
                DiagnosticsHelpers.CreateErrorFromException(e)
            );

            Stop();
        }

        public Task<Result<object>> StartAcceptingRequests(CancellationToken token) {  
            
            return serverTask = Task.Run(() => {

                serverResult = new Result<object>();

                cancellationTokenSource = new CancellationTokenSource();

                using (CancellationTokenRegistration ctr = token.Register(Stop))
                {
                    Listen(cancellationTokenSource.Token);
                }

                return serverResult;

            }, token);
        }  

        private void Listen(CancellationToken token) {  
            using (CancellationTokenRegistration ctr = token.Register(() => allDone.Set()))
            {
                while (!token.IsCancellationRequested) {  
                    // Set the event to nonsignaled state.  
                    allDone.Reset();  
    
                    if(token.IsCancellationRequested) break;

                    // Start an asynchronous socket to listen for connections.  
                    // Console.WriteLine("Waiting for a connection...");  
                    listener.BeginAccept(   
                        new AsyncCallback(AcceptCallback),  
                        listener);  
    
                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();  
                }
            }
        } 

        public void Stop()
        {
            try
            {
                listener.Shutdown(SocketShutdown.Both);  
                listener.Close();    
            }
            catch
            {}
            
            cancellationTokenSource.Cancel();

            serverTask.Wait();

            cancellationTokenSource.Dispose();
        }

    
        public void AcceptCallback(IAsyncResult ar) {  
            // Signal the main thread to continue.  
            allDone.Set();  
    
            // Get the socket that handles the client request.  
            Socket listener = (Socket) ar.AsyncState;  
            // Asynchronously accepts an incoming connection attempt.
            Socket handler = listener.EndAccept(ar);  

            // Create the state object.  
            SocketRequestState state = new SocketRequestState();  
            state.workSocket = handler;  

            // Begins to asynchronously receive data from a connected Socket.
            handler.BeginReceive( state.buffer, 0, SocketRequestState.BufferSize, 0,  
                new AsyncCallback(ReadCallback), state);  
        }  
    
        public void ReadCallback(IAsyncResult ar) {  
            String content = String.Empty;  
    
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            SocketRequestState state = (SocketRequestState) ar.AsyncState;  
            Socket handler = state.workSocket;  
    
            int bytesRead = 0;
            try
            {
                // Read data from the client socket.   
                bytesRead = handler.EndReceive(ar);  
            }
            catch(SocketException e)
            {
                // [dho] eg the CT program did it's work and closed the socket - 20/04/19
                OnSocketError(e, handler);
            }
    
            if (bytesRead > 0) {  
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(  
                    state.buffer, 0, bytesRead));  
    
                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = state.sb.ToString(); 

                var messageSentinelIndex = content.IndexOf(MessageSentinel);

                if (messageSentinelIndex > -1) {  
                    var message = content.Substring(0, messageSentinelIndex);

                    // All the data has been read from the   
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket.\nMessage : {1}\n", content.Length, message);  
                    
                    OnMessage(handler, message);  

                    // [dho] reset for the next message - 20/04/19
                    state.sb = new StringBuilder();
                } 
                // else 
                // {  
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, SocketRequestState.BufferSize, 0,  
                    new AsyncCallback(ReadCallback), state);  
                // }  
            }  
        }  

        public void Send(Socket handler, String message = null) {
            message = message ?? "ACK";  
            Console.WriteLine("Sending Message : {0}", message);

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(message + MessageSentinel);  

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,  
                new AsyncCallback(SendCallback), handler);  
        }  
    
        private void SendCallback(IAsyncResult ar) {  
            // Retrieve the socket from the state object.  
            Socket handler = (Socket) ar.AsyncState; 

            try {  
                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);  

                Console.WriteLine("Sent {0} bytes to client.", bytesSent); 
            } 
            catch (Exception e) 
            {  
                OnSocketError(e, handler);
            }  
        }  
    }  
}