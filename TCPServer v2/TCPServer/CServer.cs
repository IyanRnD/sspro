using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.Text.RegularExpressions;
using System.Linq;
using MySql.Data.MySqlClient.Memcached;
using System.Runtime.InteropServices.ComTypes;

static class SocketExtensions{
    public static bool IsConnected(this Socket socket){
        try{
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch (SocketException) { return false; }
    }
}

class OnStringReceivedEventArgs : EventArgs {
    public string received;
    public string message;
    public TcpClient AClient;
}
class OnLogAddEventArgs : EventArgs{    
    public string message;
}


class Server {

    TcpListener server = null;
    TextBox tbLog;
    Boolean AllowDuplicateClientIP;

    public Boolean IsRunning = false;
    public Boolean IsByteDisplay = false;
    

    private static List<TcpClient> listConnectedClients = new List<TcpClient>();

    public delegate void OnStringReceivedHandler(object sender, OnStringReceivedEventArgs data);
    public event OnStringReceivedHandler OnStringReceived;


    public delegate void OnLogAddHandler(object sender, OnLogAddEventArgs data);
    public event OnLogAddHandler OnLogAdd;


    private void LogAdd(string Text) {

        if (OnLogAdd != null)
        {
            OnLogAddEventArgs Args = new OnLogAddEventArgs();
            Args.message = "\r\n[Server] " + Text;
            //raise event
            OnLogAdd(this, Args);
        }
        /*
        if (tbLog != null) {
            if (tbLog.InvokeRequired) {
                tbLog.Invoke((MethodInvoker)delegate {
                    tbLog.AppendText("\r\n[Server] "+Text );
                });
            }
            else {
                tbLog.AppendText("\r\n[Server] " + Text );
            }
        }*/

    }



    public void SetTextBox(ref TextBox ATextBox) {
        tbLog = ATextBox;
    }

    public void Start() {
        server.Start();
        IsRunning = true;
        StartListener();
    }


    public Server(string ip, int port, ref TextBox ATextBox){
        tbLog = ATextBox;
        AllowDuplicateClientIP = false;
        IPAddress localAddr = IPAddress.Parse(ip);
        server = new TcpListener(localAddr, port);

    }
    public Server(string ip, int port, Boolean AllowSameClientIP, ref TextBox ATextBox) {
        AllowDuplicateClientIP = AllowSameClientIP;
        tbLog = ATextBox;
        IPAddress localAddr = IPAddress.Parse(ip);
        if (ip.Length > 0)
            server = new TcpListener(localAddr, port);
            else {
            server = new TcpListener(port);
        }
        
        //server = new TcpListener(localAddr, port);
        //server.Start();
        //IsRunning = true;
        //StartListener();
    }

    public void StartListener(){
        LogAdd("\r\nWaiting for a connection...");

        try{
            while (IsRunning){
                TcpClient client = server.AcceptTcpClient();
                string aIP = ""+IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

                Boolean IsDuplicate = false;
                TcpClient DClient = null;

                //check if no duplicate IP conected
                /*
                foreach (TcpClient AClient in listConnectedClients) {
                    string strIP = ""+IPAddress.Parse(((IPEndPoint)AClient.Client.RemoteEndPoint).Address.ToString());
                    if (strIP == aIP) {

                        IsDuplicate = true;
                        DClient = AClient;
                    }                    
                }

                //Duplicate IP Client Not Allowed
                if (AllowDuplicateClientIP == false) {
                    LogAdd(aIP + "Forced Close Duplicate IP Found");
                    if (IsDuplicate) {
                        if (DClient != null)
                            DClient.Close();
                        listConnectedClients.Remove(DClient);
                    }
                }*/
                listConnectedClients.Add(client);

                Thread t = new Thread(new ParameterizedThreadStart(HandleDevice));
                t.Start(client);
            }
        }
        catch (SocketException e){
            LogAdd(string.Format("SocketException: {0}", e));
            server.Stop();
        }
    }

    static string ByteToHexBitFiddle(byte[] bytes, int len) {
        char[] c = new char[len * 2];
        int b;
        for (int i = 0; i < len; i++) {
            b = bytes[i] >> 4;
            c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
            b = bytes[i] & 0xF;
            c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
        }
        return new string(c);
    }
    public static bool isValidASCII(IEnumerable<byte> source) {
        if (null == source)
            return true; // or false, or throw exception

        return source.All(b => b >= 32 && b <= 127);
    }


