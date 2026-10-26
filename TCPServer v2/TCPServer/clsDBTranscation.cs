using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;


using System.Windows.Forms;

namespace TCPServer {
    class clsDBTranscation {
    }

    public class SQLConnectionChecker {
        public static bool IsConnectionStringValid(string connectionString, ref string err) {
            bool isValid = false;

            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();
                    isValid = true;
                    connection.Close();
                }
            }
            catch (Exception ex) {
                err = ex.Message;
            }
            return isValid;
        }

    }
        public class CardTransactionManager {
        //PSMI
        //Data Source = PSMNXDB\\SQLNONAX
        /*
         IP SQL Server:
        Server : PSMNXDB\SQLNONAX
        DB : FuelStation
        user name : fuelintegration
        password : psmi789!
        
        */

        private string connectionString = "";// string.Format("Data Source=DESKTOP-TUO3E0A\\SQLSERVER2014;Initial Catalog=FuelStation;User id=sa;Password=fms1234;");

        public CardTransactionManager(string connectionString)
        {
            this.connectionString = connectionString;
        }



        public int UpdateDriverTag(string CardNumber, ref string ErrorMessage) {
            int R = 0;
            //daily data
            DateTime BussinessDateNow,PostingSystemDate;

            decimal TotalDayVolume=0,TotalDayMoney=0;
            string TotalDayBussinesDate;

            //monthly data
            decimal TotalMonthVolume=0, TotalMonthMoney=0;
            string MonthCurrent;

            //yearly data
            decimal TotalYearVolume=0, TotalYearMoney=0;
            string YearCurrent;

            decimal Odometer = 0;
            
            BussinessDateNow = DateTime.Now;


            TotalDayBussinesDate = BussinessDateNow.Date.ToString("dd/MM/yyyy");
            TotalDayBussinesDate = TotalDayBussinesDate.Replace('-', '/');
            //202403
            MonthCurrent = BussinessDateNow.ToString("yyyyMM");
            YearCurrent = BussinessDateNow.ToString("yyyy");

            string query;

            query = string.Format(@" 
                               SELECT BusinessDate,DriverTag, SUM(Quantity) AS TotalDayVolume, SUM(Amount) AS TotalDayMoney FROM dbo.CardTransaction 
                               WHERE BusinessDate = '{0}'  AND DriverTag = '{1}' 
                               GROUP BY BusinessDate, DriverTag; ", BussinessDateNow.Date.ToString(),CardNumber);

            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            TotalDayVolume = !reader.IsDBNull(reader.GetOrdinal("TotalDayVolume")) ? reader.GetDecimal(reader.GetOrdinal("TotalDayVolume")) : 0;
                            TotalDayMoney = !reader.IsDBNull(reader.GetOrdinal("TotalDayMoney")) ? reader.GetDecimal(reader.GetOrdinal("TotalDayMoney")) : 0;
                        }
                    }
                    else {
                        // DataNotFound
                        TotalDayVolume = 0;
                        TotalDayMoney = 0;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex){                
                ErrorMessage ="[1]"+  ex.Message;
                R = -1;
            }


            //Monthly Data
            query = @" SELECT DriverTag, SUM(Quantity) AS TotalMonthVolume, SUM(Amount) AS TotalMonthMoney
                       FROM 
                            dbo.CardTransaction
                       WHERE 
                            MONTH(BusinessDate) = MONTH(@BussinessDate) 
                            AND YEAR(BusinessDate) = YEAR(@BussinessDate) 
                            AND DriverTag = @DriverTag 
                       GROUP BY 
                            DriverTag; ";
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@DriverTag", CardNumber);
                    command.Parameters.AddWithValue("@BussinessDate", BussinessDateNow.Date.ToString());


                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            TotalMonthVolume = !reader.IsDBNull(reader.GetOrdinal("TotalMonthVolume")) ? reader.GetDecimal(reader.GetOrdinal("TotalMonthVolume")) : 0;
                            TotalMonthMoney = !reader.IsDBNull(reader.GetOrdinal("TotalMonthMoney")) ? reader.GetDecimal(reader.GetOrdinal("TotalMonthMoney")) : 0;
                        }
                    }
                    else {
                        // DataNotFound
                        TotalMonthVolume = 0;
                        TotalMonthMoney = 0;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[2]" + ex.Message;
                R = -1;

            }

            //Yearly Data
            query = @"SELECT 
                            DriverTag,
                            SUM(Quantity) AS TotalYearVolume,
                            SUM(Amount) AS TotalYearMoney
                      FROM 
                            dbo.CardTransaction
                      WHERE 
                            YEAR(BusinessDate) =  YEAR(@BussinessDate) 
                            AND DriverTag = @DriverTag 
                      GROUP BY 
                            DriverTag;";
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@DriverTag", CardNumber);
                    command.Parameters.AddWithValue("@BussinessDate", BussinessDateNow.Date.ToString());


                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            TotalYearVolume = !reader.IsDBNull(reader.GetOrdinal("TotalYearVolume")) ? reader.GetDecimal(reader.GetOrdinal("TotalYearVolume")) : 0;
                            TotalYearMoney = !reader.IsDBNull(reader.GetOrdinal("TotalYearMoney")) ? reader.GetDecimal(reader.GetOrdinal("TotalYearMoney")) : 0;
                        }
                    }
                    else {
                        // DataNotFound
                        TotalYearVolume = 0;
                        TotalYearMoney = 0;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[3]" + ex.Message;
                R = -1;
            }

            //Odometer
            query = @"select TOP 1 Odometer from CardTransaction 
                      where DriverTag = @DriverTag  
                      order by TransCompleted desc  ;";
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@DriverTag", CardNumber);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {

                            if (!reader.IsDBNull(reader.GetOrdinal("Odometer"))) {
                                Odometer = reader.GetDecimal(reader.GetOrdinal("Odometer"));
                            }
                            else {
                                // Handle null value if needed
                                Odometer = 0; // or any other appropriate default value
                            }
                        }
                    }
                    else {
                        // DataNotFound
                        Odometer = 0; // or any other appropriate default value
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[4]" + ex.Message;
                R = -1;
            }

            if (R == -1) return R;

            //apply Update Set
            PostingSystemDate = DateTime.Now;
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();

                    string commandText = @"
                       UPDATE twacard SET  
                             TotalDayBusinessDate = @TotalDayBusinessDate,
                             TotalDayVolume = @TotalDayVolume,
                             TotalDayMoney = @TotalDayMoney,
                             TotalMonthVolume = @TotalMonthVolume,
                             TotalMonthMoney = @TotalMonthMoney,
                             MonthCurrent = @MonthCurrent,
                             Odometer = @Odometer,
                             YearCurrent = @YearCurrent,
                             TotalYearMoney = @TotalYearMoney,
                             TotalYearVolume = @TotalYearVolume,
                             PostingUserID = @PostingUserID,
                             PostingSystemDate = @PostingSystemDate 
                       WHERE CardNumber=@CardNumber;";

                    using (SqlCommand command = new SqlCommand(commandText, connection)) {
                        command.Parameters.AddWithValue("@TotalDayBusinessDate", TotalDayBussinesDate);
                        command.Parameters.AddWithValue("@TotalDayVolume", TotalDayVolume);
                        command.Parameters.AddWithValue("@TotalDayMoney", TotalDayMoney);
                        command.Parameters.AddWithValue("@TotalMonthVolume", TotalMonthVolume);
                        command.Parameters.AddWithValue("@TotalMonthMoney", TotalMonthVolume);
                        command.Parameters.AddWithValue("@MonthCurrent", MonthCurrent);
                        command.Parameters.AddWithValue("@Odometer", Odometer);
                        command.Parameters.AddWithValue("@YearCurrent", YearCurrent);
                        command.Parameters.AddWithValue("@TotalYearMoney", TotalYearMoney);
                        command.Parameters.AddWithValue("@TotalYearVolume", TotalYearVolume);
                        command.Parameters.AddWithValue("@CardNumber", CardNumber);
                        command.Parameters.AddWithValue("@PostingUserID", "FMS System");
                        command.Parameters.AddWithValue("@PostingSystemDate", PostingSystemDate.ToString("yyyy/MM/dd HH:mm:ss"));
                        command.ExecuteNonQuery();
                    }
                }
                R = 1;
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[4]" + ex.Message;
                R = -1;
            }

            //Update Cards tobe synch
            if (R > 0) {
                int sync_status = 0, retryCount = 0;
                R = ModifiedCardNumber(CardNumber, sync_status, retryCount, ref ErrorMessage);
            }

            return R;
        }

        public int UpdateVehicleTag(string CardNumber, ref string ErrorMessage) {
            int R = 0;
            //daily data
            DateTime BussinessDateNow, PostingSystemDate;

            decimal TotalDayVolume = 0, TotalDayMoney = 0;
            string TotalDayBussinesDate;

            //monthly data
            decimal TotalMonthVolume = 0, TotalMonthMoney = 0;
            string MonthCurrent;

            //yearly data
            decimal TotalYearVolume = 0, TotalYearMoney = 0;
            string YearCurrent;

            decimal Odometer = 0;

            BussinessDateNow = DateTime.Now;


            TotalDayBussinesDate = BussinessDateNow.Date.ToString("dd/MM/yyyy");
            TotalDayBussinesDate = TotalDayBussinesDate.Replace('-', '/');
            //202403
            MonthCurrent = BussinessDateNow.ToString("yyyyMM");
            YearCurrent = BussinessDateNow.ToString("yyyy");

            string query;

            query = string.Format(@" 
                               SELECT BusinessDate,VehicleTag, SUM(Quantity) AS TotalDayVolume, SUM(Amount) AS TotalDayMoney FROM dbo.CardTransaction 
                               WHERE BusinessDate = '{0}'  AND VehicleTag = '{1}' 
                               GROUP BY BusinessDate, VehicleTag; ", BussinessDateNow.Date.ToString(), CardNumber);

            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            TotalDayVolume = !reader.IsDBNull(reader.GetOrdinal("TotalDayVolume")) ? reader.GetDecimal(reader.GetOrdinal("TotalDayVolume")) : 0;
                            TotalDayMoney = !reader.IsDBNull(reader.GetOrdinal("TotalDayMoney")) ? reader.GetDecimal(reader.GetOrdinal("TotalDayMoney")) : 0;
                        }
                    }
                    else {
                        // DataNotFound
                        TotalDayVolume = 0;
                        TotalDayMoney = 0;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = "[1]" + ex.Message;
                R = -1;
            }


            //Monthly Data
            query = @" SELECT VehicleTag, SUM(Quantity) AS TotalMonthVolume, SUM(Amount) AS TotalMonthMoney
                       FROM 
                            dbo.CardTransaction
                       WHERE 
                            MONTH(BusinessDate) = MONTH(@BussinessDate) 
                            AND YEAR(BusinessDate) = YEAR(@BussinessDate) 
                            AND VehicleTag = @VehicleTag 
                       GROUP BY 
                            VehicleTag; ";
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@VehicleTag", CardNumber);
                    command.Parameters.AddWithValue("@BussinessDate", BussinessDateNow.Date.ToString());


                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            TotalMonthVolume = !reader.IsDBNull(reader.GetOrdinal("TotalMonthVolume")) ? reader.GetDecimal(reader.GetOrdinal("TotalMonthVolume")) : 0;
                            TotalMonthMoney = !reader.IsDBNull(reader.GetOrdinal("TotalMonthMoney")) ? reader.GetDecimal(reader.GetOrdinal("TotalMonthMoney")) : 0;
                        }
                    }
                    else {
                        // DataNotFound
                        TotalMonthVolume = 0;
                        TotalMonthMoney = 0;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[2]" + ex.Message;
                R = -1;

            }

            //Yearly Data
            query = @"SELECT 
                            VehicleTag,
                            SUM(Quantity) AS TotalYearVolume,
                            SUM(Amount) AS TotalYearMoney
                      FROM 
                            dbo.CardTransaction
                      WHERE 
                            YEAR(BusinessDate) =  YEAR(@BussinessDate) 
                            AND VehicleTag = @VehicleTag 
                      GROUP BY 
                            VehicleTag;";
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@VehicleTag", CardNumber);
                    command.Parameters.AddWithValue("@BussinessDate", BussinessDateNow.Date.ToString());


                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            TotalYearVolume = !reader.IsDBNull(reader.GetOrdinal("TotalYearVolume")) ? reader.GetDecimal(reader.GetOrdinal("TotalYearVolume")) : 0;
                            TotalYearMoney = !reader.IsDBNull(reader.GetOrdinal("TotalYearMoney")) ? reader.GetDecimal(reader.GetOrdinal("TotalYearMoney")) : 0;
                        }
                    }
                    else {
                        // DataNotFound
                        TotalYearVolume = 0;
                        TotalYearMoney = 0;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[3]" + ex.Message;
                R = -1;
            }
            //Odometer
            query = @"select TOP 1 Odometer from CardTransaction 
                      where VehicleTag = @VehicleTag  
                      order by TransCompleted desc  ;";
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@VehicleTag", CardNumber);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows) {
                        while (reader.Read()) {
                            if (!reader.IsDBNull(reader.GetOrdinal("Odometer"))) {
                                Odometer = reader.GetDecimal(reader.GetOrdinal("Odometer"));
                            }
                            else {
                                // Handle null value if needed
                                Odometer = 0; // or any other appropriate default value
                            }
                        }
                    }
                    else {
                        // DataNotFound
                        Odometer = 0; // or any other appropriate default value
                    }

                    reader.Close();
                }
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[4]" + ex.Message;
                R = -1;
            }

            if (R == -1) return R;

            //apply Update Set
            PostingSystemDate = DateTime.Now;
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();

                    string commandText = @"
                       UPDATE twacard SET  
                             TotalDayBusinessDate = @TotalDayBusinessDate,
                             TotalDayVolume = @TotalDayVolume,
                             TotalDayMoney = @TotalDayMoney,
                             TotalMonthVolume = @TotalMonthVolume,
                             TotalMonthMoney = @TotalMonthMoney,
                             MonthCurrent = @MonthCurrent,
                             Odometer = @Odometer,
                             YearCurrent = @YearCurrent,
                             TotalYearMoney = @TotalYearMoney,
                             TotalYearVolume = @TotalYearVolume,
                             PostingUserID = @PostingUserID,
                             PostingSystemDate = @PostingSystemDate 
                       WHERE CardNumber=@CardNumber;";

                    using (SqlCommand command = new SqlCommand(commandText, connection)) {
                        command.Parameters.AddWithValue("@TotalDayBusinessDate", TotalDayBussinesDate);
                        command.Parameters.AddWithValue("@TotalDayVolume", TotalDayVolume);
                        command.Parameters.AddWithValue("@TotalDayMoney", TotalDayMoney);
                        command.Parameters.AddWithValue("@TotalMonthVolume", TotalMonthVolume);
                        command.Parameters.AddWithValue("@TotalMonthMoney", TotalMonthVolume);
                        command.Parameters.AddWithValue("@MonthCurrent", MonthCurrent);
                        command.Parameters.AddWithValue("@Odometer", Odometer);
                        command.Parameters.AddWithValue("@YearCurrent", YearCurrent);
                        command.Parameters.AddWithValue("@TotalYearMoney", TotalYearMoney);
                        command.Parameters.AddWithValue("@TotalYearVolume", TotalYearVolume);
                        command.Parameters.AddWithValue("@CardNumber", CardNumber);
                        command.Parameters.AddWithValue("@PostingUserID", "FMS System");
                        command.Parameters.AddWithValue("@PostingSystemDate", PostingSystemDate.ToString("yyyy/MM/dd HH:mm:ss"));
                        command.ExecuteNonQuery();
                    }
                }
                R = 1;
            }
            catch (Exception ex) {
                ErrorMessage = ErrorMessage + "\r\n" + "[4]" + ex.Message;
                R = -1;
            }

            //Update Cards tobe synch
            if (R > 0) {
                int sync_status=0,retryCount=0;
                R = ModifiedCardNumber(CardNumber, sync_status, retryCount,ref ErrorMessage);
            }

            return R;
        }

        public int ModifiedCardNumber(string cardNumber,int sync_status,int retryCount, ref string ErrorMessage) {
            int R = 0;
            string mergeQuery = @"
                    MERGE INTO TrxSyncCardData AS target
                    USING (
                           SELECT SiteID, @CardNumber AS CardNumber FROM Sites
                    ) AS source
                    ON target.SiteID = source.SiteID AND target.CardNumber = source.CardNumber
                    WHEN MATCHED THEN
                        UPDATE SET target.SychStatus = @SynchStatus, target.SychRetryCount = @RetryCount, target.SyncDateTime = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (SiteID, CardNumber, SychStatus, SychRetryCount, SyncDateTime)
                        VALUES (source.SiteID, source.CardNumber, @SynchStatus, @RetryCount, GETDATE());
                    ";

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                SqlCommand command = new SqlCommand(mergeQuery, connection);
                command.Parameters.AddWithValue("@CardNumber", cardNumber);
                command.Parameters.AddWithValue("@SynchStatus", sync_status);
                command.Parameters.AddWithValue("@RetryCount", retryCount);

                try {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    //Console.WriteLine($"{rowsAffected} rows affected.");
                    R = rowsAffected;
                }
                catch (Exception ex) {
                    //Console.WriteLine("Error: " + ex.Message);
                    ErrorMessage = ErrorMessage + "\r\n" + "[5]" + ex.Message;
                    R = -1;
                }
            }
            return R;
        }

        public int ModifiedSiteCardNumber(string SiteID,string cardNumber, int sync_status, int retryCount, ref string ErrorMessage) {
            int R = 0;
            string mergeQuery = @"
                   MERGE INTO TrxSyncCardData AS target
                    USING (
                        VALUES (@SiteID, @CardNumber)
                    ) AS source (SiteID, CardNumber)
                    ON target.SiteID = source.SiteID AND target.CardNumber = source.CardNumber
                    WHEN MATCHED THEN
                        UPDATE SET
                            SyncDateTime = GETDATE(),
                            SychStatus = @SynchStatus,
                            SychRetryCount = @RetryCount
                    WHEN NOT MATCHED THEN
                        INSERT (SiteID, CardNumber, SychStatus, SychRetryCount, SyncDateTime)
                        VALUES (@SiteID, @CardNumber, @SynchStatus, @RetryCount, GETDATE());

                    ";

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                SqlCommand command = new SqlCommand(mergeQuery, connection);
                command.Parameters.AddWithValue("@SiteID", SiteID);
                command.Parameters.AddWithValue("@CardNumber", cardNumber);
                command.Parameters.AddWithValue("@SynchStatus", sync_status);
                command.Parameters.AddWithValue("@RetryCount", retryCount);

                try {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    //Console.WriteLine($"{rowsAffected} rows affected.");
                    R = rowsAffected;
                }
                catch (Exception ex) {
                    //Console.WriteLine("Error: " + ex.Message);
                    ErrorMessage = ErrorMessage + "\r\n" + "[5A]" + ex.Message;
                    R = -1;
                }
            }
            return R;
        }

        public int FailedModifiedSiteCardNumber(string SiteID, string cardNumber, int sync_status, ref string ErrorMessage) {
            int R = 0;
            int retryCount = 1;
            string mergeQuery = @"
                   MERGE INTO TrxSyncCardData AS target
                    USING (
                        VALUES (@SiteID, @CardNumber)
                    ) AS source (SiteID, CardNumber)
                    ON target.SiteID = source.SiteID AND target.CardNumber = source.CardNumber
                    WHEN MATCHED THEN
                        UPDATE SET
                            SyncDateTime = GETDATE(),
                            SychStatus = @SynchStatus,
                            SychRetryCount = SychRetryCount+1
                    WHEN NOT MATCHED THEN
                        INSERT (SiteID, CardNumber, SychStatus, SychRetryCount, SyncDateTime)
                        VALUES (@SiteID, @CardNumber, @SynchStatus, @RetryCount, GETDATE());

                    ";

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                SqlCommand command = new SqlCommand(mergeQuery, connection);
                command.Parameters.AddWithValue("@SiteID", SiteID);
                command.Parameters.AddWithValue("@CardNumber", cardNumber);
                command.Parameters.AddWithValue("@SynchStatus", sync_status);
                command.Parameters.AddWithValue("@RetryCount", retryCount);

                try {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    R = rowsAffected;
                }
                catch (Exception ex) {
                    ErrorMessage = ErrorMessage + "\r\n" + "[5B]" + ex.Message;
                    R = -1;
                }
            }
            return R;
        }

        public void InsertOrUpdateCardTransaction(
            string companyId,
            string siteId,
            string terminalId,
            DateTime businessDate,
            string driverTag,
            string vehicleTag,
            DateTime transCompleted,
            long transNo,
            int pumpId,
            int nozzleId,
            string productId,
            string productName,
            decimal quantity,
            decimal price,
            decimal amount,
            decimal totalQuantity,
            decimal totalAmount,
            decimal odometer,
            string postingUserId,
            DateTime postingSystemDate) {
            using (SqlConnection connection = new SqlConnection(connectionString)) {
                connection.Open();

                string commandText = @"
                MERGE INTO dbo.CardTransaction AS target
                USING (VALUES (@CompanyId, @SiteId, @TerminalId, @BusinessDate, @TransNo, @PumpId)) AS source (CompanyId, SiteId, TerminalId, BusinessDate, TransNo, PumpId)
                ON target.CompanyId = source.CompanyId
                AND target.SiteId = source.SiteId
                AND target.TerminalId = source.TerminalId
                AND target.BusinessDate = source.BusinessDate
                AND target.TransNo = source.TransNo
                AND target.PumpId = source.PumpId
                WHEN MATCHED THEN
                    UPDATE SET DriverTag = @DriverTag,
                               VehicleTag = @VehicleTag,
                               TransCompleted = @TransCompleted,
                               NozzleID = @NozzleId,
                               ProductID = @ProductId,
                               ProductName = @ProductName,
                               Quantity = @Quantity,
                               Price = @Price,
                               Amount = @Amount,
                               TotalQuantity = @TotalQuantity,
                               TotalAmount = @TotalAmount,
                               Odometer = @Odometer,
                               PostingUserID = @PostingUserID,
                               PostingSystemDate = @PostingSystemDate
                WHEN NOT MATCHED THEN
                    INSERT (CompanyId, SiteId, TerminalId, BusinessDate, DriverTag, VehicleTag, TransCompleted, TransNo, PumpId, NozzleID, ProductID, ProductName, Quantity, Price, Amount, TotalQuantity, TotalAmount, Odometer, PostingUserID, PostingSystemDate)
                    VALUES (@CompanyId, @SiteId, @TerminalId, @BusinessDate, @DriverTag, @VehicleTag, @TransCompleted, @TransNo, @PumpId, @NozzleId, @ProductId, @ProductName, @Quantity, @Price, @Amount, @TotalQuantity, @TotalAmount, @Odometer, @PostingUserID, @PostingSystemDate);";

                using (SqlCommand command = new SqlCommand(commandText, connection)) {
                    command.Parameters.AddWithValue("@CompanyId", companyId);
                    command.Parameters.AddWithValue("@SiteId", siteId);
                    command.Parameters.AddWithValue("@TerminalId", terminalId);
                    command.Parameters.AddWithValue("@BusinessDate", businessDate);
                    command.Parameters.AddWithValue("@DriverTag", (object)driverTag ?? DBNull.Value);
                    command.Parameters.AddWithValue("@VehicleTag", (object)vehicleTag ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TransCompleted", transCompleted);
                    command.Parameters.AddWithValue("@TransNo", transNo);
                    command.Parameters.AddWithValue("@PumpId", pumpId);
                    command.Parameters.AddWithValue("@NozzleId", (object)nozzleId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProductId", (object)productId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProductName", (object)productName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@Price", price);
                    command.Parameters.AddWithValue("@Amount", amount);
                    command.Parameters.AddWithValue("@TotalQuantity", totalQuantity);
                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    command.Parameters.AddWithValue("@Odometer", odometer);
                    command.Parameters.AddWithValue("@PostingUserID", (object)postingUserId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PostingSystemDate", postingSystemDate);
                    command.ExecuteNonQuery();
                }
            }
        }


    }

}
