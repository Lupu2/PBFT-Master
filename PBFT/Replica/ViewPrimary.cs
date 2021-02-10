using System.Diagnostics.Tracing;

namespace PBFT.Replica
{
    public class ViewPrimary
    {
        public int ServID {get; set;}
        public int ViewNr {get; set;}

        public ViewPrimary(int id, int vnr)
        {
            ServID = id;
            ViewNr = vnr;
        }

        public void NextPrimary(int numberOfReplicas)
        {
            ViewNr++;
            ServID = ViewNr % numberOfReplicas;
        }
    }
}