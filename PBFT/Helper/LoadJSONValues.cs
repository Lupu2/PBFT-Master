using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace PBFT.Helper
{
    public class JSONServerObj
    {
        public int ID { get; set; }
        public string IP { get; set; }

        public JSONServerObj(int id, string ipaddr)
        {
            ID = id;
            IP = ipaddr;
        }

        public override string ToString() => $"ID: {ID}, IPAddress: {IP}";
    }
    
    public static class LoadJSONValues
    {
        public static async Task<(int, string)> GetServerData(string filepath, int id)
        {
            var serv = await LoadJSONFile(filepath, id);
            return (serv.ID, serv.IP);
        }
        
        public static async Task<JSONServerObj> LoadJSONFile(string filepath, int actualID)
        {
            using (StreamReader sr = new StreamReader(filepath))
            {
                var jsonValue = await sr.ReadToEndAsync();
                var jsonServ = JsonConvert.DeserializeObject<List<JSONServerObj>>(jsonValue); //change to list
                var serv = jsonServ.Single(s => s.ID == actualID);
                return serv;
            }
            
        }
    }
    
}