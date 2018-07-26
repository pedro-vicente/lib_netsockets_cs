using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

//FTAMS: File Transfer Access and Management Services
//create a socket
//bind the socket to a local IPEndPoint
//place the socket in listen mode
//accept an incoming connection on the socket

/////////////////////////////////////////////////////////////////////////////////////////////////////
//tcp_server_t
/////////////////////////////////////////////////////////////////////////////////////////////////////

class tcp_server_t : socket_t
{

  //create TCP socket for incoming connections
  //bind to the local address
  //mark the socket so it will listen for incoming connections
  public tcp_server_t()
  {
    IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 4000);
    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    m_socket.Bind(ipep);
    m_socket.Listen(10);
  }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
//ftams_server
/////////////////////////////////////////////////////////////////////////////////////////////////////

class ftams_server
{
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //Main
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  static void Main(string[] args)
  {
    tcp_server_t server = new tcp_server_t();
    while (true)
    {
      Socket client = server.m_socket.Accept();
      handle_client(client);
    }
    server.m_socket.Close();
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //handle_client
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  static void handle_client(Socket socket)
  {
    IPEndPoint clientep = (IPEndPoint)socket.RemoteEndPoint;
    Console.WriteLine("Connected with {0} at port {1}", clientep.Address, clientep.Port);
    string message = socket_t.receive_stream(socket);
    Console.WriteLine("Server received:");
    Console.WriteLine(message);

    string http = "";
    http += "HTTP/1.1 200 OK\r\n";
    http += "Content-Type: text/html\r\n";
    string response = "<html><body><h1>It works!</h1></body></html>";
    http += "Content-Length: ";
    int message_size = response.Length;
    http += message_size.ToString();
    http += "\r\n";
    http += "Connection: close\r\n";
    //terminate HTTP header
    http += "\r\n";
    //add message
    http += response;
    socket_t.send(socket, http);
    Console.WriteLine("Server sent:");
    Console.WriteLine(http);
  }

}

