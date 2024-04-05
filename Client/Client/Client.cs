using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WinFormsApp1
{
	public partial class ClientForm : Form
	{
		private ServerCommunication _serverCommunication;

		private TcpClient _client;
		private NetworkStream _stream;

		private MaskedTextBox _ipAddressTextBox;
		private TextBox _portTextBox;
		private Button _connectButton;
		private Button _exitButton;
		private TextBox _dataTextBox;
		private TextBox _fileNameTextBox;
		private Button _sendDataButton;
		private Label _serverIpAndFileNameLabel;
		private Label _portAndFileTextLabel;

		public ClientForm()
		{
			InitializeComponent();
			InitializeIpTextBox();
			InitializeComponentButton();
		}

		private void InitializeIpTextBox()
		{
			_serverIpAndFileNameLabel = new Label();
			_serverIpAndFileNameLabel.Text = "Enter the server's IP address to connect:";
			_serverIpAndFileNameLabel.AutoSize = true;
			_serverIpAndFileNameLabel.Location = new System.Drawing.Point(10, 10);
			this.Controls.Add(_serverIpAndFileNameLabel);

			_ipAddressTextBox = new MaskedTextBox();
			_ipAddressTextBox.Mask = "000\\.000\\.000\\.000";
			_ipAddressTextBox.Location = new System.Drawing.Point(10, 30);
			_ipAddressTextBox.Size = new System.Drawing.Size(150, 20);
			this.Controls.Add( _ipAddressTextBox );

			_portAndFileTextLabel = new Label();
			_portAndFileTextLabel.Text = "Enter port:";
			_portAndFileTextLabel.AutoSize = true;
			_portAndFileTextLabel.Location = new System.Drawing.Point(10, 70);
			this.Controls.Add(_portAndFileTextLabel);

			_portTextBox = new TextBox();
			_portTextBox.Location = new System.Drawing.Point(10, 100);
			_portTextBox.Size = new System.Drawing.Size(150, 20);
			_portTextBox.KeyPress += PortTextBox_KeyPress;
			this.Controls.Add( _portTextBox );
		}

		private void InitializeComponentButton()
		{
			_exitButton = new Button();
			_exitButton.Text = "Exit";
			_exitButton.Location = new System.Drawing.Point(10, 130);
			_exitButton.Size = new System.Drawing.Size(80, 20);
			_exitButton.Click += ExitButton_Click;
			_exitButton.Visible = false;
			this.Controls.Add(_exitButton);

			_connectButton = new Button();
			_connectButton.Text = "Connect";
			_connectButton.Location = new System.Drawing.Point(170, 30);
			_connectButton.Size = new System.Drawing.Size(80, 20);
			_connectButton.Click += ConnectButton_Click;
			this.Controls .Add( _connectButton );
		}

		private void ExitButton_Click(object? sender, EventArgs e)
		{
			try
			{
				string exitCommand = "Exit";
				string response = _serverCommunication.SendDataToServer(exitCommand);

				_serverCommunication.CloseConnection();

				MessageBox.Show("Disconnected from the server.", "Success", MessageBoxButtons.OK, 
					MessageBoxIcon.Information);

				Application.Exit();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}

		private void ConnectButton_Click(object? sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace( _ipAddressTextBox.Text )) 
			{
				MessageBox.Show("Please enter the IP adress", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (string.IsNullOrWhiteSpace(_portTextBox.Text )) 
			{
				MessageBox.Show("Please enter the port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (!IPAddress.TryParse(_ipAddressTextBox.Text, out IPAddress ipAddress))
			{
				MessageBox.Show("Invalid IP address format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (!int.TryParse(_portTextBox.Text, out int port) || port < 1 || port > 65535)
			{
				MessageBox.Show("Please enter a valid port number between 1 and 65535.", "Error", 
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				using (var ping = new Ping())
				{
					var reply = ping.Send(ipAddress);

					if (reply.Status != IPStatus.Success)
					{
						MessageBox.Show($"The server at {ipAddress} is not reachable.", "Error", 
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
				}

				_client = new TcpClient();
				_client.Connect(ipAddress, port);

				_serverCommunication = new ServerCommunication(_client);

				MessageBox.Show("Connected to the server.", "Success", MessageBoxButtons.OK, 
					MessageBoxIcon.Information);

				_exitButton.Visible = true;

				ShowFileControls();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error connecting to the server: {ex.Message}", "Error", MessageBoxButtons.OK, 
					MessageBoxIcon.Error);
			}
		}

		private void ShowFileControls()
		{
			this.Controls.Remove(_ipAddressTextBox);
			this.Controls.Remove( _portTextBox);
			this.Controls.Remove(_connectButton);

			_serverIpAndFileNameLabel.Text = "Enter file name:";

			_fileNameTextBox = new TextBox();
			_fileNameTextBox.Location = new System.Drawing.Point(10, 40);
			_fileNameTextBox.Size = new System.Drawing.Size(200, 20);
			this.Controls.Add(_fileNameTextBox);

			_portAndFileTextLabel.Text = "Enter file text:";

			_dataTextBox = new TextBox();
			_dataTextBox.Location = new System.Drawing.Point(10, 100);
			_dataTextBox.Size = new System.Drawing.Size(200, 20);
			this.Controls.Add(_dataTextBox);

			_sendDataButton = new Button();
			_sendDataButton.Text = "Send data";
			_sendDataButton.Location = new System.Drawing.Point(220, 70);
			_sendDataButton.Size = new System.Drawing.Size(80, 20);
			_sendDataButton.Click += SendButton_Click;
			this.Controls.Add(_sendDataButton);
		}


		private void SendButton_Click(object? sender, EventArgs e)
		{
			try
			{
				if (_client == null || _client.Connected == false)
				{
					MessageBox.Show("The client is not connected to the server.", "Error", MessageBoxButtons.OK, 
						MessageBoxIcon.Error);
					return;
				}

				if (string.IsNullOrWhiteSpace(_fileNameTextBox.Text))
				{
					MessageBox.Show("Please enter the file name", "Error", MessageBoxButtons.OK, 
						MessageBoxIcon.Error);
					return;
				}

				if (string.IsNullOrWhiteSpace(_dataTextBox.Text))
				{
					MessageBox.Show("Please enter the file content", "Error", MessageBoxButtons.OK, 
						MessageBoxIcon.Error);
					return;
				}

				string fileName = _fileNameTextBox.Text;
				string fileContent = _dataTextBox.Text;
				string dataToSend = "CreateFile|" + fileName + "|" + fileContent;

				string response = _serverCommunication.SendDataToServer(dataToSend);

				if (response == "Invalid command.")
				{
					MessageBox.Show(response, "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					MessageBox.Show(response, "Server Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine("Error: " + exception.Message);
			}
		}

		private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_client != null)
			{
				if (_stream != null)
				{
					_stream.Close();
				}
				_client.Close();
			}
		}

		private void PortTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
			{
				e.Handled = true;
			}
		}
	}
}
