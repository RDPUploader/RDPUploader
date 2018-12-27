


using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace RDP_Uploader
{
    /**
     * SyncManager
     * 
     * SyncManager class contains method for get rdp servers check list
     */
    class SyncManager
    {
        private static string urlGetRDPServersCheckList = "http://45.32.191.58/getRDPServersCheckList";

        private static Regex regexResponseGetRDPServerCheckList = new Regex("{\"ip\":\"([^\"]+)\",\"port\":([^,]+),\"login\":\"([^\"]+)\",\"password\":\"([^\"]+)\"}");
        
        public static List<RDP> getRDPServersCheckList()
        {
            List<RDP> rdpCheckList = new List<RDP>();

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlGetRDPServersCheckList);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string json = streamReader.ReadToEnd();

                MatchCollection matches = regexResponseGetRDPServerCheckList.Matches(json);
                
                foreach (Match match in matches)
                {
                    RDP rdp = new RDP();
                    rdp.Ip = match.Groups[1].Value;
                    rdp.Port = int.Parse(match.Groups[2].Value);
                    rdp.Login = match.Groups[3].Value;
                    rdp.Password = match.Groups[4].Value;

                    rdpCheckList.Add(rdp);
                }
               
            }
            catch (WebException e)
            {
                MessageBox mbox = new MessageBox(e.Message, "Exception");
                mbox.Show();
            }

            return rdpCheckList;
        }
    }
}
