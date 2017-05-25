SampleWindowsService - readme.txt

Author: Michael Albert LeClair, 2017-03-09


How To Run "SampleWindowsService":

	Build the project. From appropriate folder, run 'installutil.exe SampleWindowsService.exe'
	Using PuTTY or some other which supports LineMode, establish a connection through 127.0.0.1 on port 55555. 
	Type 'COUNT' or 'CONNECTIONS' or 'PRIME' or 'TERMINATE', then Enter.



Files Includes With This Project:

	AssemblyInfo.cs
	App.Config
	Program.cs
	ProjectInstaller.cs
	TcpConnectionService.cs
	TcpServer.cs
	TcpSocketListener.cs



Synopsis:

A simple Windows Service which accepts non-secure connections over tcp, accepts a handshake of 'HELO',
timeouts after 5 seconds of inactivity, and accepts the following commands:
	- COUNT			Server Response : integer, number of successful handshakes
	- CONNECTIONS	Server Response : integer, number of current connections
	- PRIME			Server Response : integer, random prime number
	- TERMINATE		Server Response : 'BYE', terminate connection



Design Decisions & Project Issues:

	threading
	threadsafe
	pooling
	data structures



Profiling Results:

	- The biggest bottle neck for the program is threadsafing the connection pool / locking the collection
	- For high scalability, a data structure other than List<T> for connections should be considered. In a real world application,
		I would expect LinkedList<TcpSocketListener> to improve performance.




Notes to future me:

	- scalability... what about Denial of Service Attacks?
	- bitconverter for int to byte[]
	- explore: lock vs. slim lock
	- why didn't you just use LinkedList<TcpSocketListener> to begin with?
