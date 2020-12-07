using MassTransit;
using System;

namespace TestConsoleApp1.Util {

    public class NewIdGuidUtil {

        public static Guid NewSequentialId() {
            return NewId.NextGuid();
        }
    }
}