using System;
using System.Runtime.InteropServices;

namespace MirrororLib
{
    static public class WindowsNetworking
    {
        #region Consts
        const int NO_ERROR = 0;
        const int RESOURCETYPE_DISK = 0x00000001;
        const int CONNECT_UPDATE_PROFILE = 0x00000001;
        #endregion

        [DllImport("Mpr.dll")]
        private static extern int WNetUseConnection(
            IntPtr hwndOwner,
            NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUserID,
            int dwFlags,
            string lpAccessName,
            string lpBufferSize,
            string lpResult
            );

        [DllImport("Mpr.dll")]
        private static extern int WNetCancelConnection2(
            string lpName,
            int dwFlags,
            bool fForce
            );

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope = 0;
            public int dwType = 0;
            public int dwDisplayType = 0;
            public int dwUsage = 0;
            public string lpLocalName = "";
            public string lpRemoteName = "";
            public string lpComment = "";
            public string lpProvider = "";
        }

        public static bool ConnectToRemote(string remoteUNC, string username, string password)
        {
            NETRESOURCE nr = new NETRESOURCE();
            nr.dwType = RESOURCETYPE_DISK;
            nr.lpRemoteName = remoteUNC;

            return (WNetUseConnection(IntPtr.Zero, nr, password, username, 0, null, null, null) == NO_ERROR);
        }

        public static bool DisconnectRemote(string remoteUNC)
        {
            return (WNetCancelConnection2(remoteUNC, CONNECT_UPDATE_PROFILE, false) == NO_ERROR);
        }
    }
}
