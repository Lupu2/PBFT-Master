namespace PBFT.Helper.JsonObjects
{    
    public class JSONServer
    {
        public int ID { get; set;}
        public string IP { get; set;}

        public JSONServer(int id, string ipaddr)
        {
            ID = id;
            IP = ipaddr;
        }

        public override string ToString() => $"ID: {ID}, IPAddress: {IP}";
    }
}