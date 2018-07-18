using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

//FTP uses two TCP connections to transfer files : a control connection and a data connection
//connect a socket(control socket) to a ftp server on the port 21
//receive on the socket a message from the ftp server(code : 220)
//send login to the ftp server using the command USER and wait for confirmation (331)
//send password using the command PASS and wait for confirmation that you are logged on the server (230)
//send file:
//use the passive mode: send command PASV
//receive answer with an IP address and a port (227), parse this message.
//connect a second socket(a data socket) with the given configuration
//use the command STOR on the control socket
//send data through the data socket, close data socket.
//leave session using on the control socket the command QUIT.

public class FtpClient
{
  IPAddress ip_address; //server IP address
  Socket socket_ctrl; // control socket 
  Socket socket_data; // data socket 
  NetworkStream stream;
  StreamReader reader;
  bool verbose = false;

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //public methods
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //FtpClient
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public FtpClient(string server_ip, bool verbose)
  {
    this.ip_address = IPAddress.Parse(server_ip);
    this.verbose = verbose;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //login
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public bool login(string username, string password)
  {
    //create the control socket
    socket_ctrl = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    try
    {
      socket_ctrl.Connect(new IPEndPoint(this.ip_address, 21));
      stream = new NetworkStream(socket_ctrl);
      reader = new StreamReader(stream);

      //a response MUST be obtained after Connect(), to get the welcome message
      string response = receive_stream();
      if (verbose) Console.WriteLine(response);

      //construct USER request message using input parameter
      //Note: there is no space between the user name and CRLF; example of request is "USER me\r\n"
      string request = "USER " + username + "\r\n";
      send(socket_ctrl, request);

      response = receive_stream();
      if (verbose) Console.WriteLine(response);

      //construct PASS request message using input parameter
      request = "PASS " + password + "\r\n";
      send(socket_ctrl, request);

      response = receive_stream();
      if (verbose) Console.WriteLine(response);
      return true;
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      return false;
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //put
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public bool put(string content, string remote_name)
  {
    try
    {
      //send PASSIVE command, tells the server to listen for a connection
      string request = "PASV\r\n";
      send(socket_ctrl, request);

      string response = receive_stream();
      if (verbose) Console.WriteLine(response);

      //parse the PASV response
      //PASV request asks the server to accept a data connection on a new TCP port selected by the server
      //PASV parameters are prohibited
      //The server normally accepts PASV with code 227
      //Its response is a single line showing the IP address of the server and the TCP port number 
      //where the server is accepting connections
      //RFC 959 failed to specify details of the response format. 
      //implementation examples
      //227 Entering Passive Mode (h1,h2,h3,h4,p1,p2).
      //the TCP port number is p1*256+p2

      int start = response.IndexOf("(");
      int end = response.IndexOf(")");
      int len = end - start;
      string str_sub = response.Substring(start + 1, len - 1);
      string[] str_a = str_sub.Split(',');
      int[] na = new int[6];
      for (int idx = 0; idx < 6; idx++)
      {
        na[idx] = Int32.Parse(str_a[idx]);
      }
      int port = na[4] * 256 + na[5];

      //create the data socket
      socket_data = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      socket_data.Connect(new IPEndPoint(this.ip_address, port));

      //send command STOR
      request = "STOR " + remote_name + "\r\n";
      send(socket_ctrl, request);

      response = receive_stream();
      if (verbose) Console.WriteLine(response);

      //send on the data socket
      send(socket_data, content);

      //close data socket
      socket_data.Close();

      //read response AFTER closing data socket, otherwise the connection will hang
      response = receive_stream();
      if (verbose) Console.WriteLine(response);
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
      return false;
    }

    return true;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //quit
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public void quit()
  {
    try
    {
      string request = "QUIT\r\n";
      send(socket_ctrl, request);

      string response = receive_stream();
      if (verbose) Console.WriteLine(response);

      reader.Close();
      stream.Close();
      socket_ctrl.Close();
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //private methods
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //send
  //http://man7.org/linux/man-pages/man2/sendto.2.html
  //TCP is a byte stream. There is no guarantee of a one to one relation between the number of 
  //items sent and the number of calls to send() or recv().
  //send() accepts a buffer size as parameter and keeps looping on a send() call, 
  //(that returns the number of bytes sent) until all bytes are sent.
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  private int send(Socket socket, string content)
  {
    int total = 0;
    try
    {
      byte[] buf = Encoding.ASCII.GetBytes(content);
      int size = buf.Length;
      int size_left = size;
      int sent;
      while (total < size)
      {
        sent = socket.Send(buf, total, size_left, SocketFlags.None);
        total += sent;
        size_left -= sent;
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
    return total;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //receive_line
  //uses StreamReader.ReadLine to read until \r\n
  //FTP responses terminate with \r\n, so ReadLine() can be used to retrieve a FTP message
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public string receive_line()
  {
    StringBuilder builder = new StringBuilder();
    if (stream.CanRead)
    {
      do
      {
        string str = reader.ReadLine();
        builder.AppendFormat("{0}", str);
      }
      while (stream.DataAvailable);
    }
    return builder.ToString();
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //receive_stream
  //uses NetworkStream.Read to read all data (including \r\n)
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public string receive_stream()
  {
    StringBuilder builder = new StringBuilder();
    if (stream.CanRead)
    {
      byte[] buf = new byte[1024];
      int nbr_rcv = 0;
      do
      {
        nbr_rcv = stream.Read(buf, 0, buf.Length);
        builder.AppendFormat("{0}", Encoding.ASCII.GetString(buf, 0, nbr_rcv));
      }
      while (stream.DataAvailable);
    }
    return builder.ToString();
  }

}


