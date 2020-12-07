using MassTransit;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TestConsoleApp1.Util;

namespace TestConsoleApp1 {

    internal class Program {

        private static void Main(string[] args) {
            evalSortingAlgorithms();
            evalNewSequentialIds();
            evalNewId();
            evalNewIdSequence();
        }

        #region eval sorting algorithms

        private static void evalSortingAlgorithms() {
            var range = Enumerable.Range(0, 16);
            var byteRange = range.Select(i => {
                var bytes = Enumerable.Range(0, 16).Select(_ => (byte)0).ToArray();
                bytes[i] = 1;
                return new { i, bytes };
            }).ToList();

            Console.WriteLine("byteRange:");
            foreach (var o in byteRange) {
                Console.WriteLine($"{o.i}: {toString(o.bytes)} ({new Guid(o.bytes)})");
            }

            var winRange = byteRange.Select(o => new { o.i, guid = new Guid(o.bytes) }).OrderBy(o => o.guid).ToList();
            Console.WriteLine("winRange:");
            foreach (var o in winRange) {
                Console.WriteLine($"{o.i}: {toString(o.guid.ToByteArray())} ({o.guid})");
            }

            var sqlRange = byteRange.Select(o => new { o.i, guid = new SqlGuid(o.bytes) }).OrderBy(o => o.guid).ToList();
            Console.WriteLine("sqlRange:");
            foreach (var o in sqlRange) {
                Console.WriteLine($"{o.i}: {toString(o.guid.ToByteArray())} ({o.guid}) swap = {toString(SqlGuidUtil.ToSqlSequential(o.guid.ToByteArray()))}");
            }

            var stringRange = byteRange.Select(o => new { o.i, str = toStringGuid(o.bytes) }).OrderBy(o => o.str).ToList();
            Console.WriteLine("stringRange:");
            foreach (var o in stringRange) {
                Console.WriteLine($"{o.i}: {o.str}");
            }

            var winStringRange = stringRange.Select(o => new { o.i, guid = new Guid(o.str) }).OrderBy(o => o.guid).ToList();
            Console.WriteLine("winStringRange:");
            foreach (var o in winStringRange) {
                Console.WriteLine($"{o.i}: {toString(o.guid.ToByteArray())} ({o.guid})");
            }

            var sqlStringRange = stringRange.Select(o => new { o.i, guid = new SqlGuid(o.str) }).OrderBy(o => o.guid).ToList();
            Console.WriteLine("sqlStringRange:");
            foreach (var o in sqlStringRange) {
                Console.WriteLine($"{o.i}: {toString(o.guid.ToByteArray())} ({o.guid}) swap = {toString(SqlGuidUtil.ToSqlSequential(o.guid.ToByteArray()))}");
            }
        }

        #endregion eval sorting algorithms

        #region eval sequential IDs

