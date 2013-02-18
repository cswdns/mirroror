using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using log4net;

namespace MirrororLib
{
    public class Mirroror
    {
        static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public int RetryLimit { get; set; }
        public TimeSpan MirrorPeriod { get; set; }
        public ISource Source { get; private set; }
        public IMirror[] Mirrors {get; private set;}
        private Thread _mirrorThread;
        private bool _running;
        object _lockObject = new object();

        private bool Running
        {
            get
            {
                lock (_lockObject)
                    return _running;
            }
            set
            {
                lock (_lockObject)
                    _running = value;
            }
        }

        public Mirroror(ISource source, params IMirror[] mirrors)
        {
            log4net.Config.XmlConfigurator.Configure();

            MirrorPeriod = new TimeSpan(1, 0, 0, 0);
            RetryLimit = 5;
            Source = source;
            Mirrors = mirrors;

            _mirrorThread = new Thread(MirrorThreadFunction);
        }

        public void Start()
        {
            _log.Info("Starting Mirroror...");
            _mirrorThread.Start();
        }

        public void Stop()
        {
            _log.Info("Stopping Mirroror...");
            Running = false;
            _eventWaitHandle.Set();
            _mirrorThread.Join();
            _log.Info("Mirroror stopped.");
        }

        private void MirrorThreadFunction()
        {
            for (Running = true; Running; )
            {
                _log.Info("Periodic mirror operation starting...");
                Mirror();
                _log.Info("Periodic mirror operation completed.");

                _eventWaitHandle.WaitOne(MirrorPeriod);
            }
        }

        private void Mirror()
        {
            foreach (IMirror mirror in Mirrors)
            {
                if (!Running)
                    break;

                if (_log.IsInfoEnabled)
                    _log.Info(string.Format("Beginning mirror operation from {0} to {1}", Source.FullName, mirror.FullName));
                
                Mirror(Source, mirror);

                if (_log.IsInfoEnabled)
                    _log.Info(string.Format("Completed mirror operation from {0} to {1}", Source.FullName, mirror.FullName));
            }
        }

        private void Mirror(ISource source, IMirror mirror)
        {
            for (int retryCount = 0; ; retryCount++)
            {
                if (!Running)
                    break;

                try
                {
                    MirrorFiles(source.Files, mirror);
                    MirrorDirectories(source.SubSources, mirror);
                    break;
                }
                catch (DirectoryNotFoundException dnfe)
                {
                    _log.Warn("Directory not found", dnfe);
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    if (retryCount == RetryLimit)
                        throw;
                    _log.Error(string.Format("Error encountered.  Attempting retry number {0}", retryCount), e);
                }
            }
        }

        private IFile CopyFileToMirror(IMirror mirror, IFile sourceFile)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Copying {0} to {1}...", sourceFile.Name, mirror.FullName));

            IFile retVal = mirror.CopyFile(sourceFile);

            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Finished copying {0} to {1}", sourceFile.Name, mirror.FullName));

            return retVal;
        }

        private IMirror CreateSubMirror(IMirror mirror, string subMirrorName)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Creating submirror {0} in {1}...", subMirrorName, mirror.FullName));

            IMirror retVal = mirror.CreateSubMirror(subMirrorName);

            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Finished creating submirror {0} in {1}...", subMirrorName, mirror.FullName));

            return retVal;
        }

        private void DeleteSubMirror(IMirror mirror, IMirror subMirror)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Deleting submirror {0} from {1}...", subMirror.Name, mirror.FullName));

            mirror.DeleteSubMirror(subMirror);

            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Finishsed deleting submirror {0} from {1}...", subMirror.Name, mirror.FullName));
        }

        private void DeleteFileFromMirror(IMirror mirror, IFile file)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Deleting file {0} from {1}...", file.Name, mirror.FullName));

            mirror.DeleteFile(file);

            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Finished deleting file {0} from {1}...", file.Name, mirror.FullName));
        }

        private void MirrorFiles(IEnumerable<IFile> sourceFiles, IMirror mirror)
        {
            HashSet<string> mirrorFilesToKeep = new HashSet<string>();
            List<IFile> mirrorFilesToDelete = new List<IFile>();
            foreach (IFile sourceFile in sourceFiles)
            {
                if (!Running)
                    break;

                IFile mirrorFile = mirror.FindFile(sourceFile.Name);
                if (mirrorFile == null)
                    mirrorFile = CopyFileToMirror(mirror, sourceFile);
                else if (sourceFile.LastUpdated > mirrorFile.LastUpdated)
                    CopyFileToMirror(mirror, sourceFile);

                if (mirrorFile != null)
                    mirrorFilesToKeep.Add(mirrorFile.Name);
            }

            foreach (IFile mirrorFile in mirror.Files)
            {
                if (!Running)
                    break;

                if (!mirrorFilesToKeep.Contains(mirrorFile.Name))
                    DeleteFileFromMirror(mirror, mirrorFile);
            }
        }

        private void MirrorDirectories(IEnumerable<ISource> subSources, IMirror mirror)
        {
            HashSet<string> mirrorsToKeep = new HashSet<string>();
            List<IMirror> mirrorsToDelete = new List<IMirror>();
            foreach (ISource source in subSources)
            {
                if (!Running)
                    break;

                string name = source.Name;
                IMirror subMirror = mirror.FindSubMirror(name);
                if (subMirror == null)
                    subMirror = CreateSubMirror(mirror, name);

                Mirror(source, subMirror);

                mirrorsToKeep.Add(subMirror.Name);
            }

            foreach (IMirror subMirror in mirror.SubMirrors)
            {
                if (!Running)
                    break;

                if (!mirrorsToKeep.Contains(subMirror.Name))
                    DeleteSubMirror(mirror, subMirror);
            }
        }
    }
}
