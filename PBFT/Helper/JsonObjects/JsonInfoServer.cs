namespace PBFT.Helper.JsonObjects
{    
    public class JSONInfoServer
    {
        public int ID { get; set; }
        public string IP { get; set; }

        public JSONInfoServer(int id, string ipaddr)
        {
            ID = id;
            IP = ipaddr;
        }

        public override string ToString() => $"ID: {ID}, IPAddress: {IP}";
    }
}