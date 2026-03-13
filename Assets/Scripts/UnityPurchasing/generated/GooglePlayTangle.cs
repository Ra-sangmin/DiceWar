// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("3r1fyPrTsZrppLO+H51khRPhmSG5QD4RdYetNv0+gyNx5YBDiT7pYr0+MD8PvT41Pb0+Pj+HFszrWxWDTQttGHrwRcMGbgrbwZxLlSsNFDuW/Fxdp5lM9KHNKb1RqiF56kMDKbhE8PKBh7d4OFojLfullM9ufPIyOSqFYQwxej9lhBIIr+bVIwoXPdIPvT4dDzI5NhW5d7nIMj4+Pjo/PAd3sNInxB+yONHtEjL+aJbhaSoyBQct41aDIoIX+EKEz3ANzzFbUm87gIrbMzX3mDMw9UW1IKTBMamLpLxkKSbPSLwddZ1Q6gcH1ndMhdl64dmrhSuvfwhtw4dBKfmskpzphzpYm17Swwqnm9ryc+rxsk/nPnrgMHeLzkbRVcsXOD08Pj8+");
        private static int[] order = new int[] { 7,10,10,10,5,6,12,7,10,13,13,12,12,13,14 };
        private static int key = 63;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
