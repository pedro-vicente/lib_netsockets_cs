using System;
using System.Net;

namespace http_listener
{
  class http_listener_t
  {
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //Main
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    static void Main(string[] args)
    {
      HttpListener listener = new HttpListener();
      listener.Prefixes.Add("http://+:8000/");
      listener.Prefixes.Add("https://+:8443/");
      listener.Start();
      Console.WriteLine("Server listening in http://+:8000, https://+:8443/...");
      HttpListenerContext context = listener.GetContext();
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;
      string message = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><test>TEST</test>";
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
      response.ContentLength64 = buffer.Length;
      System.IO.Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      Console.WriteLine("Server sent:");
      Console.WriteLine(message);
      listener.Stop();
    }
  }
}
