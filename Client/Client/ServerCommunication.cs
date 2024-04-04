using System.Net.Sockets;
using System.Text;

namespace WinFormsApp1
{
	public class ServerCommunication
	{
		private TcpClient _client;
		private NetworkStream _stream;

		public ServerCommunication(TcpClient client)
		{
			_client = client;
			_stream = _client.GetStream();
		}

		public string SendDataToServer(string data)
		{
			try
			{
				// Отправляем данные на сервер
				byte[] buffer = Encoding.UTF8.GetBytes(data);
				_stream.Write(buffer, 0, buffer.Length);

				// Получаем ответ от сервера
				buffer = new byte[1024];
				int bytesRead = _stream.Read(buffer, 0, buffer.Length);
				string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				return response;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return "Error communicating with the server.";
			}
		}

		public void CloseConnection()
		{
			// Закрываем поток и клиент
			_stream.Close();
			_client.Close();
		}
	}
}
