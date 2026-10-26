using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using System.Data;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Runtime.InteropServices;

namespace TCPServer {

    public class SSPROConfig {
        public List<string> SiteNumbers { get; private set; }
        public List<string> SiteName { get; private set; }
        public List<string> IPAddresses { get; private set; }

        public string ErrorMessage;

        public int LoadConfiguration(string connectionString) {
            int R = 0;
            SiteNumbers = new List<string>();
            SiteName = new List<string>();
            IPAddresses = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                try {
                    connection.Open();
                    string query = "SELECT SiteID, SiteName, IPAddress from Sites order by SiteID";
                    using (SqlCommand command = new SqlCommand(query, connection)) {
                        using (SqlDataReader reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                string siteNumber = reader.GetString(0);
                                string siteName = reader.GetString(1);
                                string IPAddress = reader.GetString(2);

                                SiteNumbers.Add(siteNumber);
                                SiteName.Add(siteNumber);
                                IPAddresses.Add(IPAddress);

                                R = 1;
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    ErrorMessage = ex.Message;
                    R = -1;
                }
            }
            return R;
        }
    }
    public class CardUpdater {
        public event EventHandler<string> StatusUpdated;
        public string ErrorMessage;

        public string siteNumber;
        public string siteIPAddress;
        public string mysqlcon;
        public string connectionString;
        public int Id;
        public int IntervalSec;
        TextBox tbLog;

        public Boolean IsNeedUpdate;

        private Thread thdPooling;
        protected virtual void OnUpdateStatus(string StatusValue) {
            StatusUpdated?.Invoke(this, "\r\n["+ siteNumber + "]: "+ StatusValue);
        }

        public CardUpdater(string SiteID, string IPAddress) {
            siteNumber = SiteID;
            siteIPAddress = IPAddress;
            //"Server=127.0.0.1;Database=dbss;Uid=fms;Pwd=sspro123;";
            mysqlcon = string.Format("Server = {0}; Database = dbss; Uid = fms; Pwd = sspro123; ",IPAddress);
            IntervalSec = 10;
            // Set the thread as a background thread
            thdPooling = new Thread(FPooling);

        }
        public void Run() {
            thdPooling.IsBackground = true;
            thdPooling.Start();
        }

        private int stPooling = 0;
        private Boolean IsRunning = false;
        private void FPooling() {
            IsRunning = true;
            while (IsRunning) {
                switch (stPooling) {
                    case 0://Init
                        OnUpdateStatus("Init:" + siteIPAddress);

                        stPooling = 3;
                        break;
                    case 1://Idle
                        //check for pending un updated cards
                        //select * from twacards where cardnumber = pendingcardnumber
                        ErrorMessage = "";
                        
                        stPooling = 2;

                        DataSet ACArds = GetPendingCardsData(siteNumber, ref ErrorMessage);
                        if (ACArds.Tables[0].Rows.Count > 0)
                        {
                            OnUpdateStatus(ACArds.Tables[0].Rows.Count + " Cards Found");

                            for (int i = 0; i < ACArds.Tables[0].Rows.Count; i++)
                            {
                                DataRow dRow = ACArds.Tables[0].Rows[i];
                                string cardnum = dRow["CardNumber"].ToString();
                                OnUpdateStatus(cardnum + " Updating Cards Table");

                                int R = mysql_save_twacard(ACArds, dRow, ref cardnum, ref ErrorMessage);
                                int sync_status, retryCount = 0;
                                CardTransactionManager cm = new CardTransactionManager(connectionString);
                                if (R > 0)
                                {
                                    OnUpdateStatus(cardnum + " Mysql Update Success!");
                                    sync_status = 1;
                                    int RR = cm.ModifiedSiteCardNumber(siteNumber, cardnum, sync_status, retryCount, ref ErrorMessage);
                                    if (RR > 0)
                                    {
                                        OnUpdateStatus(cardnum + " Finish");
                                    }
                                    else
                                    {
                                        OnUpdateStatus(cardnum + " Failed Update TrxSyncCardData");
                                        if (RR < 0) OnUpdateStatus("[11]Error:" + ErrorMessage);
                                    }
                                }
                                else
                                {
                                    OnUpdateStatus(cardnum + " Mysql Update Failed");
                                    stPooling = 3;//check if mysql available
                                    if (R < 0) OnUpdateStatus("[12]Error:" + ErrorMessage);

                                    sync_status = -1;
                                    int RR = cm.FailedModifiedSiteCardNumber(siteNumber, cardnum, sync_status, ref ErrorMessage);

                                }
                            }
                        }
                        else {
                            OnUpdateStatus("Cards Pending:0");
                        }
                       
                        break;
                    case 2:
                        Thread.Sleep(IntervalSec * 1000);
                        stPooling = 1;
                        break;
                    case 3:
                        //Checking if destination SSPRO is Available                        
                        if (IsAvailableMysql(ref ErrorMessage)){
                            OnUpdateStatus("Mysql Ready");
                            stPooling = 1;
                        }
                        else {
                            OnUpdateStatus("Mysql Not Available");
                            OnUpdateStatus(ErrorMessage);
                            stPooling = 4;
                        }
                        
                        break;
                    case 4:
                        Thread.Sleep(10*IntervalSec * 1000);
                        stPooling = 3;
                        break;
                }
            }
        }

