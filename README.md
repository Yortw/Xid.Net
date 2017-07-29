# Xid.Net - Globally Unique ID Generator
This project is a .Net port (with modifications) of [https://github.com/rs/xid](https://github.com/rs/xid) by [Olivier Poitrey](https://github.com/rs). Significant parts of the code and this readme are based on that project.

Xid.Net is a globally unique id generator library, ready to be used safely and   directly in your code. It provides an interface *similar* to System.Guid but generates ids which are smaller (12 bytes vs 16 in raw format, 20 vs 36 when converted to a string). Generating Xids is faster than Guids, often significantly.

Xid uses the Mongo Object ID algorithm to generate globally unique ids with a different serialization (base32 vs base64) to make it shorter when transported as a string: [https://docs.mongodb.org/manual/reference/object-id/](https://docs.mongodb.org/manual/reference/object-id/).

[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Yortw/Xid.Net/blob/master/LICENSE) 
[![Build status](https://ci.appveyor.com/api/projects/status/966nb6l0q7j4bomm?svg=true)](https://ci.appveyor.com/project/Yortw/xid-net)
[![NuGet Badge](https://buildstats.info/nuget/Xid.Net)](https://www.nuget.org/packages/Xid.Net/)

# Platforms
* .Net 4.0+
* .Net Standard 1.3

# Install
```powershell
    PM> Install-Package Xid.Net
```

# Usage/API
Creating a Xid;
```c#
    var id = Xid.NewXid();
```

Converting to Xid to a string;
```c#
    var id = Xid.NewXid();
    var idString = id.ToString();
```

Converting a string to a Xid (TryParse method also provided);
```c#
    var idString = "9m4e2mr0ui3e8a215n4g";
    var id = Xid.Parse(idString);
```

Converting to Xid to a byte array;
```c#
    var id = Xid.NewXid();
    var bytes = id.ToBytes();
```

or
```c#
    var id = Xid.NewXid();
    var preallocatedByteArray = new byte[12];
    var bytes = id.ToBytes(preallocatedByteArray);
```

Getting components of a Xid;
```c#
    var xid = Xid.NewXid();
    DateTime timestamp = xid.GetTimeStamp();
    byte[] machineId = xid.GetMachineId();
    UInt16 processId = xid.GetProcessId();
    int counter = xid.GetCounter();
```

Also supported/other features of .Net implementation;
* IFormattable
* IComparable
* IComparable<Xid>
* IEquatable<Xid>
* == > < >= <= and != operators
* Correctly implemented Equals and GetHashCode overrides
* Xid.NewXid is thread-safe

# Xid Format, Implementation & Comparison to other UUIDs
The format of a Xid is;

* 4-byte value representing the seconds since the Unix epoch,
* 3-byte machine identifier,
* 2-byte process id, and
* 3-byte counter, starting with a random value.

The binary representation of the id is compatible with Mongo 12 byte Object IDs. The string representation uses base32 hex (without padding) for better space efficiency when stored in that form (20 bytes). The hex variant of base32 is used to retain the sortable property of the id.

Xid doesn't use base64 because case sensitivity and the 2 non alphanum chars may be an issue when transported as a string between various systems. Base36 wasn't used because it's not standard, the resulting size is not predictable (not bit aligned), and it would not remain sortable. To validate a base32 xid, expect a 20 character long, all lowercase sequence using characters in the range of a to v and 0 to 9 numbers ([0-9a-v]{20}).

UUIDs are 16 bytes (128 bits) and 36 chars as string representation. Twitter Snowflake ids are 8 bytes (64 bits) but require machine/data-center configuration and/or central generator servers. Xid stands in between at 12 bytes (96 bits) with a more compact URL-safe string representation (20 chars). No configuration or central generator server is required so it can be used directly in application code.

| Name        | Binary Size | String Size    | Features
|-------------|-------------|----------------|----------------
| [UUID](https://en.wikipedia.org/wiki/Universally_unique_identifier)      | 16 bytes    | 36 chars       | configuration free, not sortable
| [shortuuid](https://github.com/stochastic-technologies/shortuuid) | 16 bytes    | 22 chars       | configuration free, not sortable
| [Snowflake](https://blog.twitter.com/2010/announcing-snowflake) | 8 bytes     | up to 20 chars | needs machine/DC configuration, needs central server, sortable
| [MongoID](https://docs.mongodb.org/manual/reference/object-id/)   | 12 bytes    | 24 chars       | configuration free, sortable
| [xid](https://github.com/rs/xid)         | 12 bytes    | 20 chars       | configuration free, sortable

## Features:

- Size: 12 bytes (96 bits), smaller than UUID, larger than snowflake
- Base32 hex encoded by default (20 chars when transported as printable string, still sortable)
- Non configured, you don't need set a unique machine and/or data center id
- K-ordered
- Embedded time with 1 second precision
- Unicity guaranteed for 16,777,216 (24 bits) unique ids per second and per host/process
- Lock-free (i.e.: unlike UUIDv1 and v2)

## References:

- [https://github.com/rs/xid](https://github.com/rs/xid)
- [http://www.slideshare.net/davegardnerisme/unique-id-generation-in-distributed-systems](http://www.slideshare.net/davegardnerisme/unique-id-generation-in-distributed-systems)
- [https://en.wikipedia.org/wiki/Universally_unique_identifier](https://en.wikipedia.org/wiki/Universally_unique_identifier)
- [https://blog.twitter.com/2010/announcing-snowflake](https://blog.twitter.com/2010/announcing-snowflake)
- [Python port](https://github.com/graham/python_xid) of Xid by [Graham Abbott](https://github.com/graham): https://github.com/graham/python_xid

# License

All source code is licensed under the [MIT License](https://github.com/Yortw/Xid.Net/blob/master/LICENSE).
Original Xid repo license at; [Xid License](https://github.com/rs/xid/blob/master/LICENSE).
