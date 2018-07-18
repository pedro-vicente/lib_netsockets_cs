using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

//FTAMS: File Transfer Access and Management Services

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
  public int open()
  {
    IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4000);
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
    tcp_client_t client = new tcp_client_t();
    if (client.open() < 0)
    {
      return;
    }

    string http_request;
    http_request = "test";

    if (client.write_all(http_request) < 0)
    {
      client.m_socket.Close();
      return;
    }

    Console.WriteLine("Sent:");
    Console.WriteLine(http_request);

    string response = socket_t.receive_stream(client.m_socket);
    Console.WriteLine("Received:");
    Console.WriteLine(response);
    client.m_socket.Close();
  }



}//ftams_client