    public void HandleDevice(Object obj){

        TcpClient client = (TcpClient)obj;
        var stream = client.GetStream();
        string imei = String.Empty;
        string data = null;
        string strData = null;

        Byte[] bytes = new Byte[2048];
        int i;
        int len = 0;
        Stopwatch TimPacket = new Stopwatch();


        int ThreadCount = Process.GetCurrentProcess().Threads.Count;
        string clientIPAddress = ""+IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
        //string IPHeader = string.Format("Th:{1}/{2} [{0}] ", clientIPAddress, Thread.CurrentThread.ManagedThreadId, ThreadCount);
        string IPHeader = string.Format("{1}/{2}:[{0}] ", clientIPAddress, Thread.CurrentThread.ManagedThreadId, ThreadCount);

        LogAdd(IPHeader+" Connected");

        try {
            
            while (!((client.Available == 0) && (client.Client.Poll(100000, SelectMode.SelectRead)))){
                try {
                    len = client.Available;
                    string IP = clientIPAddress;
                    while ((i = stream.Read(bytes, 0, len)) != 0){
                        TimPacket.Reset();
                        TimPacket.Start();

                        string hex = BitConverter.ToString(bytes);
                        //mix display both ascii and non ascii
                        data = ByteToHexBitFiddle(bytes,len);
                        strData = Encoding.ASCII.GetString(bytes, 0, i);

                        string SDisplay="";
                        if (IsByteDisplay) SDisplay = string.Format("Message[{0}]:[{1}]\r\n[{2}] ", len,data,strData);
                        else SDisplay = string.Format("Message[{0}]\r\n[{2}] ", len, data, strData);

                        LogAdd(IPHeader+SDisplay);

                        if (OnStringReceived != null) {
                            OnStringReceivedEventArgs Args = new OnStringReceivedEventArgs();
                            Args.AClient = client;
                            Args.received = data;
                            Args.message = strData;                            
                            //raise event
                            OnStringReceived(this, Args);
                        }

                        /*Reply Section*/
                        //Event receive string
                          
                        //string str = "" + clientIPAddress + ">" + data;
                        //Echo Reply
                        //Byte[] reply = bytes;
                        //stream.Write(reply, 0, reply.Length);
                        //GetTotal number of running threads
                        //int numThread = Process.GetCurrentProcess().Threads.Count;                        
                        //LogAdd(string.Format("{1}/{2}: Sent: {0}", str, Thread.CurrentThread.ManagedThreadId,numThread));
                    }
                }
                catch (Exception e){
                    LogAdd(string.Format(IPHeader+"Exception: {0}", e.ToString()));
                    client.Close();
                }
            }//endWhile
        }
        catch (Exception e){
            LogAdd(string.Format(IPHeader+"Exception: {0}", e.ToString()));
        }

        LogAdd(IPHeader + " Disconnected");
        //listConnectedClients.Remove(client);

    }

    public void Response(TcpClient client ,string msg) {        
            StreamWriter sWriter = new StreamWriter(client.GetStream());
            sWriter.Write(msg);
            sWriter.Flush();        
    }
    public void CloseClient(TcpClient SrcClient) {
        //check if no duplicate IP conected
        TcpClient DClient = SrcClient;
        string aIP = "" + IPAddress.Parse(((IPEndPoint)SrcClient.Client.RemoteEndPoint).Address.ToString());
        foreach (TcpClient AClient in listConnectedClients) {
            string strIP = "" + IPAddress.Parse(((IPEndPoint)AClient.Client.RemoteEndPoint).Address.ToString());            
            if (strIP == aIP) {
                if (DClient != null) DClient.Close();
                listConnectedClients.Remove(DClient);
                LogAdd(aIP + " Disconnected");
            }
        }
    }


    public void BroadCast(string msg){
        foreach (TcpClient client in listConnectedClients){
                StreamWriter sWriter = new StreamWriter(client.GetStream());
                sWriter.WriteLine(msg);
                sWriter.Flush();            
        }
    }

    public void BroadCast(string sIPAddress, string msg) {
        foreach (TcpClient client in listConnectedClients) {
            string clientIPAddress = "" + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
            if (sIPAddress==clientIPAddress) {
                StreamWriter sWriter = new StreamWriter(client.GetStream());
                sWriter.WriteLine(msg);
                sWriter.Flush();
            }
        }
    }
}
