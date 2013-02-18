using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MirrororLib;
using log4net;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Press 'Enter' to terminate...");

                log4net.Config.XmlConfigurator.Configure();


                //            Mirroror mirroror = new Mirroror(new DriveVolume("c:\\pics"), new FtpVolume("andrew-winston.com/winston-family.com/LG-NAS_Multimedia/pics", "awinston", "Gorelik1"));
                Mirroror mirroror = new Mirroror(new DriveVolume("c:\\pics"), new DriveVolume("c:\\"));

                mirroror.Start();
                Console.ReadLine();
                mirroror.Stop();
            }
            finally
            {
            }
        }
    }
}
