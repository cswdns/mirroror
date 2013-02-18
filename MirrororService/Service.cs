using System;
using System.Linq;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using MirrororLib;

namespace MirrororService
{
    class NetworkDriveCredential
    {
        public string Unc { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

	public class Service : System.ServiceProcess.ServiceBase
    {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        Mirroror _mirroror;
        List<string> _connectedUncs = new List<string>();

        private string SourcePath
        {
            get
            {
                return ConfigurationManager.AppSettings["SourcePath"];
            }
        }

        private DriveVolume[] DriveVolumes
        {
            get
            {
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                List<DriveVolume> driveVolumeList = new List<DriveVolume>();

                for (int i = 1; ; i++)
                {
                    string destinationPath = appSettings[string.Format("DestinationPath{0}", i)];
                    if (string.IsNullOrEmpty(destinationPath))
                        break;

                    driveVolumeList.Add(new DriveVolume(destinationPath));
                }

                return driveVolumeList.ToArray();
            }
        }

        private NetworkDriveCredential ParseNetworkDriveCredential(string ndcString)
        {
            string[] ndcArray = ndcString.Split('|');
            return new NetworkDriveCredential()
            {
                Unc = ndcArray[0],
                UserName = ndcArray[1],
                Password = ndcArray[2]
            };
        }

        private NetworkDriveCredential[] NetworkDriveCredentials
        {
            get
            {
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                List<NetworkDriveCredential> networkDriveCredentialList = new List<NetworkDriveCredential>();

                for (int i = 1; ; i++)
                {
                    string networkDriveCredential = appSettings[string.Format("NetworkDriveCredential{0}", i)];
                    if (string.IsNullOrEmpty(networkDriveCredential))
                        break;

                    networkDriveCredentialList.Add(ParseNetworkDriveCredential(networkDriveCredential));
                }

                return networkDriveCredentialList.ToArray();
            }
        }


        public Service()
        {
            // This call is required by the Windows.Forms Component Designer.
            InitializeComponent();

            foreach (var ndc in NetworkDriveCredentials)
            {
                if (WindowsNetworking.ConnectToRemote(ndc.Unc, ndc.UserName, ndc.Password))
                    _connectedUncs.Add(ndc.Unc);
                else
                    throw new ApplicationException(string.Format("bad credentials {0} {1} {2}", ndc.Unc, ndc.UserName, ndc.Password));
            }

            _mirroror = new Mirroror(new DriveVolume(SourcePath), DriveVolumes);

            var appSettings = ConfigurationManager.AppSettings;

            string mirrorPeriod = appSettings["MirrorPeriod"];
            if (!string.IsNullOrEmpty(mirrorPeriod))
                _mirroror.MirrorPeriod = TimeSpan.Parse(mirrorPeriod);

            string retryLimit = appSettings["RetryLimit"];
            if (!string.IsNullOrEmpty(retryLimit))
                _mirroror.RetryLimit = int.Parse(retryLimit);
        }

		// The main entry point for the process
		static void Main()
		{
			System.ServiceProcess.ServiceBase[] ServicesToRun;
	
			// More than one user Service may run within the same process. To add
			// another service to this process, change the following line to
			// create a second service object. For example,
			//
			//   ServicesToRun = New System.ServiceProcess.ServiceBase[] {new Service1(), new MySecondUserService()};
			//
            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new MirrororService.Service() };

			System.ServiceProcess.ServiceBase.Run(ServicesToRun);	
		}

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            // 
            // Service1
            // 
            this.ServiceName = "MirrororService";

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
            foreach (string connectedUnc in _connectedUncs)
            {
                try
                {
                    WindowsNetworking.DisconnectRemote(connectedUnc);
                }
                catch (Exception)
                {
                    // eat
                }
            }

            if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Set things in motion so your service can do its work.
		/// </summary>
		protected override void OnStart(string[] args)
		{
            _mirroror.Start();
        }
 
		/// <summary>
		/// Stop this service.
		/// </summary>
		protected override void OnStop()
		{
            _mirroror.Stop();
        }

        protected override void OnContinue()
		{
		}
	}
}
