using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Linq;

/////////////////////////////////////////////////////////////////////////////////////////////////////
//FormattedFile
/////////////////////////////////////////////////////////////////////////////////////////////////////

public class FormattedFile
{
  public string content;
  public string filename;
  public string full_filename;
  public FormattedFile(string full_filename, string filename, string content)
  {
    this.full_filename = full_filename;
    this.filename = filename;
    this.content = content;
  }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
//FtpState
/////////////////////////////////////////////////////////////////////////////////////////////////////

public class FtpState
{
  public ManualResetEvent wait;
  public string filename;
  public string full_filename;
  public string buf;
  public FtpWebRequest request;

  public FtpState(FtpWebRequest request, string filename, string buf)
  {
    this.request = request;
    this.filename = filename;
    this.buf = buf;
    wait = new ManualResetEvent(false);
  }

  public FtpState(FtpWebRequest request, string full_filename)
  {
    this.request = request;
    this.full_filename = full_filename;
    wait = new ManualResetEvent(false);
  }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
//Ftp
/////////////////////////////////////////////////////////////////////////////////////////////////////

class Ftp
{
  protected string username;
  protected string password;
  protected string address;
  protected string directory;

  public Ftp(string address, string username, string password, string directory)
  {
    this.username = username;
    this.password = password;
    this.address = address;
    this.directory = directory;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //delete
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void delete(Queue<FormattedFile> files_queue)
  {
    try
    {
      while (files_queue.Count > 0)
      {
        FormattedFile file = files_queue.Dequeue();
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + file.filename);
        request.Method = WebRequestMethods.Ftp.DeleteFile;
        request.Credentials = new NetworkCredential(username, password);
        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        response.Close();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //upload
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void upload(Queue<FormattedFile> files_queue, bool alive, string connection_name)
  {
    try
    {
      while (files_queue.Count > 0)
      {
        FormattedFile file = files_queue.Dequeue();

        FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + file.filename);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(username, password);
        request.ContentLength = file.content.Length;
        request.KeepAlive = alive;
        if (!String.IsNullOrEmpty(connection_name))
        {
          request.ConnectionGroupName = connection_name;
        }

        Stream stream = request.GetRequestStream();
        byte[] buf = Encoding.ASCII.GetBytes(file.content);
        stream.Write(buf, 0, file.content.Length);
        stream.Close();

        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        response.Close();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //upload
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private bool upload(string filename, string content, bool alive, string connection_name, int connection_limit)
  {
    try
    {
      FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + filename);
      request.Method = WebRequestMethods.Ftp.UploadFile;
      request.Credentials = new NetworkCredential(username, password);
      request.ContentLength = content.Length;
      request.KeepAlive = alive;
      if (!String.IsNullOrEmpty(connection_name))
      {
        request.ConnectionGroupName = connection_name;
      }
      request.ServicePoint.ConnectionLimit = connection_limit;

      Stream stream = request.GetRequestStream();
      byte[] buf = Encoding.ASCII.GetBytes(content);
      stream.Write(buf, 0, content.Length);
      stream.Close();

      FtpWebResponse response = (FtpWebResponse)request.GetResponse();
      response.Close();
      return true;
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      return false;
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //exists
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private bool exists(string filename, bool alive, string connection_name)
  {
    try
    {
      FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + filename);
      request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
      request.Credentials = new NetworkCredential(username, password);
      request.KeepAlive = alive;
      if (!String.IsNullOrEmpty(connection_name))
      {
        request.ConnectionGroupName = connection_name;
      }

      FtpWebResponse response = (FtpWebResponse)request.GetResponse();
      response.Close();
      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //rename
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private bool rename(string filename, string name, bool alive, string connection_name)
  {
    try
    {
      FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + filename);
      request.Credentials = new NetworkCredential(username, password);
      request.Method = WebRequestMethods.Ftp.Rename;
      request.RenameTo = name;
      request.KeepAlive = alive;
      if (!String.IsNullOrEmpty(connection_name))
      {
        request.ConnectionGroupName = connection_name;
      }
      bool has = exists(filename, alive, connection_name);
      if (has)
      {
        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        response.Close();
        return true;
      }
      else
      {
        return false;
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      return false;
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //upload_async
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private bool upload_async(string filename, string content, bool alive, string connection_name, int connection_limit)
  {
    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + filename);
    request.Method = WebRequestMethods.Ftp.UploadFile;
    request.Credentials = new NetworkCredential(username, password);
    request.ContentLength = content.Length;
    request.KeepAlive = alive;
    if (!String.IsNullOrEmpty(connection_name))
    {
      request.ConnectionGroupName = connection_name;
    }
    request.ServicePoint.ConnectionLimit = connection_limit;
    FtpState state = new FtpState(request, filename, content);
    ManualResetEvent wait = state.wait;
    request.BeginGetRequestStream(new AsyncCallback(get_stream_buf), state);
    wait.WaitOne();
    return true;
  }


  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //upload_async_read
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private bool upload_async_read(string full_filename, string filename, bool alive, string connection_name, int connection_limit)
  {
    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + address + "/" + directory + "/" + filename);
    request.Method = WebRequestMethods.Ftp.UploadFile;
    request.Credentials = new NetworkCredential(username, password);
    request.KeepAlive = alive;
    if (!String.IsNullOrEmpty(connection_name))
    {
      request.ConnectionGroupName = connection_name;
    }
    request.ServicePoint.ConnectionLimit = connection_limit;
    FtpState state = new FtpState(request, full_filename);
    ManualResetEvent wait = state.wait;
    request.BeginGetRequestStream(new AsyncCallback(get_stream), state);
    wait.WaitOne();
    return true;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //get_stream_buf
  //gets buffer from FtpState
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private void get_stream_buf(IAsyncResult ar)
  {
    FtpState state = (FtpState)ar.AsyncState;
    try
    {
      Stream stream = state.request.EndGetRequestStream(ar);
      byte[] buf = Encoding.ASCII.GetBytes(state.buf);
      stream.Write(buf, 0, state.buf.Length);
      stream.Close();
      state.request.BeginGetResponse(new AsyncCallback(end_response), state);
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      return;
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //get_stream
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private void get_stream(IAsyncResult ar)
  {
    FtpState state = (FtpState)ar.AsyncState;
    try
    {
      Stream request_stream = state.request.EndGetRequestStream(ar);
      const int len = 2048;
      byte[] buf = new byte[len];
      int count = 0;
      int read = 0;
      FileStream file_stream = File.OpenRead(state.full_filename);
      do
      {
        read = file_stream.Read(buf, 0, len);
        request_stream.Write(buf, 0, read);
        count += read;
      }
      while (read != 0);
      request_stream.Close();
      state.request.BeginGetResponse(new AsyncCallback(end_response), state);
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      return;
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //end_response
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private void end_response(IAsyncResult ar)
  {
    FtpState state = (FtpState)ar.AsyncState;
    try
    {
      FtpWebResponse response = (FtpWebResponse)state.request.EndGetResponse(ar);
      response.Close();
      state.wait.Set();
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      state.wait.Set();
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //send
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void send(Queue<FormattedFile> files_queue, bool alive, string connection_name, int connection_limit)
  {
    try
    {
      while (files_queue.Count > 0)
      {
        FormattedFile file = files_queue.Dequeue();
        //rename file change extention from "txt" to "tmp"
        string name = Path.GetFileNameWithoutExtension(file.filename);
        string tmp_filename = Path.ChangeExtension(file.filename, ".tmp");
        bool has = exists(tmp_filename, alive, connection_name);
        if (has)
        {
        
        }
        //upload temporary
        bool up = upload(tmp_filename, file.content, alive, connection_name, connection_limit);
        if (!up)
        {
          Debug.Assert(false);
          return;
        }
        //rename
        bool ren = rename(tmp_filename, file.filename, alive, connection_name);
        if (!ren)
        {
          Debug.Assert(false);
          return;
        }
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //send_async
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void send_async(Queue<FormattedFile> files_queue, bool alive, string connection_name, int connection_limit)
  {
    try
    {
      while (files_queue.Count > 0)
      {
        FormattedFile file = files_queue.Dequeue();
        //rename file change extention from "txt" to "tmp"
        string name = Path.GetFileNameWithoutExtension(file.filename);
        string tmp_filename = Path.ChangeExtension(file.filename, ".tmp");
        bool has = exists(tmp_filename, alive, connection_name);
        if (has)
        {
          Debug.Assert(false);
          return;
        }
        //upload temporary
        bool up = upload_async(tmp_filename, file.content, alive, connection_name, connection_limit);
        if (!up)
        {
          Debug.Assert(false);
          return;
        }
        //rename
        bool ren = rename(tmp_filename, file.filename, alive, connection_name);
        if (!ren)
        {
          Debug.Assert(false);
          return;
        }
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }


  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //send_async_read
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void send_async_read(Queue<FormattedFile> files_queue, bool alive, string connection_name, int connection_limit)
  {
    try
    {
      while (files_queue.Count > 0)
      {
        FormattedFile file = files_queue.Dequeue();
        //rename file change extention from "txt" to "tmp"
        string name = Path.GetFileNameWithoutExtension(file.filename);
        string tmp_filename = Path.ChangeExtension(file.filename, ".tmp");
        bool has = exists(tmp_filename, alive, connection_name);
        if (has)
        {
          Debug.Assert(false);
          return;
        }
        //upload temporary
        bool up = upload_async_read(file.full_filename, tmp_filename, alive, connection_name, connection_limit);
        if (!up)
        {
          Debug.Assert(false);
          return;
        }
        //rename
        bool ren = rename(tmp_filename, file.filename, alive, connection_name);
        if (!ren)
        {
          Debug.Assert(false);
          return;
        }
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //send_thread
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void send_thread(Queue<FormattedFile> files_queue)
  {
    try
    {
      while (files_queue.Count > 0)
      {
        FormattedFile file = files_queue.Dequeue();
        Thread thread = new Thread(work);
        thread.Start(file);
        //block the calling thread (main) to measure time after all threads done
        thread.Join();
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //work (thread)
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private void work(object param)
  {
    FormattedFile file = (FormattedFile)param;
    bool alive = true;
    string connection_name = "foo";
    int connection_limit = 500;
    Stopwatch watch = new Stopwatch();
    watch.Start();

    //rename file change extention from "txt" to "tmp"
    string name = Path.GetFileNameWithoutExtension(file.filename);
    string tmp_filename = Path.ChangeExtension(file.filename, ".tmp");
    bool has = exists(tmp_filename, alive, connection_name);
    if (has)
    {
      Debug.Assert(false);
      return;
    }
    //upload temporary
    bool up = upload(tmp_filename, file.content, alive, connection_name, connection_limit);
    if (!up)
    {
      Debug.Assert(false);
      return;
    }
    //rename
    bool ren = rename(tmp_filename, file.filename, alive, connection_name);
    if (!ren)
    {
      Debug.Assert(false);
      return;
    }

    watch.Stop();
    string fname = file.full_filename + ".time.txt";
    StreamWriter stream = new StreamWriter(fname);
    string tmp = watch.ElapsedMilliseconds.ToString();
    stream.Write(tmp);
    stream.Close();
  }

}