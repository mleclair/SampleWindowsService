using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace BasicTcpServerWindowsService
{
	[RunInstaller ( true )]
	public partial class ProjectInstaller : System.Configuration.Install.Installer
	{
		public ProjectInstaller ( )
		{
			InitializeComponent ( );

			this.AfterInstall += new InstallEventHandler ( ProjectInstaller_AfterInstall );
		}

		private void ProjectInstaller_AfterInstall ( object sender , InstallEventArgs e )
		{
			ServiceController serviceController = new ServiceController ( "TcpConnectionService" );

			serviceController.Start ( );
		}

		private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
		{

		}

		private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
		{

		}
	}
}
