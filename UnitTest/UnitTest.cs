using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Timers;
using nsWinSer = BasicTcpServerWindowsService;

namespace UnitTest
{
	[TestClass]
	public class UnitTest
	{
		#region Properties

		private nsWinSer.TcpServer server;

		#endregion Properties


		#region Test Methods

		[TestMethod]
		public void Construct ( )
		{
			this.server = new nsWinSer.TcpServer ( );

			//try
			//{
			//	this.server.GetType ( ).InvokeMember (
			//			"OnStart" ,
			//				System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance ,
			//					null ,
			//						 this.server , // this.server.GetType ( ) ,
			//							new object [ ] { new string [ ] { } } );
			//}
			//catch ( System.Exception ex )
			//{
			//	Assert.IsTrue ( false , "remote OnStart attempt" );
			//}

			// Get number of connections before connect
			int initialConnectionCount = nsWinSer.TcpServer.ConnectionCount;

			// Get number of handshakes before connect
			int initialHandShakeCount = nsWinSer.TcpServer.SucessfulHandshakes;

			// Connect
			System.Net.IPAddress testIpAddress = System.Net.IPAddress.Parse ( "127.0.0.2" );

			System.Net.Sockets.TcpClient connection = new TcpClient ( new System.Net.IPEndPoint ( testIpAddress , 55556 ) );

			try
			{
				connection.Connect ( new System.Net.IPEndPoint ( System.Net.IPAddress.Parse ( "127.0.0.1" ) , 55555 ) );
			}
			catch ( SocketException ex )
			{
				Assert.IsTrue ( false , "Could not connect to windows service" );
			}

			Assert.IsTrue ( connection.Connected , "Not Connected" );

			// Wait to test the 5 second timeout
			System.Threading.Thread.Sleep ( 6000 );

			Assert.IsTrue ( nsWinSer.TcpServer.ConnectionCount - initialConnectionCount == 1 ,
							"Connection did not timeout" );

			// Test handshake counter worked
			Assert.IsTrue ( nsWinSer.TcpServer.SucessfulHandshakes - initialHandShakeCount == 1 ,
							"Handshake counter did not increment" );

			//// Now test everything else

			// Current number of connections
			initialConnectionCount = nsWinSer.TcpServer.ConnectionCount;

			// Current number of handshakes
			initialHandShakeCount = nsWinSer.TcpServer.SucessfulHandshakes;

			// CONNECTIONS request
			connection.Client.SendTo ( System.Text.Encoding.ASCII.GetBytes ( "CONNECTIONS" ) ,
										connection.Client.LocalEndPoint );

			int size = 0;

			Byte[ ] bytes = new Byte [ 1024 ];

			size = connection.Client.Receive ( bytes , 0 , 1024 , SocketFlags.None );

			char [ ] chars = new char [ size ];

			System.Text.Decoder decoder = System.Text.Encoding.ASCII.GetDecoder ( );

			string received = new System.String ( chars );

			Assert.IsTrue ( Convert.ToInt64 ( received ) - initialConnectionCount == 1 , "Number of current connections is wrong" );

			// COUNT request
			connection.Client.SendTo ( System.Text.Encoding.ASCII.GetBytes ( "COUNT" ) ,
										connection.Client.LocalEndPoint );

			size = 0;

			bytes = new Byte [ 1024 ];

			size = connection.Client.Receive ( bytes , 0 , 1024 , SocketFlags.None );

			chars = new char [ size ];

			decoder = System.Text.Encoding.ASCII.GetDecoder ( );

			received = new System.String ( chars );

			Assert.IsTrue ( Convert.ToInt64 ( received ) - initialConnectionCount == 1 , "Handshake count is wrong" );

			// PRIME request
			connection.Client.SendTo ( System.Text.Encoding.ASCII.GetBytes ( "PRIME" ) ,
										connection.Client.LocalEndPoint );

			size = 0;

			bytes = new Byte [ 1024 ];

			size = connection.Client.Receive ( bytes , 0 , 1024 , SocketFlags.None );

			chars = new char [ size ];

			decoder = System.Text.Encoding.ASCII.GetDecoder ( );

			received = new System.String ( chars );

			Assert.IsTrue ( this.IsPrime ( Convert.ToInt32 ( received ) ) , "Not prime" );

			// TERMINATE
			connection.Client.SendTo ( System.Text.Encoding.ASCII.GetBytes ( "TERMINATE" ) ,
										connection.Client.LocalEndPoint );

			Assert.IsTrue ( initialConnectionCount - nsWinSer.TcpServer.ConnectionCount == 1 ,
							"Connection count did not decrement" );
		}

		[TestMethod]
		public void Primes ( )
		{
			int prime = nsWinSer.TcpServer.GetRandomPrime ( );

			Assert.IsNotNull ( prime );

			Assert.IsTrue ( prime > 0 , "Not greater than 0" );

			Assert.IsTrue ( this.IsPrime ( prime ) , "Not prime" );
		}

		#endregion Test Methods


		#region Utils

		/// <summary>
		/// Method to test if an integer is prime
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public bool IsPrime ( int number )
		{
			if ( ( number & 1 ) == 0 )
			{
				return number == 2;
			}

			for ( int i = 3 ; ( i * i ) <= number ; i += 2 )
			{
				if ( ( number % i ) == 0 )
				{
					return false;
				}
			}

			return number != 1;
		}

		#endregion Utils
	}
}
