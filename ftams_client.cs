using System;
using System.Net;
using System.Net.Sockets;

/////////////////////////////////////////////////////////////////////////////////////////////////////
//FTAMS: File Transfer Access and Management Services
//usage
//http://127.0.0.1/FTAMSDeviceService/DeviceService.svc/Reports/GetMessages/3
//http://127.0.0.1/FTAMSDeviceService/DeviceService.svc/Reports/PostMessage
/////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////
//tcp_client_t
/////////////////////////////////////////////////////////////////////////////////////////////////////

class tcp_client_t : socket_t
{

  public tcp_client_t()
  {

  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //open
  //create a socket and call connect()
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  public int open(string ip, int port)
  {
    IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), port);
    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    try
    {
      m_socket.Connect(ipep);
    }
    catch (SocketException e)
    {
      Console.WriteLine("Unable to connect to server.");
      Console.WriteLine(e.ToString());
      return -1;
    }
    return 0;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //write_all
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  public int write_all(string buf)
  {
    send(m_socket, buf);
    return 0;
  }

}

/////////////////////////////////////////////////////////////////////////////////////////////////////
//ftams_client
/////////////////////////////////////////////////////////////////////////////////////////////////////

class ftams_client
{
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //Main
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  static void Main(string[] args)
  {
    const string ftams_place = "/FTAMSDeviceService/DeviceService.svc/Reports";
    string uri = args[0];

    if (uri.Contains(ftams_place) == false)
    {
      return;
    }

    //parse URI 
    int start = uri.IndexOf("://");
    if (start == -1)
    {

    }
    start += 3;
    int end = uri.IndexOf("/", start + 1);
    string host_name = uri.Substring(start, end - start);
    Console.WriteLine("host: {0}", host_name);

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //make HTTP request
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    string http = "";
    string message;
    message = "test";

    //get API token
    int ftams_place_pos = uri.IndexOf(ftams_place, 0);
    string api_token = uri.Substring(ftams_place_pos + ftams_place.Length);
    string get_messages_nbr;
    if (api_token.IndexOf("/GetMessages/") != -1)
    {
      const string get_messages = "/GetMessages/";
      get_messages_nbr = api_token.Substring(get_messages.Length);
      http += "GET ";
      http += api_token;
      http += " HTTP/1.1\r\n";
      http += "\r\n"; //terminate HTTP header
    }
    else if (api_token.CompareTo("/PostMessage") == 0)
    {
      http += "POST ";
      http += api_token;
      http += " HTTP/1.1\r\n";
      http += "Content-Length: ";
      int message_size = message.Length;
      http += message_size.ToString();
      http += "\r\n";
      http += "Connection: close\r\n";
      http += "\r\n"; //terminate HTTP header
      //add message
      http += message;
    }

    tcp_client_t client = new tcp_client_t();
    if (client.open(host_name, 4000) < 0)
    {
      return;
    }

    if (client.write_all(http) < 0)
    {
      client.m_socket.Close();
      return;
    }

    Console.WriteLine("Sent:");
    Console.WriteLine(http);

    string response = socket_t.receive_stream(client.m_socket);
    Console.WriteLine("Received:");
    Console.WriteLine(response);
    client.m_socket.Close();
  }



}//ftams_client

