namespace PBFT.Helper
{
    public static class Quorum
    {
        //CalculateFailureLimit calculates maximum number of faulty nodes a network can have based on the number of nodes given.
        public static int CalculateFailureLimit(int nodes) => (nodes - 1) / 2;
    }
}