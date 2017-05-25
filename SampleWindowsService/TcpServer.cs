using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace BasicTcpServerWindowsService
{
	/// <summary>
	/// Manages the tcp server instance
	/// </summary>
	public class TcpServer
	{
		#region Properties

		/// <summary>
		/// Default port number
		/// </summary>
		private Int32 defaultPort = 55555;

		/// <summary>
		/// Limit the maximum number of connections to avoid denial of service attacks
		/// </summary>
		/// <remarks>could always read the following property from App.config</remarks>
		private Int32 maxConnections = 1000;

		/// <summary>
		/// Accessor, count of live connections
		/// </summary>
		public static Int32 ConnectionCount
		{
			get
			{
				if ( socketListeners == null )
				{
					return 0;
				}
				else
				{
					lock ( socketListeners )
					{
						return socketListeners.Count ( );
					}
				}
			}
		}

		private static Int32 sucessfulHandshakes = 0;

		/// <summary>
		/// A counter of all successful handshakes since server start
		/// </summary>
		/// <remarks>kind of thing that should be persisted, no?</remarks>
		public static Int32 SucessfulHandshakes
		{
			get { return sucessfulHandshakes; }
			set { sucessfulHandshakes = value; }
		}

		/// <summary>
		/// Default address
		/// </summary>
		/// <remarks>For extensibility, should use an array of addresses here</remarks>
		private System.Net.IPAddress defaultIpAddress = System.Net.IPAddress.Parse ( "127.0.0.1" );

		/// <summary>
		/// Here, the server listener
		/// </summary>
		private System.Net.Sockets.TcpListener portListener;

		// thread states
		private bool stopServer = false ,
					 stopRemoveConnectionsThread = false;

		/// <summary>
		/// Thread server runs on
		/// </summary>
		private System.Threading.Thread serverThread;

		/// <summary>
		/// Separate thread which monitors and removes connections marked for termination
		/// </summary>
		private System.Threading.Thread removeConnectionsThread;

		/// <summary>
		/// Collection of active connection threads
		/// </summary>
		private static List<TcpSocketListener> socketListeners;

		/// <summary>
		/// Limit of number of primes
		/// </summary>
		private static int primeCount = 13214;

		/// <summary>
		/// Progressive series of prime number
		/// </summary>
		private static List<int> primes;

		/// <summary>
		/// Event Log
		/// </summary>
		public static System.Diagnostics.EventLog EventLog;

		#endregion Properties


		#region Constructors and Destructors

		/// <summary>
		/// Default
		/// </summary>
		public TcpServer ( )
		{
			Initializer ( new IPEndPoint ( defaultIpAddress , this.defaultPort ) );
		}

		/// <summary>
		/// Overload
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <remarks>Not used in this implementation, for future scalability</remarks>
		public TcpServer ( IPAddress ipAddress )
		{
			Initializer ( new IPEndPoint ( ipAddress , this.defaultPort ) );
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~TcpServer ( )
		{
			StopServer ( );
		}

		#endregion Constructors and Destructors


		#region Methods

		/// <summary>
		/// Initializes the server and event logging
		/// </summary>
		/// <param name="ipEndPoint"></param>
		private void Initializer ( IPEndPoint ipEndPoint )
		{
			try
			{
				this.portListener = new TcpListener ( ipEndPoint );
			}
			catch ( System.Exception ex )
			{
				this.portListener = null;
			}

			primes = this.GeneratePrimesNaive ( primeCount );

			string eventSourceName = "ConnectionSource" ,
					eventLogName = "ConnectionLog";

			EventLog = new System.Diagnostics.EventLog ( );

			if ( !System.Diagnostics.EventLog.SourceExists ( eventSourceName ) )
			{
				System.Diagnostics.EventLog.CreateEventSource ( eventSourceName , eventLogName );
			}

			EventLog.Source = eventSourceName;

			EventLog.Log = eventLogName;
		}

		/// <summary>
		/// Starts the TcpServer instance
		/// </summary>
		public void StartServer ( )
		{
			EventLog.WriteEntry ( "StartServer called" );

			if ( this.portListener != null )
			{
				socketListeners = new List<TcpSocketListener> ( );

				this.portListener.Start ( );

				this.serverThread = new System.Threading.Thread (
										new System.Threading.ThreadStart ( ServerThreadStart ) );

				this.serverThread.Start ( );

				this.removeConnectionsThread = new System.Threading.Thread (
										new System.Threading.ThreadStart ( RemoveConnectionsThreadStart ) );

				this.removeConnectionsThread.Priority = System.Threading.ThreadPriority.Lowest;

				this.removeConnectionsThread.Start ( );
			}
		}

		/// <summary>
		/// Starts thread server runs on
		/// </summary>
		private void ServerThreadStart ( )
		{
			Socket socket = null;

			TcpSocketListener socketListener = null;

			while ( !this.stopServer )
			{
				if ( ConnectionCount < this.maxConnections )
				{
					try
					{
						socket = this.portListener.AcceptSocket ( );

						socketListener = new TcpSocketListener ( socket );

						lock ( socketListeners )
						{
							socketListeners.Add ( socketListener );
						}

						socketListener.StartSocketListener ( );
					}
					catch ( System.Net.Sockets.SocketException ex )
					{
						this.stopServer = true;
					}
				}
			}
		}

		/// <summary>
		/// Thread which runs remove connections functionality
		/// </summary>
		private void RemoveConnectionsThreadStart ( )
		{
			while ( !this.stopRemoveConnectionsThread )
			{
				List<TcpSocketListener> deleteList = new List<TcpSocketListener> ( );

				lock ( socketListeners )
				{
					// Find sockets to delete
					foreach ( TcpSocketListener socketListener in socketListeners )
					{
						if ( socketListener.IsMarkedForDeletion ( ) )
						{
							deleteList.Add ( socketListener );

							socketListener.StopSocketListener ( );
						}
					}

					// Delete socket objects
					for ( int i = 0 ; i < deleteList.Count ( ) ; i++ )
					{
						socketListeners.Remove ( deleteList [ i ] );
					}
				}

				deleteList = null;

				Thread.Sleep ( 5000 );
			}
		}

		/// <summary>
		/// Stops the server instance
		/// </summary>
		public void StopServer ( )
		{
			EventLog.WriteEntry ( "StopServer called" );

			if ( this.portListener != null )
			{
				this.stopServer = true;

				this.portListener.Stop ( );

				this.serverThread.Join ( 1000 );

				if ( this.serverThread.IsAlive )
				{
					this.serverThread.Abort ( );
				}

				this.serverThread = null;

				this.stopRemoveConnectionsThread = true;

				this.removeConnectionsThread.Join ( 1000 );

				if ( this.removeConnectionsThread.IsAlive )
				{
					this.removeConnectionsThread.Abort ( );
				}

				this.removeConnectionsThread = null;

				this.portListener = null;

				StopAllSockeListeners ( );
			}
		}

		/// <summary>
		/// Quits all socket connections
		/// </summary>
		private void StopAllSockeListeners ( )
		{
			foreach ( TcpSocketListener socketListener in socketListeners )
			{
				socketListener.StopSocketListener ( );
			}

			socketListeners.Clear ( );

			socketListeners = null;
		}

		#endregion Methods


		#region Utils

		/// <summary>
		/// Simple & standard implementation, taken directly from http://stackoverflow.com/questions/1042902/most-elegant-way-to-generate-prime-numbers
		/// </summary>
		/// <param name="n"></param>
		/// <remarks>Sieve of Eratosthenes, Sundaram could be used to improve performance</remarks>
		private List<int> GeneratePrimesNaive ( int n )
		{
			List<int> primes = new List<int> ( );

			primes.Add ( 2 );

			int next = 3;

			while ( primes.Count < n )
			{
				int sqrt = ( int )Math.Sqrt ( next );

				bool isPrime = true;

				for ( int i = 0 ; ( int )primes [ i ] <= sqrt ; i++ )
				{
					if ( next % primes [ i ] == 0 )
					{
						isPrime = false;
						break;
					}
				}

				if ( isPrime )
				{
					primes.Add ( next );
				}

				next += 2;
			}

			return primes;
		}

		/// <summary>
		/// Method to return a random prime
		/// </summary>
		/// <returns>A random prime number</returns>
		public static int GetRandomPrime ( )
		{
			System.Random random = new System.Random ( );

			int randomInt = random.Next ( 0 , primeCount );

			return primes.ElementAt ( randomInt );
		}

		/// <summary>
		/// Tracks total number of connections made
		/// </summary>
		public static void IncrementHandshakeCount ( )
		{
			try
			{
				System.Threading.Interlocked.Increment ( ref sucessfulHandshakes );

				TcpServer.EventLog.WriteEntry ( "Successful Handshakes: " + sucessfulHandshakes.ToString ( ) );
			}
			catch ( System.Exception ex )
			{
				TcpServer.EventLog.WriteEntry ( ex.Message );
			}
		}

		#endregion Utils
	}
}
