using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Windows.Forms;

using System.Globalization;

namespace TCPServer {
    public class DataField {
        public string Header { get; set; }
        public int Seq { get; set; }
        public string CompanyID { get; set; }
        public string SiteID { get; set; }
        public int TerminalID { get; set; }
        public int TransNo { get; set; }
        public string TransCompleted { get; set; }
        public int PumpID { get; set; }
        public int NozzleID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal Tot_Amount { get; set; }
        public decimal Tot_Volume { get; set; }
        public decimal Cal_Tot_Amount { get; set; }
        public decimal Cal_Tot_Volume { get; set; }

        public string MOP { get; set; }
        public string MOPCardID { get; set; }
        public string SupervisorTag { get; set; }
        public string AttendantTag { get; set; }
        public string CustomerTag { get; set; }
        public string VehicleTag { get; set; }

        public int Odometer { get; set; }
        public int EngineHour { get; set; }
        public string TransferTo { get; set; }
        public int TankID { get; set; }

        public string TranType { get; set; }
        public string TranMode { get; set; }
        public string Loc_Longitude { get; set; }
        public string Loc_Latitude { get; set; }
        public string OverridePurpose { get; set; }

        public decimal TankReadingBefore_H { get; set; }
        public decimal TankReadingBefore_V { get; set; }
        public decimal TankReadingAfter_H { get; set; }
        public decimal TankReadingAfter_V { get; set; }

        public string TranSignature { get; set; }
        public string PostedToServer { get; set; }
        public string PostedToFuelokey { get; set; }
        public string ExportedToUSB { get; set; }

        public int PrintCount { get; set; }
        public string JobOrderID { get; set; }
        public int CompartemenID { get; set; }
        public string SynchToAX { get; set; }
    }

    public class DataSetParser {
        TextBox tbLog;

        private static int ParseInt(string value)
            => int.TryParse(value,out int r) ? r : 0;

        private static decimal ParseDecimal(string value)
            => decimal.TryParse(value,NumberStyles.Any,CultureInfo.InvariantCulture,out decimal r)
               ? r : 0m;

        public Boolean IsValidMessage(string inputMessage) {
            Boolean R = false;
            string[] fields = inputMessage.Split(new string[] { "-:-" },StringSplitOptions.None);
            if(fields.Length == 45) {
                R = true;
            }
            return R;
        }

        public DataField ParseInputMessage(string inputMessage,ref TextBox ATextBoxLog) {
            tbLog = ATextBoxLog;
            string[] fields = inputMessage.Split(new[] { "-:-" },StringSplitOptions.None);

            DataField data = new DataField();

            try {
                data.Header = fields[0];
                data.Seq = ParseInt(fields[1]);
                data.CompanyID = fields[2];
                data.SiteID = fields[3];
                data.TerminalID = ParseInt(fields[4]);
                data.TransNo = ParseInt(fields[5]);
                data.TransCompleted = fields[6];
                data.PumpID = ParseInt(fields[7]);
                data.NozzleID = ParseInt(fields[8]);
                data.ProductID = ParseInt(fields[9]);
                data.ProductName = fields[10];

                data.Quantity = ParseDecimal(fields[11]);
                data.UnitPrice = ParseDecimal(fields[12]);
                data.Amount = ParseDecimal(fields[13]);
                data.Tot_Amount = ParseDecimal(fields[14]);
                data.Tot_Volume = ParseDecimal(fields[15]);
                data.Cal_Tot_Amount = ParseDecimal(fields[16]);
                data.Cal_Tot_Volume = ParseDecimal(fields[17]);

                data.MOP = fields[18];
                data.MOPCardID = fields[19];
                data.SupervisorTag = fields[20];
                data.AttendantTag = fields[21];
                data.CustomerTag = fields[22];
                data.VehicleTag = fields[23];

                data.Odometer = ParseInt(fields[24]);
                data.EngineHour = ParseInt(fields[25]);
                data.TransferTo = fields[26];
                data.TankID = ParseInt(fields[27]);

                data.TranType = fields[28];
                data.TranMode = fields[29];
                data.Loc_Longitude = fields[30];
                data.Loc_Latitude = fields[31];
                data.OverridePurpose = fields[32];

                data.TankReadingBefore_H = ParseDecimal(fields[33]);
                data.TankReadingBefore_V = ParseDecimal(fields[34]);
                data.TankReadingAfter_H = ParseDecimal(fields[35]);
                data.TankReadingAfter_V = ParseDecimal(fields[36]);

                data.TranSignature = fields[37];
                data.PostedToServer = fields[38];
                data.PostedToFuelokey = fields[39];
                data.ExportedToUSB = fields[40];
                data.PrintCount = ParseInt(fields[41]);
                data.JobOrderID = fields[42];
                data.CompartemenID = ParseInt(fields[43]);
                data.SynchToAX = fields[44];
            }
            catch(Exception ex) {
                LogAdd("Parser error: " + ex.Message);
            }

            return data;
        }

        private void LogAdd(string message) {
            tbLog?.AppendText(message + Environment.NewLine);
        }
    }



  

}
