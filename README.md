# UUID testing

## SQL-Server sorting is not the way dotnet Guid is sorting

[More details on how you can evaluate the sorting](SqlServerVsWindowsSorting.md).

Summary:

- SQL GUID sorting
  - 0f = most significant byte
  - 00 = least significant byte
  - `00010203-0405-0607-0908-0f0e0d0c0b0a`
- Windows GUID sorting
  - 0f = most significant byte
  - 00 = least significant byte
  - `0f0e0d0c-0b0a-0908-0706-050403020100`

**!!!But be careful, this has nothing to do with the order in which the bytes are saved in an array (for example when you do `guid.ToByteArray()`)!!!**

Also there are two different ways SQL-Server and dotnet are converting a GUID to an array. The byte-shuffling is explained on [docs.microsoft.com](https://docs.microsoft.com/en-us/archive/blogs/dbrowne/how-to-generate-sequential-guids-for-sql-server-in-net) and is implemented in this repo in [SqlGuidUtil.ToSqlSequential(...)](TestConsoleApp1\TestConsoleApp1\Util\SqlGuidUtil.cs).