        private static void evalNewSequentialIds() {
            Console.WriteLine($"{nameof(evalNewSequentialIds)}: generating ids");
            var range = Enumerable.Range(0, 512);
            var winUuidSeqRange = range
                //.Select(delay(42))
                .Select(x => { if (x % 32 == 0) { Thread.Sleep(10); } return x; })
                .Select(i => new { i, guid = WinGuidUtil.NewSequentialId() })
                .Select(x => (
                    x.i,
                    x.guid,
                    sqlGuid: SqlGuidUtil.ToSqlSequential(x.guid),
                    sqlByteGuid: new SqlGuid(x.guid.ToByteArray())
                )).ToList();
            var combSeqGuidRange = range
                //.Select(delay(42))
                .Select(x => { if (x % 32 == 0) { Thread.Sleep(10); } return x; })
                .Select(i => new { i, guid = CombGuidUtil.NewSequentialId() })
                .Select(x => (
                    x.i,
                    x.guid,
                    sqlGuid: SqlGuidUtil.ToSqlSequential(x.guid),
                    sqlByteGuid: new SqlGuid(x.guid.ToByteArray())
                )).ToList();
            var newIdSeqGuidRange = range
                //.Select(delay(42))
                .Select(x => { if (x % 32 == 0) { Thread.Sleep(10); } return x; })
                .Select(i => new { i, guid = NewIdGuidUtil.NewSequentialId() })
                .Select(x => (
                    x.i,
                    x.guid,
                    sqlGuid: SqlGuidUtil.ToSqlSequential(x.guid),
                    sqlByteGuid: new SqlGuid(x.guid.ToByteArray())
                )).ToList();

            Console.WriteLine($"{nameof(evalNewSequentialIds)}: sorting");
            Console.WriteLine();
            Console.WriteLine("SQL GUID sorting 0f = most significant byte, 00 = least significant byte: 00010203-0405-0607-0908-0f0e0d0c0b0a");
            Console.WriteLine("Windows GUID sorting 0f = most significant byte, 00 = least significant byte: 0f0e0d0c-0b0a-0908-0706-050403020100");
            Console.WriteLine("!!!But be careful, this has nothing to do with the order in which the bytes are saved in an array (for example when you do guid.ToByteArray())!!!");
            Console.WriteLine();
            evalSortNewSequentialIds("winUuidSeqRange", winUuidSeqRange);
            Console.WriteLine("winUuidSeqRange: Windows GUID and SQL GUID is sorted the same way, but only if it is converted correctly and the GUIDs are only generated on one machine and without reboot of the PC.");
            Console.WriteLine("- The format is: t3t2t1t0-t5t4-t7t6-rrrr-m0m1m2m3m4m5.");
            Console.WriteLine("  - m0 - m5: MAC-address (m0-m1-m2-m3-m4-m5 = [supplier id]-[individual id])");
            Console.WriteLine("  - t7 - t0: most to least significant byte of timestamp (60-bits of 100ns intervals since 15.10.1582) (bits 4-7 of t7 are a version indicator and is 0b0001 for this version)");
            Console.WriteLine("  - rrrr: clock sequence (14-bits random initial number if the system cannot guarantee that the time of the system was not changed) (the bits 14-15 are always 0b10 for this version)");
            Console.WriteLine("  - If you'd like to dive deeper into this topic you may find the answer to the above '?' in https://tools.ietf.org/html/rfc4122.");
            Console.WriteLine();
            evalSortNewSequentialIds("combSeqGuidRange", combSeqGuidRange);
            Console.WriteLine("combSeqGuidRange:");
            Console.WriteLine("- Windows GUID is sorting it completely wrong (because it starts sorting by the random numbers first, and the timestamp is on the least significant position).");
            Console.WriteLine("- SQL GUID is sorting it correct if enough time has passed between each generation of a GUID (if the GUIDs are generated very fast, the 6 bytes for the timestamp are equal and a random number decides about the ordering).");
            Console.WriteLine("- There may be a different ordering between sqlGuid (with correct conversion from Windows GUID to SQL GUID) and sqlByteGuid (without correct conversion from Windows GUID to SQL GUID).");
            Console.WriteLine("  - This happens if the GUIDs are generated very fast and therefore the timestamp is the same.");
            Console.WriteLine("  - Then the next two bytes of random bits are sorted equally but afterwards some bytes are swapped.");
            Console.WriteLine("  - If the first two random bytes are equal, then the sorting may differ (between sqlGuid and sqlByteGuid) based on the following random bytes.");
            Console.WriteLine("- This approach has no effect if the GUIDs are generated on the same machine or on separate machines.");
            Console.WriteLine("- The format is: rrrrrrrr-rrrr-rrrr-rrrr-t5t4t3t2t1t0.");
            Console.WriteLine("  - rr: random byte");
            Console.WriteLine("  - t5 - t0: most to least significant byte of timestamp");
            Console.WriteLine();
            evalSortNewSequentialIds("newIdSeqGuidRange", newIdSeqGuidRange);
            Console.WriteLine("newIdSeqGuidRange:");
            Console.WriteLine("- Windows GUID is sorting it completely wrong (because it starts sorting by the MAC-address and the thread-number first, and the timestamp is on the least significant position).");
            Console.WriteLine("  - If the GUIDs are generated very fast, there may be a partially correct order, because 6 of the 8 timestamp bytes (which are at the end and are the most significant part of the timestamp) are the some for some time until the change again.");
            Console.WriteLine("- SQL GUID is sorting it completely correct (because it starts sorting by the timestamp and afterwards by the mixed number of MAC-address and thread-number).");
            Console.WriteLine("- The default format is (without process id): m4m5s1s0-m2m3-m0m1-t1t0-t7t6t5t4t3t2.");
            Console.WriteLine("  - m0 - m5: MAC-address (m0-m1-m2-m3-m4-m5 = [supplier id]-[individual id])");
            Console.WriteLine("  - s1 - s0: most to least significant byte of sequence counter (16-bits)");
            Console.WriteLine("  - t7 - t0: most to least significant byte of timestamp (64-bits)");
            Console.WriteLine("- The format with process id is: p0p1s1s0-m2m3-m0m1-t1t0-t7t6t5t4t3t2.");
            Console.WriteLine("  - nnnn: thread number");
            Console.WriteLine("  - m0 - m5: MAC-address (m0-m1-m2-m3-m4-m5 = [supplier id]-[individual id]) (m4 and m5 are not used)");
            Console.WriteLine("  - p1 - p0: most to least significant byte of process id (16-bits)");
            Console.WriteLine("  - s1 - s0: most to least significant byte of sequence counter (16-bits)");
            Console.WriteLine("  - t7 - t0: most to least significant byte of timestamp (64-bits)");
            Console.WriteLine();
        }

        private static void evalSortNewSequentialIds(string name, List<(int i, Guid guid, SqlGuid sqlGuid, SqlGuid sqlByteGuid)> seqRange) {
            Console.WriteLine($"{name}:");

            var i = 0;
            var guidOrder = seqRange.OrderBy(x => x.guid).ToDictionary(x => x.i, x => i++);
            i = 0;
            var sqlGuidOrder = seqRange.OrderBy(x => x.sqlGuid).ToDictionary(x => x.i, x => i++);
            i = 0;
            var sqlByteGuidOrder = seqRange.OrderBy(x => x.sqlByteGuid).ToDictionary(x => x.i, x => i++);

            Console.WriteLine($"|   i | guid | sqlGuid | sqlByteGuid | {"guid",36} |");
            foreach (var x in seqRange) {
                Console.WriteLine($"| {x.i,3} | {guidOrder[x.i],4} | {sqlGuidOrder[x.i],7} | {sqlByteGuidOrder[x.i],11} | {x.guid,36} |");
            }
        }

