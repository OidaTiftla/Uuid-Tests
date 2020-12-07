using System;
using System.Runtime.InteropServices;

namespace TestConsoleApp1.Util {

    public class WinGuidUtil {

        [DllImport("rpcrt4.dll", SetLastError = true)]
        private static extern int UuidCreateSequential(out Guid guid);

        public static Guid NewSequentialId() {
            UuidCreateSequential(out var guid);
            return guid;
        }
    }
}