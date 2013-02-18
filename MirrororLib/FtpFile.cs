using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace MirrororLib
{
    public class FtpFile : IFile
    {
        FtpVolume _parent;

        public FtpFile(FtpVolume parent, string fileName)
        {
            _parent = parent;
            Name = fileName;
        }

        public string Name {get; private set;}

        public string FullName
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime LastUpdated
        {
            get
            {
                try
                {
                    FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(_parent.GetConnectionString(Name));

                    ftpWebRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                    FtpWebResponse response = (FtpWebResponse)ftpWebRequest.GetResponse();
                    return response.LastModified;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
    }
}
