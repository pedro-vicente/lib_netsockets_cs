using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;

public class Program
{
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //Main
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  static void Main(string[] args)
  {
    if (args.Length < 3)
    {
      Console.WriteLine("Usage: ftp_async <server> <user name> <password>");
      return;
    }
    string address = args[0];
    string username = args[1];
    string password = args[2];
    test_ftp_client(address, username, password);
    benchmark(address, username, password);
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //test_ftp_client
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  static void test_ftp_client(string address, string username, string password)
  {
    StreamReader stream = new StreamReader("C://FTP//_1//test_1.txt");
    string buf = stream.ReadToEnd();
    stream.Close();

    FtpClient ftp = new FtpClient(address, true);
    ftp.login(username, password);
    for (int idx = 0; idx < 3; idx++)
    {
      string name = idx + ".txt";
      ftp.put(buf, name);
    }
    ftp.quit();
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //benchmark
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  static void benchmark(string address, string username, string password)
  {
    int nbr_messages = 30;
    int nbr_lines = 60;
    string root = "C://FTP";
    string path = "C://FTP//_1";
    bool do_test_1 = false;
    Stopwatch watch = new Stopwatch();

    Ftp ftp = new Ftp(address, username, password, ".");
    Queue<FormattedFile> files_queue = new Queue<FormattedFile>();

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //generate test files: number of files and file size as parameters
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    delete_messages(root);
    delete_messages(path);
    make_messages(path, nbr_messages, nbr_lines);

    //get file size
    List<FileInfo> files = new DirectoryInfo(path).GetFiles("*.txt").OrderBy(f => f.CreationTime).ToList();
    long length = files.Last().Length;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //performance parameters
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    bool alive = false;
    string connection_name = null;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test FTP client
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    read_messages(path, files_queue);
    Debug.Assert(files_queue.Count() == nbr_messages);
    watch.Start();
    print_start("Test FTP client", length, nbr_messages);

    FtpClient ftp_client = new FtpClient(address, false);
    ftp_client.login(username, password);
    while (files_queue.Count > 0)
    {
      FormattedFile file = files_queue.Dequeue();
      string name = Path.GetFileNameWithoutExtension(file.filename);
      string tmp_filename = Path.ChangeExtension(file.filename, ".tmp");
      ftp_client.put(file.content, tmp_filename);
    }
    ftp_client.quit();

    watch.Stop();
    print_end(watch.ElapsedMilliseconds);
    watch.Reset();
    Debug.Assert(files_queue.Count() == 0);

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test upload
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    if (do_test_1)
    {
      delete_messages(root);
      read_messages(path, files_queue);
      Debug.Assert(files_queue.Count() == nbr_messages);
      watch.Start();
      print_start("Test upload (alive false, no connection name)", length, nbr_messages);
      ftp.send(files_queue, alive, connection_name, nbr_messages);
      watch.Stop();
      print_end(watch.ElapsedMilliseconds);
      watch.Reset();
      Debug.Assert(files_queue.Count() == 0);
    }

    alive = true;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test upload
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    if (do_test_1)
    {
      delete_messages(root);
      read_messages(path, files_queue);
      Debug.Assert(files_queue.Count() == nbr_messages);
      watch.Start();
      print_start("Test upload (alive true, no connection name)", length, nbr_messages);
      ftp.send(files_queue, alive, connection_name, nbr_messages);
      watch.Stop();
      print_end(watch.ElapsedMilliseconds);
      watch.Reset();
      Debug.Assert(files_queue.Count() == 0);
    }

    connection_name = address + username;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test upload
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    delete_messages(root);
    read_messages(path, files_queue);
    Debug.Assert(files_queue.Count() == nbr_messages);
    watch.Start();
    print_start("Test upload (alive true, connection name)", length, nbr_messages);
    ftp.send(files_queue, alive, connection_name, nbr_messages);
    watch.Stop();
    print_end(watch.ElapsedMilliseconds);
    watch.Reset();
    Debug.Assert(files_queue.Count() == 0);

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test upload async
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    delete_messages(root);
    read_messages(path, files_queue);
    Debug.Assert(files_queue.Count() == nbr_messages);
    watch.Start();
    print_start("Test async(alive true, connection name)", length, nbr_messages);
    ftp.send_async(files_queue, alive, connection_name, nbr_messages);
    watch.Stop();
    print_end(watch.ElapsedMilliseconds);
    watch.Reset();
    Debug.Assert(files_queue.Count() == 0);

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test upload async read async
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    if (do_test_1)
    {
      delete_messages(root);
      read_messages(path, files_queue);
      Debug.Assert(files_queue.Count() == nbr_messages);
      watch.Start();
      print_start("Test async read (alive true, connection name)", length, nbr_messages);
      ftp.send_async_read(files_queue, alive, connection_name, nbr_messages);
      watch.Stop();
      print_end(watch.ElapsedMilliseconds);
      watch.Reset();
      Debug.Assert(files_queue.Count() == 0);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //test send thread
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    delete_messages(root);
    read_messages(path, files_queue);
    Debug.Assert(files_queue.Count() == nbr_messages);
    print_start("Test send thread (alive true, connection name)", length, nbr_messages);
    ftp.send_thread(files_queue);
    Debug.Assert(files_queue.Count() == 0);

    //read each file with saved time in thread
    long tt = 0;
    List<FileInfo> file_times = new DirectoryInfo("C://FTP//_1").GetFiles("*.time.txt").OrderBy(f => f.CreationTime).ToList();
    foreach (FileInfo f in file_times)
    {
      StreamReader reader = new StreamReader(f.FullName);
      string buf = reader.ReadToEnd();
      long t = Convert.ToInt64(buf);
      tt += t;
      reader.Close();
    }
    string message = "\t\t" + tt / (float)1000.0 + " seconds, " + file_times.Count + " files";
    Console.Write(message);
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //delete_messages
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static void delete_messages(string path)
  {
    List<FileInfo> files = new DirectoryInfo(path).GetFiles("*.*").OrderBy(f => f.CreationTime).ToList();
    foreach (FileInfo file in files)
    {
      File.Delete(file.FullName);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //make_messages
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static void make_messages(string path, int nbr_messages, int nbr_lines)
  {
    path += "//";
    int counter = 0;
    string name;
    while (true)
    {
      counter++;
      if (counter > nbr_messages)
      {
        break;
      }
      name = "test_";
      name += counter;
      name += ".txt";
      string send_file = path + name;
      string contents = "RELAY_MSGTYPE: FTP\r\n";
      contents += "ADDRSS: 127.0.0.1\r\n";
      contents += "FTPUSR: pvicente\r\n";
      contents += "PSSWRD: 1234\r\n";
      contents += "DIRECTORY: .\r\n";
      contents += name;
      contents += "\r\n";
      for (int idx = 0; idx < nbr_lines; idx++)
      {
        contents += "TEST MESSAGE TEST MESSAGE TEST MESSAGE TEST MESSAGE TEST MESSAGE TEST MESSAGE TEST MESSAGE\r\n";
      }
      File.WriteAllText(send_file, contents);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //read_messages
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static void read_messages(string path, Queue<FormattedFile> files_queue)
  {
    List<FileInfo> files = new DirectoryInfo(path).GetFiles("*.txt").OrderBy(f => f.CreationTime).ToList();
    foreach (FileInfo file in files)
    {
      StreamReader stream = new StreamReader(file.FullName);
      string buf = stream.ReadToEnd();
      stream.Close();
      FormattedFile formatted_file = new FormattedFile(file.FullName, file.Name, buf);
      files_queue.Enqueue(formatted_file);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //print_time
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static void print_end(long time)
  {
    float time_sec = time / (float)1000.0;
    string message = "\t\t" + time_sec + " seconds";
    Console.WriteLine(message);
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //print_start
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static void print_start(string msg, long length, long nbr_files)
  {
    string message = msg + " " + nbr_files + " files of size " + length / 1024 + " Kbytes took ...";
    Console.Write(message);
  }

}
