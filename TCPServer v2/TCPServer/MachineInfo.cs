using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Management;
using System.Security.Cryptography;

namespace TCPServer {
    class MachineInfo {
        static public string GenerateProductKey(string APP, string cpuId, string motherboardId, string hardDiskId) {
            string key = APP + cpuId + motherboardId + hardDiskId;
            byte[] data = System.Text.Encoding.UTF8.GetBytes(key); using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider()) {
                byte[] hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 25);
            }
        }

        static public bool ValidateProductKey(string APP, string productKey, string cpuId, string motherboardId, string hardDiskId) {
            string expectedProductKey = GenerateProductKey(APP, cpuId, motherboardId, hardDiskId);
            return (productKey == expectedProductKey);
        }

        static public string GetCpuId() {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            try {
                foreach (ManagementObject mo in moc) {
                    if (cpuInfo == "") {
                        cpuInfo = mo.Properties["processorID"].Value.ToString();
                    }
                }
            }
            catch (Exception ex) {
                cpuInfo = "0000";
            }

            return cpuInfo;
        }

        static public string GetMotherboardId() {
            string motherboardInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_baseboard");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc) {
                if (motherboardInfo == "") {
                    motherboardInfo = mo.Properties["SerialNumber"].Value.ToString();
                }
            }

            return motherboardInfo;
        }

        static public string GetHardDiskId() {
            string hardDiskInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_logicaldisk");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc) {
                if (mo["DriveType"].ToString() == "3") {
                    hardDiskInfo = mo["VolumeSerialNumber"].ToString();
                }
            }

            return hardDiskInfo;
        }

        static public string GetProductID(string APP, string MachineID) {
            string input = MachineID;// "MachineID:[BFEBFBFF000806EA,NA,14D0E158];";

            // Find the index of the first '[' character and the last ']' character
            int startIndex = input.IndexOf('[');
            int endIndex = input.LastIndexOf(']');

            // Extract the substring between the '[' and ']' characters
            string dataString = input.Substring(startIndex + 1, endIndex - startIndex - 1);

            // Split the data string into three parts using ',' as the delimiter
            string[] parts = dataString.Split(',');

            // Trim any whitespace from each part
            string s1 = parts[0].Trim();
            string s2 = parts[1].Trim();
            string s3 = parts[2].Trim();
            return GenerateProductKey(APP, s1, s2, s3);
        }
    }
}

