namespace PBFT.Helper
{
    public static class Quorum
    {
        public static int CalculateFailureLimit(int nodes) => (nodes - 1) / 2;
    }
}