        #endregion eval sequential IDs

        #region eval NewId

        private static void evalNewId() {
            Console.WriteLine($"{nameof(evalNewId)}:");
            ITickProvider tickProvider = new MyTickProvider();
            IWorkerIdProvider workerIdProvider = new MyWorkerIdProvider();
            var genWithoutProcess = new NewIdGenerator(tickProvider, workerIdProvider);
            IProcessIdProvider processIdProvider = new MyProcessIdProvider();
            var genWithProcess = new NewIdGenerator(tickProvider, workerIdProvider, processIdProvider);

            Console.WriteLine("genWithoutProcess:");
            for (int i = 0; i < 10; ++i) {
                Console.WriteLine($"- {genWithoutProcess.NextGuid()}");
            }
            Console.WriteLine("genWithProcess:");
            for (int i = 0; i < 10; ++i) {
                Console.WriteLine($"- {genWithProcess.NextGuid()}");
            }
        }

        private class MyTickProvider : ITickProvider {

            public long Ticks {
                get {
                    return 0x7d7c7b7a79787776;
                }
            }
        }

        private class MyWorkerIdProvider : IWorkerIdProvider {

            public byte[] GetWorkerId(int index) {
                return new byte[] { 0x70, 0x71, 0x72, 0x73, 0x74, 0x75 };
            }
        }

        private class MyProcessIdProvider : IProcessIdProvider {
            private readonly int processId_;
            private readonly byte[] processIdBytes_;

            public MyProcessIdProvider() {
                this.processId_ = Process.GetCurrentProcess().Id;
                this.processIdBytes_ = BitConverter.GetBytes(this.processId_);

                Console.WriteLine($"processId: {this.processId_}");
                Console.WriteLine($"processIdBytes: {toString(this.processIdBytes_)}");
            }

            public byte[] GetProcessId() {
                //return new byte[] { 0x7e, 0x7f };
                return this.processIdBytes_;
            }
        }

        #endregion eval NewId

        #region eval NewId sequence sorting

        private static void evalNewIdSequence() {
            Console.WriteLine($"{nameof(evalNewIdSequence)}:");

            var array = NewId.Next(512);
            var ids = array.Select(x => new SqlGuid(x.ToGuid().ToByteArray())).ToList();
            Console.WriteLine("| i   | guid                                 |");
            Console.WriteLine("| --- | ------------------------------------ |");
            foreach (var x in ids) {
                var originalIndex = ids.IndexOf(x);
                Console.WriteLine($"| {originalIndex,3} | {x,36} |");
            }

            Console.WriteLine("Sorted:");
            var ordered = ids.OrderBy(x => x).ToList();
            Console.WriteLine("| i   | guid                                 |");
            Console.WriteLine("| --- | ------------------------------------ |");
            foreach (var x in ordered) {
                var originalIndex = ids.IndexOf(x);
                Console.WriteLine($"| {originalIndex,3} | {x,36} |");
            }
            Console.WriteLine("Note that the IDs are not sorted in the order they were created.");
            Console.WriteLine();
            Console.WriteLine("Note that NewId does not match the perfect shape for MSSQL clustered index: https://github.com/phatboyg/NewId/issues/16.");
        }

        #endregion eval NewId sequence sorting

        #region helpers

        private static Func<int, int> delay(int seed) {
            var rnd = new Random(seed);
            return x => { Thread.Sleep(rnd.Next(0, 10)); return x; };
        }

        private static string toString(byte[] bytes) {
            var sb = new StringBuilder();
            sb.Append("bytes:[");
            foreach (var b in bytes) {
                sb.Append(string.Format("{0:x2}", b));
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string toCSharp(byte[] bytes) {
            var sb = new StringBuilder();
            sb.Append("new byte[] { ");
            foreach (var b in bytes) {
                sb.Append(string.Format("0x{0:x2}", b));
                sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string toStringGuid(byte[] bytes) {
            var regex = new Regex(@"(?<a>\d\d\d\d\d\d\d\d)-?(?<b>\d\d\d\d)-?(?<c>\d\d\d\d)-?(?<d>\d\d\d\d)-?(?<e>\d\d\d\d\d\d\d\d\d\d\d\d)");
            var m = regex.Match(string.Join("", bytes.Select(x => string.Format("{0:x2}", x))));
            if (!m.Success) {
                throw new Exception("Not a Guid");
            }
            return $"{m.Groups["a"].Value}-{m.Groups["b"].Value}-{m.Groups["c"].Value}-{m.Groups["d"].Value}-{m.Groups["e"].Value}";
        }

        #endregion helpers
    }
}