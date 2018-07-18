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
    string response = socket_t.receive_stream(socket);
    Console.WriteLine("Received:");
    Console.WriteLine(response);

    string reply = "ok";
    socket_t.send(socket, reply);
    Console.WriteLine("Sent:");
    Console.WriteLine(reply);
  }

}

