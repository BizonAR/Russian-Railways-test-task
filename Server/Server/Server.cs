using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ServerSocket
{
	private static bool _isRunning = true; // Флаг для контроля состояния сервера
	private static TcpListener _server;

	public static void Main(string[] args)
	{
		try
		{
			StartServer();

			ShowServerInfo();

			Console.WriteLine("Waiting for connections...");

			Task.Run(() => ListenForClients()); // Запускаем обработку клиентов в отдельном потоке

			// Главный поток продолжает выполнение, выводя сообщение "Waiting for connections..."
			// При этом, в отдельном потоке выполняется прослушивание подключений

			// Добавляем здесь код, который может выполняться параллельно с обработкой клиентов

			// Программа завершится только если флаг _isRunning станет false
			while (_isRunning)
			{
				// Добавьте здесь любую другую логику, которую вы хотите выполнять в главном потоке
			}

			StopServer();
			Console.WriteLine("Server closed.");
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}

	private static void StartServer()
	{
		IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
		_server = new TcpListener(ipAddress, 0);
		_server.Start();
	}

	private static void ShowServerInfo()
	{
		IPEndPoint localEndPoint = (IPEndPoint)_server.LocalEndpoint;
		Console.WriteLine("Server is running...");
		Console.WriteLine("IP Address: " + localEndPoint.Address);
		Console.WriteLine("Port: " + localEndPoint.Port);
	}

	private static void StopServer()
	{
		_server?.Stop();
		Console.WriteLine("Server stopped.");
	}

	private static void ListenForClients()
	{
		try
		{
			while (_isRunning)
			{
				TcpClient client = _server.AcceptTcpClient();
				Console.WriteLine("Client connected.");

				Task.Run(() => ProcessClient(client));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error accepting client connection: " + ex.Message);
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
		catch (Exception ex)
		{
			Console.WriteLine("Error processing client request: " + ex.Message);
		}
		finally
		{
			try
			{
				client.Close();
			}
			catch (Exception closeEx)
			{
				Console.WriteLine("Error closing client connection: " + closeEx.Message);
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
			_isRunning = false;
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
			catch (Exception ex)
			{
				SendResponse(stream, "Error creating file: " + ex.Message);
				Console.WriteLine("Error creating file: " + ex.Message);
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
