using System;
using System.Net.Sockets;
using System.Threading;

namespace BasicTcpServerWindowsService
{
	/// <summary>
	/// Individual connection
	/// </summary>
	public class TcpSocketListener
	{
		#region Properties and Variables

		/// <summary>
		/// Accepted commands
		/// </summary>
		private enum COMMANDS
		{
			CONNECTIONS ,
			COUNT ,
			PRIME ,
			TERMINATE
		}

		/// <summary>
		/// The connection endpoint
		/// </summary>
		private Socket socket = null;

		// Thread state
		private bool stopClient = false ,
					 markedForDeletion = false;

		/// <summary>
		/// Thread connection runs on
		/// </summary>
		private Thread connectionThread = null;

		// used to track thread activity
		private DateTime lastMessageReceived ,
						 currentMessageReceived;

		/// <summary>
		/// Inactive connection timeout
		/// </summary>
		/// <remarks>Could read this from App.config</remarks>
		private int timeout = 15000;

		#endregion Properties and Variables


		#region Constructors and Destructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="socket"></param>
		public TcpSocketListener ( Socket socket )
		{
			this.socket = socket;
		}

		/// <summary>
		/// Deconstructor
		/// </summary>
		~TcpSocketListener ( )
		{
			StopSocketListener ( );
		}

		#endregion Constructors and Destructors


		#region Methods

		/// <summary>
		/// Connection thread start
		/// </summary>
		public void StartSocketListener ( )
		{
			if ( this.socket != null )
			{
				this.connectionThread = new Thread ( new ThreadStart ( SocketListenerThreadStart ) );

				this.connectionThread.Start ( );
			}
		}

		/// <summary>
		/// Handles incoming connection traffic
		/// </summary>
		private void SocketListenerThreadStart ( )
		{
			this.socket.Send ( System.Text.Encoding.ASCII.GetBytes ( "HELO" ) );

			TcpServer.IncrementHandshakeCount ( );

			int size = 0;

			Byte[ ] bytes = new Byte[ 1024 ];

			this.lastMessageReceived = DateTime.Now;

			this.currentMessageReceived = DateTime.Now;

			Timer timer = new Timer ( new TimerCallback ( CheckConnectionTime )
										, null
											, this.timeout
												, this.timeout );
			
			while ( !this.stopClient )
			{
				try
				{
					//this.socket.ReceiveBufferSize = 1024;

					size = this.socket.Receive ( bytes , 0 , 1024 , SocketFlags.None );

					this.currentMessageReceived = DateTime.Now;

					char [ ] chars = new char [ size ];

					System.Text.Decoder decoder = System.Text.Encoding.ASCII.GetDecoder ( );

					decoder.GetChars ( bytes , 0 , size , chars , 0 );

					string received = new System.String ( chars );

					received = received.ToUpper ( );

					if ( received.Equals ( COMMANDS.CONNECTIONS.ToString ( ) ) )
					{
						this.socket.Send ( System.Text.Encoding.ASCII.GetBytes ( TcpServer.ConnectionCount.ToString ( ) ) );
					}
					else if ( received.Equals ( COMMANDS.COUNT.ToString ( ) ) )
					{
						this.socket.Send ( System.Text.Encoding.ASCII.GetBytes ( TcpServer.SucessfulHandshakes.ToString ( ) ) );
					}
					else if ( received.Equals ( COMMANDS.PRIME.ToString ( ) ) )
					{
						int prime = TcpServer.GetRandomPrime ( );

						this.socket.Send ( System.Text.Encoding.ASCII.GetBytes ( prime.ToString ( ) ) );
					}
					else if ( received.Equals ( COMMANDS.TERMINATE.ToString ( ) ) )
					{
						this.socket.Send ( System.Text.Encoding.ASCII.GetBytes ( "BYE" ) );

						this.StopSocketListener ( );
					}
				}
				catch ( System.Net.Sockets.SocketException ex )
				{
					this.stopClient = true;

					this.markedForDeletion = true;
				}
			}

			timer.Change ( Timeout.Infinite , Timeout.Infinite );

			timer = null;
		}

		/// <summary>
		/// Checks inactivity duration and closes thread as appropriate
		/// </summary>
		/// <param name="state"></param>
		private void CheckConnectionTime ( object state )
		{
			if ( this.lastMessageReceived.Equals ( this.currentMessageReceived ) )
			{
				this.StopSocketListener ( );
			}
			else
			{
				this.lastMessageReceived = this.currentMessageReceived;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool IsMarkedForDeletion ( )
		{
			return this.markedForDeletion;
		}

		/// <summary>
		/// Thread terimation
		/// </summary>
		public void StopSocketListener ( )
		{
			if ( this.socket != null )
			{
				this.stopClient = true;

				this.socket.Close ( );

				this.connectionThread.Join ( 1000 );

				if ( this.connectionThread.IsAlive )
				{
					this.connectionThread.Abort ( );
				}

				this.connectionThread = null;

				this.socket = null;

				this.markedForDeletion = true;
			}
		}

		#endregion Methods
	}
}
