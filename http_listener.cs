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
      listener.Start();
      Console.WriteLine("Listening in port 8000...");
      HttpListenerContext context = listener.GetContext();
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;
      string message = "server sent";
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
      response.ContentLength64 = buffer.Length;
      System.IO.Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      listener.Stop();
    }
  }
}
