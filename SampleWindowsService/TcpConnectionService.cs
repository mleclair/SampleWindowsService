using System.ServiceProcess;

namespace BasicTcpServerWindowsService
{
	public partial class TcpConnectionService : ServiceBase
	{
		#region Properties

		private TcpServer server = null;

		#endregion Properties


		#region Constructors

		public TcpConnectionService ( )
		{
			InitializeComponent ( );
		}

		#endregion Constructors


		#region Service Method overrides

		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		protected override void OnStart ( string [ ] args )
		{
			this.server = new TcpServer ( );

			this.server.StartServer ( );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void OnStop ( )
		{
			this.server.StopServer ( );

			this.server = null;
		}

		#endregion Service Method overrides
	}
}
