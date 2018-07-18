using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class socket_t
{
  public Socket m_socket;

  public socket_t()
  {

  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //send
  //http://man7.org/linux/man-pages/man2/sendto.2.html
  //TCP is a byte stream. There is no guarantee of a one to one relation between the number of 
  //items sent and the number of calls to send() or recv().
  //send() accepts a buffer size as parameter and keeps looping on a send() call, 
  //(that returns the number of bytes sent) until all bytes are sent.
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static int send(Socket socket, string content)
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
  //receive
  /////////////////////////////////////////////////////////////////////////////////////////////////////

  public static byte[] receive(Socket socket, int size)
  {
    int total = 0;
    int size_left = size;
    byte[] data = new byte[size];
    int recv;

    while (total < size)
    {
      recv = socket.Receive(data, total, size_left, 0);
      if (recv == 0)
      {
        break;
      }
      total += recv;
      size_left -= recv;
    }
    return data;
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////
  //receive_stream
  //uses NetworkStream.Read to read all data (including \r\n)
  /////////////////////////////////////////////////////////////////////////////////////////////////////
  public static string receive_stream(Socket socket)
  {
    NetworkStream stream = new NetworkStream(socket);
    StreamReader reader = new StreamReader(stream);
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


