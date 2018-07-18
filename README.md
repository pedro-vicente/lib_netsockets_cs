# lib_netsockets_cs
C# socket library with FTP client, HTTP client and server implementations

# ftp_t
FTP client implementation using Socket and FtpWebRequest benchmark.

##
Usage:

```
ftp_t 'server' 'user name' 'password'
```


##
Benchmarks:

```
Test FTP client 30 files of size 5 Kbytes took ...                                      14.897 seconds
Test upload (alive true, connection name) 30 files of size 5 Kbytes took ...            34.229 seconds
Test async(alive true, connection name) 30 files of size 5 Kbytes took ...              40.659 seconds
Test send thread (alive true, connection name) 30 files of size 5 Kbytes took ...       38.926 seconds, 30 files
```

##
FTP client usage:

```c#
FtpClient ftp = new FtpClient("127.0.0.1", true);
ftp.login("user", "password");
ftp.put(buffer, "filename.txt");
ftp.quit();
```