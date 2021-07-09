namespace PBFT.Helper.JsonObjects
{    
    //Template object used to easily load our server information from our JSON files.
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