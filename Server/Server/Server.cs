using System.Net;
using System.Net.Sockets;
using System.Text;

class ServerSocket
{
	private static bool s_isRunning = true; // Флаг для контроля состояния сервера
	private static TcpListener s_server;

	public static void Main(string[] args)
	{
		try
		{
			StartServer();

			ShowServerInfo();

			Console.WriteLine("Waiting for connections...");

			Task.Run(() => ListenForClients());

			while (s_isRunning)
			{
			}

			StopServer();
			Console.WriteLine("Server closed.");
		}
		catch (Exception exception)
		{
			Console.WriteLine("Error: " + exception.Message);
		}
	}

	private static void StartServer()
	{
		IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
		s_server = new TcpListener(ipAddress, 0);
		s_server.Start();
	}

	private static void ShowServerInfo()
	{
		IPEndPoint localEndPoint = (IPEndPoint)s_server.LocalEndpoint;
		Console.WriteLine("Server is running...");
		Console.WriteLine("IP Address: " + localEndPoint.Address);
		Console.WriteLine("Port: " + localEndPoint.Port);
	}

	private static void StopServer()
	{
		s_server?.Stop();
		Console.WriteLine("Server stopped.");
	}

	private static void ListenForClients()
	{
		try
		{
			while (s_isRunning)
			{
				if (s_server.Pending() == false)
				{
					continue;
				}

				TcpClient client = s_server.AcceptTcpClient();
				Console.WriteLine("Client connected.");

				Task.Run(() => ProcessClient(client));
			}
		}
		catch (Exception exception)
		{
			Console.WriteLine("Error accepting client connection: " + exception.Message);
		}
	}

	public static void ProcessClient(TcpClient client)
	{
		try
		{
			using (NetworkStream stream = client.GetStream())
			{
				while (true)
				{
					if (client.Connected == false)
					{
						Console.WriteLine("Client disconnected.");
						break;
					}

					byte[] buffer = new byte[1024];
					int bytesRead = stream.Read(buffer, 0, buffer.Length);
					string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

					HandleClientCommand(dataReceived, stream);
				}
			}
		}
		catch (Exception exception)
		{
			Console.WriteLine("Error processing client request: " + exception.Message);
		}
		finally
		{
			try
			{
				client.Close();
			}
			catch (Exception closeException)
			{
				Console.WriteLine("Error closing client connection: " + closeException.Message);
			}
		}
	}

	private static void HandleClientCommand(string command, NetworkStream stream)
	{
		if (command.Trim().StartsWith("CreateFile", StringComparison.OrdinalIgnoreCase))
		{
			CreateFile(command, stream);
		}
		else if (command.Trim().StartsWith("Exit", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine("Exit command received. Closing connection...");
			s_isRunning = false;
		}
		else
		{
			SendResponse(stream, "Invalid command.");
		}
	}

	private static void CreateFile(string command, NetworkStream stream)
	{
		string[] dataParts = command.Split('|');

		if (dataParts.Length == 3)
		{
			string fileName = dataParts[1];
			string fileContent = dataParts[2];

			try
			{
				File.WriteAllText(fileName, fileContent);
				SendResponse(stream, "File created successfully.");
				Console.WriteLine("File created successfully");
			}
			catch (Exception exception)
			{
				SendResponse(stream, "Error creating file: " + exception.Message);
				Console.WriteLine("Error creating file: " + exception.Message);
			}
		}
		else
		{
			SendResponse(stream, "Invalid command format.");
			Console.WriteLine("Invalid command format.");
		}
	}

	private static void SendResponse(NetworkStream stream, string message)
	{
		byte[] responseBuffer = Encoding.UTF8.GetBytes(message);
		stream.Write(responseBuffer, 0, responseBuffer.Length);
	}
}
