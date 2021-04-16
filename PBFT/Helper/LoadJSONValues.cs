using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Newtonsoft.Json;
using PBFT.Helper.JsonObjects;

namespace PBFT.Helper
{

    public static class LoadJSONValues
    {
        public static async Task<(int, string)> GetServerData(string filepath, int id)
        {
            var serv = await LoadJSONFileServer(filepath, id);
            return (serv.ID, serv.IP);
        }
        
        public static async Task<JSONInfoServer> LoadJSONFileServer(string filepath, int actualID)
        {
            using (StreamReader sr = new StreamReader(filepath))
            {
                var jsonValue = await sr.ReadToEndAsync();
                var jsonServ = JsonConvert.DeserializeObject<List<JSONInfoServer>>(jsonValue);
                var serv = jsonServ.Single(s => s.ID == actualID);
                return serv;
            }
        }

        public static async Task<CDictionary<int,string>> LoadJSONFileContent(string filepath)
        {
            using (StreamReader sr = new StreamReader(filepath))
            {
                var jsonValue = await sr.ReadToEndAsync();
                var jsonServers = JsonConvert.DeserializeObject<List<JSONInfoServer>>(jsonValue);
                CDictionary<int, string> servInfo = new CDictionary<int, string>();
                foreach (var servobj in jsonServers) servInfo[servobj.ID] = servobj.IP;
                return servInfo;
            }
        }
    }
    
}