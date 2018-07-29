using System;
using System.Net;
using System.IO;

namespace http_listener
{
  class http_listener_t
  {
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //Main
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    static void Main(string[] args)
    {
      bool IS_POST = true;
      HttpListener listener = new HttpListener();
      listener.Prefixes.Add("http://+:8000/");
      listener.Prefixes.Add("https://+:8443/");
      listener.Start();
      Console.WriteLine("Server listening in http://+:8000, https://+:8443/...");
      HttpListenerContext context = listener.GetContext();
      string str_recv = "test";
      if (IS_POST)
      {
        str_recv = new StreamReader(context.Request.InputStream).ReadToEnd();
      }
      Console.WriteLine("Server received:");
      Console.WriteLine(str_recv);
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;
      string message = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><received>";
      message += str_recv;
      message += "</received>";
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
      response.ContentLength64 = buffer.Length;
      Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      Console.WriteLine("Server sent:");
      Console.WriteLine(message);
      listener.Stop();
    }
  }
}
