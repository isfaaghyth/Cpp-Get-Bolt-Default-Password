/*/////////////////////////////////////////////////////////////////////////////////
 * THIS CODE IS PROVIDED AS IS WITH NO WARRENTY.
 * IT IS JUST A PROOF OF CONCEPT TO SHARE KNOWLEDGE 
 * 
 * Author: Mohammad Said Hefny
 * Email: Mohammad.Hefny@Gmail.com
 */
////////////////////////////////////////////////////////////////////////////////

/*/////////////////////////////////////////////
 * ManagedWifi Licence can be found @ http://managedwifi.codeplex.com/license 
 * 
 */
///////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using NativeWifi;

namespace GoogleLocationQuery
{
    public partial class MainForm : Form
    {

        string Json = "";
        string JsonTest = @"{ ""host"" : ""Test"", ""radio_type"" : ""unknown"", ""request_address"" : true, ""version"" : ""1.1.0"", ""wifi_towers"" : [ {""mac_address"" :""00-1D-19-70-AE-00"", ""signal_strength"" :-77, ""ssid"" : ""voda"" },{""mac_address"" :""00-1D-A2-62-5E-11"", ""signal_strength"" :-69, ""ssid"" : ""LINKdotNET-Local"" },{""mac_address"" :""00-1D-7E-B2-0D-43"", ""signal_strength"" :-91, ""ssid"" : ""HNF"" },{""mac_address"" :""22-1E-8C-E4-0C-45"", ""signal_strength"" :-86, ""ssid"" : ""wireless"" },{""mac_address"" :""00-1D-A2-62-5E-10"", ""signal_strength"" :-66, ""ssid"" : ""LINKdotNET-Guest"" },{""mac_address"" :""00-1E-2A-DA-13-D5"", ""signal_strength"" :-82, ""ssid"" : ""NETGEAR"" },{""mac_address"" :""00-0C-C3-53-03-75"", ""signal_strength"" :-79, ""ssid"" : ""M.RaSHeD"" },{""mac_address"" :""00-22-57-1F-C9-E7"", ""signal_strength"" :-96, ""ssid"" : ""pop"" },{""mac_address"" :""02-1F-3C-00-14-8E"", ""signal_strength"" :-94, ""ssid"" : ""101"" }]}";

        public MainForm()
        {
            InitializeComponent();
            textSent.Text = JsonTest;
        }

        private void btnWifiList_Click(object sender, EventArgs e)
        {
            try
            {
                Json = @"{ ""host"" : ""Test"", ""radio_type"" : ""unknown"", ""request_address"" : true, ""version"" : ""1.1.0"", ""wifi_towers"" : [ ";
                lstWifi.Items.Clear();

                WlanClient wLanClient = new NativeWifi.WlanClient();
                if (wLanClient.Interfaces.Length == 0)
                {
                    MessageBox.Show("No Wifi Interfaces found.");
                    return;
                }
                Wlan.WlanBssEntry[] lstWlanBss = wLanClient.Interfaces[0].GetNetworkBssList(); //.GetAvailableNetworkList(Wlan.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles);
                if (lstWlanBss == null)
                {
                    MessageBox.Show("No networks has been detected.");
                    return;
                }

                System.Text.StringBuilder SB = new StringBuilder();
                foreach (var oWlan in lstWlanBss)
                {
                    ListViewItem lstItem = lstWifi.Items.Add(System.Text.Encoding.UTF8.GetString(oWlan.dot11Ssid.SSID));

                    lstItem.SubItems.Add(CalculateSignalQuality(oWlan.linkQuality).ToString());
                    string MAC = ConvertToMAC(oWlan.dot11Bssid);
                    lstItem.SubItems.Add(MAC);
                    SB.Append(@"{""mac_address"" :""");
                    SB.Append(MAC);
                    SB.Append(@"""");
                    SB.Append(@", ""signal_strength"" :");
                    SB.Append(CalculateSignalQuality(oWlan.linkQuality).ToString());
                    SB.Append(@", ""ssid"" : """);
                    string SSID = System.Text.Encoding.UTF8.GetString(oWlan.dot11Ssid.SSID, 0, (int)oWlan.dot11Ssid.SSIDLength);
                    if ((SSID.Length == 0) || SSID[0]==0) SSID = "NA";
                    SB.Append(SSID);
                    SB.Append(@""" },");
                }
                Json += SB.ToString().Substring(0, SB.Length - 1); // copy all except last ","
                Json += "]}";

                textSent.Text = Json;
            }
            catch
            {
                MessageBox.Show("Could not retrieve Wifi information. However you can still make manual query to Google.");
            }

        }

        private void btnGetLocation_Click(object sender, EventArgs e)
        {
            try
            {
                txtAddress.Text = "";
                txtLatitude.Text = "";
                txtLongitude.Text = "";
                txtResponse.Text = "";

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(@"http://www.google.com/loc/json");
                request.ContentType = "application/json; charset=utf-8";
                request.Accept = "application/json, text/javascript, */*";
                request.Method = "POST";

                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(textSent.Text);
                }
                WebResponse response = request.GetResponse();
                Stream stream = response.GetResponseStream();
                string json = "";

                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        json += reader.ReadLine();
                    }
                }

                txtResponse.Text = json;
                ParseLocation(json);
            }
            catch
            {
                MessageBox.Show("Could not retrieve location info");
            }
        }


        #region "Helper Functions"


        /// <summary>
        /// calculate the RSSI signal strength.
        /// </summary>
        /// <param name="Percentage">
        /// /// A percentage value that represents the signal quality of the network.
        /// This field contains a value between 0 and 100.
        /// A value of 0 implies an actual RSSI signal strength of -100 dbm.
        /// A value of 100 implies an actual RSSI signal strength of -50 dbm.
        /// You can calculate the RSSI signal strength value for values between 1 and 99 using linear interpolation.
        ///</param>
        /// <returns></returns>
        int CalculateSignalQuality(uint Percentage)
        {
            int RSSI = (int)Percentage / 2 - 100;

            return RSSI;
        }

        /// <summary>
        /// Parse Json response from google
        /// </summary>
        /// <param name="Response"></param>
        void ParseLocation(string Response)
        {
            Response = Response.ToLower();
            int nIndex = Response.IndexOf(@"""latitude"":") + 11;  // 11 = length of "latitude":
            txtLatitude.Text = Response.Substring(nIndex, Response.IndexOf(",", nIndex) - nIndex);

            nIndex = Response.IndexOf(@"""longitude"":") + 12;  // 12 = length of "longitude":
            txtLongitude.Text = Response.Substring(nIndex, Response.IndexOf(",", nIndex) - nIndex);

            nIndex = Response.IndexOf(@"""address"":{") + 11;  // 11 = length of "address":{
            txtAddress.Text = Response.Substring(nIndex, Response.IndexOf("},", nIndex) - nIndex);

        }


        string ConvertToMAC(byte[] MAC)
        {
            string strMAC = "";
            for (int index = 0; index < 6; index++)

                strMAC += MAC[index].ToString("X2") + "-";
            return strMAC.Substring(0, strMAC.Length - 1); // return all except last "-"
        }


        #endregion
    }
}