        //--------------------------------------------------------------------------------------
        //Updating MYSQL data
        public int a, b, c, d;
        //direct SQL server
        private SqlDataAdapter directSQLAdapter;
        
        public DataSet SQLquerry(string strQuerry, ref string ErrorMessage) {
            DataSet dSet = new DataSet();
            try {
                SqlConnection ASQL = new SqlConnection();
                ASQL.ConnectionString = connectionString;

                ASQL.Open();
                using (directSQLAdapter = new SqlDataAdapter(strQuerry, ASQL)) {
                    directSQLAdapter.Fill(dSet);
                }

                ASQL.Close();
            }
            catch (Exception ex) {
                
            }
            return dSet;
        }

        public DataSet GetPendingCardsData(string SiteID, ref string ErrorMessage) {
            //DataSet dSet = new DataSet();
            string query = string .Format(@"
                              SELECT 
                                    ISNULL(c.CardNumber,'')as CardNumber,
                                    ISNULL(c.AuthCode,'')as AuthCode,
                                    ISNULL(c.CardStatus,'')as CardStatus,
                                    ISNULL(c.AccNumber,'')as AccNumber,
                                    ISNULL(c.CardHolder,'')as CardHolder,
                                    cast(ISNULL(c.DailyLimit,0) as bigint) as DailyLimit,
                                    cast(ISNULL(c.CreditLimit,0) as bigint)as CreditLimit,
                                    ISNULL(c.VehicleNo,'')as VehicleNo,
                                    ISNULL(c.UserID,'')as UserID,
                                    --ISNULL(c.StoreWide,'')as StoreWide,
                                    --ISNULL(c.StoredValueCard,0)as StoredValueCard,
                                    ISNULL(c.CardType,0)as CardType,
                                    ISNULL(c.ProductType,0)as ProductType,
                                    ISNULL(c.TransLimitStatus,0)as TransLimitStatus,
                                    ISNULL(c.TransLimitType,0)as TransLimitType,
                                    cast(ISNULL(c.TransLimitAmount,0)  as bigint) as TransLimitAmount,
                                    cast(ISNULL(c.TransLimitVolume,0)  as bigint) as TransLimitVolume,
                                    ISNULL(c.MaxLimitStatus,0)as MaxLimitStatus,
                                    cast(ISNULL(c.MaxLimitAmount,0) as bigint) as MaxLimitAmount,
                                    cast(ISNULL(c.MaxLimitVolume,0) as bigint) as MaxLimitVolume,
                                    ISNULL(c.DateCreated,'')as DateCreated,
                                    ISNULL(c.DateExpired,'')as DateExpired,
                                    ISNULL(c.PostingUserID,'')as PostingUserID,
                                    ISNULL(c.PostingSystemDate,'')as PostingSystemDate,
                                    cast(ISNULL(c.TotalDayVolume,0) as bigint) as TotalDayVolume,
                                    cast(ISNULL(c.TotalDayMoney,0) as bigint) as TotalDayMoney,
                                    ISNULL(c.TotalDayBusinessDate,'')as TotalDayBusinessDate,
                                    cast(ISNULL(c.OverDrawLimit,0) as bigint) as OverDrawLimit,
                                    cast(ISNULL(c.CardBalance,0) as bigint) as CardBalance,
                                    cast(ISNULL(c.Odometer,0) as bigint) as Odometer,
                                    ISNULL(c.CompanyID,'')as CompanyID,
                                    --cast(ISNULL(c.LastCutOffDate,'') as bigint) as LastCutOffDate,
                                    --ISNULL(c.LastCutOffBalance,0)as LastCutOffBalance,
                                    cast(ISNULL(c.WeeklyLimit,0) as bigint) as WeeklyLimit,
                                    cast(ISNULL(c.MonthlyLimit,0) as bigint) as MonthlyLimit,
                                    cast(ISNULL(c.YearlyLimit,0) as bigint) as YearlyLimit,
                                    ISNULL(c.WeekCurrent,'')as WeekCurrent,
                                    cast(ISNULL(c.TotalWeekMoney,0) as bigint) as TotalWeekMoney,
                                    cast(ISNULL(c.TotalWeekVolume,0) as bigint) as TotalWeekVolume,
                                    ISNULL(c.MonthCurrent,'')as MonthCurrent,
                                    cast(ISNULL(c.TotalMonthMoney,0) as bigint) as TotalMonthMoney,
                                    cast(ISNULL(c.TotalMonthVolume,0) as bigint) as TotalMonthVolume,
                                    ISNULL(c.YearCurrent,'')as YearCurrent,
                                    cast(ISNULL(c.TotalYearMoney,0) as bigint) as TotalYearMoney,
                                    cast(ISNULL(c.TotalYearVolume,0) as bigint) as TotalYearVolume,
                                    ISNULL(c.QuotaType,'')as QuotaType,
                                    ISNULL(c.QuotaValue,0)as QuotaValue

                              FROM twaCard c 
                              INNER JOIN TrxSyncCardData s ON c.CardNumber = s.CardNumber
                              WHERE s.SiteID = '{0}' AND (s.SychStatus = 0 OR s.SychStatus = -1);", SiteID);

            DataSet dSet = SQLquerry(query,ref ErrorMessage);

            return dSet;
        }

        public int mysql_save_twacard(DataSet dSet, DataRow dRow,ref string CardNumber, ref string ErrorMessage) {
            int R = 0;
            try {
                //int MaxRows = dSet.Tables[0].Rows.Count;
                int MaxColum = dSet.Tables[0].Columns.Count;

                
                CardNumber = dRow["CardNumber"].ToString();

                string strTemp = "";
                string strInsertCmd = "INSERT INTO tbWaCard (";
                string strInsertValue = " VALUES(";


                string strDuplicateValue = " ON DUPLICATE KEY UPDATE ";

                int i, j;
                for (i = 0; i < MaxColum - 1; i++) {

                    strTemp += dSet.Tables[0].Columns[i].Caption + " ";
                    strInsertCmd += dSet.Tables[0].Columns[i].Caption;
                    if (i < MaxColum - 2)
                        strInsertCmd += ",";
                    else
                        strInsertCmd += ")";
                }

                    for (j = 0; j < MaxColum - 1; j++) {
                        string sCaption = dSet.Tables[0].Columns[j].Caption;
                        string sValue = dRow.ItemArray.GetValue(j).ToString().Trim();
                        strInsertValue += String.Format("'{0}'", sValue);

                        strDuplicateValue += String.Format("{0}='{1}'", sCaption, sValue);
                        if (j < MaxColum - 2) {
                            strInsertValue += ",";
                            strDuplicateValue += ",";

                        }
                        else {
                            strInsertValue += ")";
                            strDuplicateValue += ";";
                        }
                    }

                    //ON Duplicate Key Update
                    string s = strInsertCmd + strInsertValue + strDuplicateValue;
                    R = mysql_execute(s, ref ErrorMessage);
                    strInsertValue = " VALUES(";
                    strDuplicateValue = " ON DUPLICATE KEY UPDATE ";
            }
            catch (Exception e) {
                ErrorMessage = ErrorMessage + "[13]\r\n" + e.Message;
                R = -1;
            }
            return R;
        }

        public int mysql_save_twacard(DataSet dsDataTwaCard, ref string ErrorMessage) {
            int R = 0;
            try {
                DataSet dSet = new DataSet();
                dSet = dsDataTwaCard;

                DataRow dRow;
                int MaxRows = dSet.Tables[0].Rows.Count;
                int MaxColum = dSet.Tables[0].Columns.Count;
                dRow = dSet.Tables[0].Rows[0];

                string strTemp = "";
                string strInsertCmd = "INSERT INTO tbWaCard (";
                string strInsertValue = " VALUES(";


                string strDuplicateValue = " ON DUPLICATE KEY UPDATE ";

                int i, j;
                for (i = 0; i < MaxColum - 1; i++) {

                    strTemp += dSet.Tables[0].Columns[i].Caption + " ";
                    strInsertCmd += dSet.Tables[0].Columns[i].Caption;
                    if (i < MaxColum - 2)
                        strInsertCmd += ",";
                    else
                        strInsertCmd += ")";
                }
                //MessageBox.Show(strInsertCmd);
                for (i = 0; i < MaxRows; i++) {
                    dRow = dSet.Tables[0].Rows[i];
                    for (j = 0; j < MaxColum - 1; j++) {
                        string sCaption = dSet.Tables[0].Columns[j].Caption;
                        string sValue = dRow.ItemArray.GetValue(j).ToString();
                        strInsertValue += String.Format("'{0}'", sValue);

                        strDuplicateValue += String.Format("{0}='{1}'", sCaption, sValue);


                        if (j < MaxColum - 2) {
                            strInsertValue += ",";
                            strDuplicateValue += ",";

                        }
                        else {
                            strInsertValue += ")";
                            strDuplicateValue += ";";
                        }
                    }

                    //ON Duplicate Key Update
                    string s = strInsertCmd + strInsertValue + strDuplicateValue;

                    mysql_execute(s, ref ErrorMessage);
                    strInsertValue = " VALUES(";
                    strDuplicateValue = " ON DUPLICATE KEY UPDATE ";
                }
            }
            catch (Exception e) {
                ErrorMessage = ErrorMessage +"[14]\r\n"+ e.Message;
                R = -1;
            }
            return R;
        }

        public Boolean IsAvailableMysql(ref string ErrorMessage) {
            Boolean R = false;

            try{
                MySqlConnection connection = new MySqlConnection(mysqlcon);
                connection.Open();
                connection.Close();
                R = true;
            }
            catch (MySqlException ex){
                if (ex.Number == 0){
                    R=false;
                }
                else{
                    ErrorMessage = "[20]" + ex.Message;
                    R = false;
                }
            }
            return R;
        }

        public int mysql_execute(string strCmd, ref string ErrorMessage) {
            int R = 0;
            try {
                MySqlConnection Myconn = new MySqlConnection(mysqlcon);
                MySqlCommand Cmd;
                Myconn.Open();
                Cmd = Myconn.CreateCommand();
                Cmd.CommandText = strCmd;
                R = Cmd.ExecuteNonQuery();
                Myconn.Close();
            }
            catch (Exception e) {
                ErrorMessage = ErrorMessage + "[15]\r\n" + e.Message;
                R = -1;
            }
            return R;
        }

    }

}
