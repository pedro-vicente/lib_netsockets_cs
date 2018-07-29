using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

/////////////////////////////////////////////////////////////////////////////////////////////////////
//web_client
/////////////////////////////////////////////////////////////////////////////////////////////////////

class web_client
{


  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //Main
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  static void Main(string[] args)
  {
    bool secure = true;
    string uri;
    if (secure)
    {
      ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
      uri = "https://127.0.0.1:8443";
    }
    else
    {
      uri = "http://127.0.0.1:8000";
    }
    string message = "test";
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
    request.KeepAlive = false;
    request.ProtocolVersion = HttpVersion.Version10;
    request.Method = "POST";
    byte[] buf = Encoding.ASCII.GetBytes(message);
    request.ContentType = "application/x-www-form-urlencoded";
    request.ContentLength = buf.Length;
    Stream stream = request.GetRequestStream();
    stream.Write(buf, 0, buf.Length);
    stream.Close();
    Console.WriteLine("Client sent:");
    Console.WriteLine(message);

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //get response
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
    Console.WriteLine("Client received:");
    Console.WriteLine(new StreamReader(response.GetResponseStream()).ReadToEnd());
    Console.WriteLine(response.StatusCode);

  }

}//web_client

