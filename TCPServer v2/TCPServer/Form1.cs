/*
24/01/2026
parse class need to be enhanced: financial safe and .decimal conversion regardsless server se

21/03/2024
Version 2.1 
-enhance mysql update function
-enhance connectionString: found bug and fixed
-tested on Server2019 ok

Version 2.0
 -ProductKey validation
 -connectionString:harcoded
 -Source moved to GPD laptop
 
 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;

using System.IO;
using IniFileClass;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net;


namespace TCPServer
{

    public partial class Form1 : Form
    {

       
        public Form1()
        {
            InitializeComponent();
        }

        private string connectionString = "";// string.Format("Data Source=DESKTOP-TUO3E0A\\SQLSERVER2014;Initial Catalog=FuelStation;User id=sa;Password=fms1234;");

        private Server myserver;
        List<CardUpdater> ListCardUpdater = new List<CardUpdater>();

        private void Form1_Load(object sender, EventArgs e){

            int AppType = 0;
            // Get hardware information
            string cpuId = MachineInfo.GetCpuId();
            string motherboardId = MachineInfo.GetMotherboardId();
            string hardDiskId = MachineInfo.GetHardDiskId();
            string MachineID = "[" + cpuId + "," + motherboardId + "," + hardDiskId + "]";
            // Generate product key
            string productKey = "";
            bool isValid;

            // Initialize the INI file object with the path to the INI file
            IniFile iniFile = new IniFile("config.ini");
            iniFile.Init("type", "APP", "1");
            AppType = int.Parse(iniFile.Read("type", "APP"));
            iniFile.Init("Port", "Server", "9103");
            iniFile.Init("IPAddress", "Server", "127.0.0.1");

            //iniFile.Init("DataSource", "SQLServer", "(local)\\SQLServer");
            //iniFile.Init("DBName", "SQLServer", "FuelStation");
            //iniFile.Init("UserID", "SQLServer", "sa");
            //iniFile.Init("Password", "SQLServer", "fms1234");

            iniFile.Init("MachineID", "License", MachineID);
            iniFile.Init("ProductKey", "License", productKey);

            productKey = iniFile.Read("ProductKey", "License");
            isValid = MachineInfo.ValidateProductKey(AppType.ToString(), productKey, cpuId, motherboardId, hardDiskId);

            //Generate
            //MessageBox.Show("Key:"+ MachineInfo.GenerateProductKey(AppType.ToString(), cpuId, motherboardId, hardDiskId));

            string IPAddress = iniFile.Read("IPAddress", "Server");
            int Port = int.Parse(iniFile.Read("Port", "Server"));

            //string DataSource = iniFile.Read("DataSource", "SQLServer");
            //string DBName = iniFile.Read("DBName", "SQLServer");
            //string UserID = iniFile.Read("UserID", "SQLServer");
            //string Password = iniFile.Read("Password", "SQLServer");
            //connectionString = string.Format("Data Source={0};Initial Catalog={1};User id={2};Password={3};",DataSource,DBName,UserID,Password);

            if (!iniFile.KeyExists("EncryptConn", "SQLServer")) {
                //string DefaultConn = "Data Source=DESKTOP-TUO3E0A\\SQLSERVER2014;Initial Catalog=FuelStation;User id=sa;Password=fms1234;";
                //string DefaultConn = "Data Source=WIN-BA7F5138JOH\\SQLEXPRESS;Initial Catalog=FuelStation;User id=sa;Password=fms#1234;";
                string DefaultConn = "Data Source=PSMNXDB\\SQLNONAX;Initial Catalog=FuelStation;User id=fuelintegration;Password=psmi789!;";

                string EncryptMessage = AESq.Encrypt(DefaultConn);
                iniFile.Write("EncryptConn", EncryptMessage, "SQLServer");
            }

            string EncryptedConnString = iniFile.Read("EncryptConn", "SQLServer");
            connectionString = AESq.Decrypt(EncryptedConnString);

            //SQLConnectionChecker AChecker = new SQLConnectionChecker();
            string errmsg="";
            if (!SQLConnectionChecker.IsConnectionStringValid(connectionString, ref errmsg)) {
                LogAdd("cn:"+connectionString+"]");
                LogAdd("DB Connection Failed");
                MessageBox.Show("");
            }
            
            LogAdd("cpuId:"+ cpuId);
            LogAdd("motherboardId:" + motherboardId);
            LogAdd("hardDiskId:" + hardDiskId);
            //isValid = false;

            if(isValid) {
                Thread t = new Thread(delegate () {
                    myserver = new Server(IPAddress, Port, true, ref Log);
                    //IP Public 45.127.133.210
                    //myserver = new Server("45.127.133.210", 10001, true, ref Log);

                    myserver.OnStringReceived += OnReceive;
                    myserver.OnLogAdd += OnLogAdd;
                    myserver.Start();
                });
                t.Start();
                ///bwProcess.RunWorkerAsync();
                LogAdd("Server Started on");
                LogAdd(IPAddress+":"+Convert.ToString(Port));

                //Load Configuration CardUpdater
                SSPROConfig ASSPRO = new SSPROConfig();
                if (ASSPRO.LoadConfiguration(connectionString) > 0) {
                    for (int i=0;i<ASSPRO.SiteNumbers.Count();i++) {
                        string SiteID = ASSPRO.SiteNumbers[i];
                        string IPAddr = ASSPRO.IPAddresses[i];

                        CardUpdater aUpdater = new CardUpdater(SiteID, IPAddr);
                        aUpdater.Id = i + 1;
                        aUpdater.connectionString = connectionString;
                        aUpdater.StatusUpdated += CardUpdater_handler;

                        aUpdater.Run();
                        ListCardUpdater.Add(aUpdater);
                    }
                }


            }
            else {
                LogAdd("[Invalid License Code]");
            }

            //default is minimezed
            this.WindowState = FormWindowState.Minimized;
            Hide();
            notifyIcon.Visible = true;

        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e){
            if (myserver!=null)
                myserver.IsRunning = false;
        }

        int i = 0;
        private void bwProcess_DoWork(object sender, DoWorkEventArgs e) {
            this.BeginInvoke((Action)delegate {
                Log.AppendText("\r\nWorker Process Started");
            });

            while (true) {
               /* i++;
                this.BeginInvoke((Action)delegate {
                    Log.AppendText("\r\ni:" + i.ToString());
                    myserver.BroadCast(">>Send Data " + i.ToString());
                });
                */
                Thread.Sleep(1000);
            }
        }
       
        private void button1_Click(object sender, EventArgs e) {
            Log.Clear();
        }

        private void SendMessage(TcpClient client, string message) {
            NetworkStream networkStream = client.GetStream();
            byte[] messageBytes = Encoding.ASCII.GetBytes(message); // a UTF-8 encoder would be 'better', as this is the standard for network communications
            int length = messageBytes.Length;
            byte[] lengthBytes = System.BitConverter.GetBytes(length);
            if (System.BitConverter.IsLittleEndian) {
                Array.Reverse(lengthBytes);
            }
            networkStream.Write(lengthBytes, 0, lengthBytes.Length);
            networkStream.Write(messageBytes, 0, length);
        }

        private void LogAdd(string message) {
            this.BeginInvoke((Action)delegate {

                int currentPosition = Log.SelectionStart;
                Log.AppendText(message);

                if (!ChAutoScroll.Checked){
                    // Restore the cursor position
                    Log.SelectionStart = currentPosition;
                    Log.SelectionLength = 0;
                    Log.ScrollToCaret();
                }

                if (Log.Lines.Count() > 1000) {
                    Log.Clear();
                }
                EventLogger.Log(message);              
            });
        }

        private void OnLogAdd(object sender, OnLogAddEventArgs e) {
            LogAdd(e.message);
        }
        

        private string cmd = "";
        Boolean IsEcho = false;
        private void OnReceive(object sender, OnStringReceivedEventArgs e) {
            cmd = cmd + e.received;
            string ErrorMessage;
            LogAdd("Parsing..->\r\n" + e.message);
            DataSetParser OParse = new DataSetParser();

            try {
                if (OParse.IsValidMessage(e.message)) {
                    DataField AData = OParse.ParseInputMessage(e.message, ref Log);

                    LogAdd("Parsing OK==========================");
                    LogAdd("\r\nHeader:" + AData.Header.ToString());
                    LogAdd("\r\nSeq:" + AData.Seq.ToString());
                    LogAdd("\r\nCompanyID:" + AData.CompanyID.ToString());
                    LogAdd("\r\nSiteID:" + AData.SiteID.ToString());
                    LogAdd("\r\nTerminalID:" + AData.TerminalID.ToString());
                    LogAdd("\r\nTransNo:" + AData.TransNo.ToString());
                    LogAdd("\r\nTransCompleted:" + AData.TransCompleted.ToString());
                    LogAdd("\r\nPumpID:" + AData.PumpID.ToString());
                    LogAdd("\r\nNozzleID:" + AData.NozzleID.ToString());
                    LogAdd("\r\nProductID:" + AData.ProductID.ToString());
                    LogAdd("\r\nProductName:" + AData.ProductName.ToString());
                    LogAdd("\r\nQuantity:" + AData.Quantity.ToString());
                    LogAdd("\r\nUnitPrice:" + AData.UnitPrice.ToString());
                    LogAdd("\r\nAmount:" + AData.Amount.ToString());
                    LogAdd("\r\nTot_Amount:" + AData.Tot_Amount.ToString());
                    LogAdd("\r\nTot_Volume:" + AData.Tot_Volume.ToString());
                    LogAdd("\r\nCal_Tot_Amount:" + AData.Cal_Tot_Amount.ToString());
                    LogAdd("\r\nCal_Tot_Volume:" + AData.Cal_Tot_Volume.ToString());
                    LogAdd("\r\nMOP:" + AData.MOP.ToString());
                    LogAdd("\r\nMOPCardID:" + AData.MOPCardID.ToString());
                    LogAdd("\r\nSupervisorTag:" + AData.SupervisorTag.ToString());
                    LogAdd("\r\nAttendantTag:" + AData.AttendantTag.ToString());
                    LogAdd("\r\nCustomerTag:" + AData.CustomerTag.ToString());
                    LogAdd("\r\nVehicleTag:" + AData.VehicleTag.ToString());
                    LogAdd("\r\nOdometer:" + AData.Odometer.ToString());
                    LogAdd("\r\nEngineHour:" + AData.EngineHour.ToString());
                    LogAdd("\r\nTransferTo:" + AData.TransferTo.ToString());
                    LogAdd("\r\nTankID:" + AData.TankID.ToString());
                    LogAdd("\r\nTranType:" + AData.TranType.ToString());
                    LogAdd("\r\nTranMode:" + AData.TranMode.ToString());
                    LogAdd("\r\nLoc_Longitude:" + AData.Loc_Longitude.ToString());
                    LogAdd("\r\nLoc_Latitude:" + AData.Loc_Latitude.ToString());
                    LogAdd("\r\nOverridePurpose:" + AData.OverridePurpose.ToString());
                    LogAdd("\r\nTankReadingBefore_H:" + AData.TankReadingBefore_H.ToString());
                    LogAdd("\r\nTankReadingBefore_V:" + AData.TankReadingBefore_V.ToString());
                    LogAdd("\r\nTankReadingAfter_H:" + AData.TankReadingAfter_H.ToString());
                    LogAdd("\r\nTankReadingAfter_V:" + AData.TankReadingAfter_V.ToString());
                    LogAdd("\r\nTranSignature:" + AData.TranSignature.ToString());
                    LogAdd("\r\nPostedToServer:" + AData.PostedToServer.ToString());
                    LogAdd("\r\nPostedToFuelokey:" + AData.PostedToFuelokey.ToString());
                    LogAdd("\r\nExportedToUSB:" + AData.ExportedToUSB.ToString());
                    LogAdd("\r\nPrintCount:" + AData.PrintCount.ToString());
                    LogAdd("\r\nJobOrderID:" + AData.JobOrderID.ToString());
                    LogAdd("\r\nCompartemenID:" + AData.CompartemenID.ToString());
                    LogAdd("\r\nSynchToAX:" + AData.SynchToAX.ToString());
                    LogAdd("\r\nParsing OK==========================");

                    try {
                        CardTransactionManager ADB = new CardTransactionManager(connectionString);
                        //LogAdd("\r\nConn:"+connectionString);

                        DateTime BussinesDate = DateTime.Parse(AData.TransCompleted);
                        DateTime TransCompleted = DateTime.Parse(AData.TransCompleted);
                        DateTime PostedDatetime = DateTime.Now;
                        string PostedUserID = "System";

                        ADB.InsertOrUpdateCardTransaction(AData.CompanyID, AData.SiteID, AData.TerminalID.ToString(), BussinesDate.Date,
                                                          AData.CustomerTag, AData.VehicleTag, TransCompleted,
                                                          AData.TransNo, AData.PumpID, AData.NozzleID, AData.ProductID.ToString(), AData.ProductName,
                                                          Convert.ToDecimal(AData.Quantity), 
                                                          Convert.ToDecimal(AData.UnitPrice), 
                                                          Convert.ToDecimal(AData.Amount),
                                                          Convert.ToDecimal(AData.Tot_Volume), 
                                                          Convert.ToDecimal(AData.Tot_Amount), 
                                                          Convert.ToDecimal(AData.Odometer),
                                                          PostedUserID, PostedDatetime);
                        LogAdd("\r\nPosting OK==========================");
                        //Update Cards
                        int err = 0;
                        ErrorMessage = "";
                        //DriverTag
                        err = ADB.UpdateDriverTag(AData.CustomerTag, ref ErrorMessage);
                        if (err == -1) LogAdd("\r\n\tError DriverTag:" + ErrorMessage);
                        else LogAdd("\r\nDrverTag Updated:" + AData.CustomerTag);
                        //VehicleTag
                        err = ADB.UpdateVehicleTag(AData.VehicleTag, ref ErrorMessage);
                        if (err == -1) LogAdd("\r\n\tError VehicleTag:" + ErrorMessage);
                        else LogAdd("\r\nVehicleTag Updated:" + AData.VehicleTag);

                        //reply with valid 
                        string Response = string.Format("{0}-:-{1}-:-{2}-:-[OK] \r\n", AData.Header, AData.Seq, AData.SiteID);
                        myserver.Response(e.AClient, Response);
                        LogAdd("\r\nResponse:" + Response);
                    }
                    catch (Exception ex) {
                        LogAdd("\r\nError" + ex.Message);
                    }
                }
                else {
                    LogAdd("Invalid Message:" + e.message);
                    //reply with valid 
                    string Response = string.Format("[NOT] \r\n");
                    myserver.Response(e.AClient, Response);
                    LogAdd("\r\nResponse:" + Response);
                    //myserver.CloseClient(e.AClient);
                    //save to DB as eventLog
                }
            }
            catch (Exception ex) {
                LogAdd("Error:" + ex.Message);
            }
        }

        private void CardUpdater_handler(object sender, string Status) {
            try {
                if (sender is CardUpdater ACU) {
                    LogAdd(Status);
                }
            }
            catch (Exception ex) {
                LogAdd("Error: " + ex.Message);
            }
        }

        private void Log_TextChanged(object sender, EventArgs e) {
            // Ensure the caret stays at the end of the text
            Log.SelectionStart = Log.Text.Length;
            Log.ScrollToCaret();
        }
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = true;
        }
        private void Form1_Resize(object sender, EventArgs e){
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized){
                Hide();
                notifyIcon.Visible = true;
            }
        }
    }
}
