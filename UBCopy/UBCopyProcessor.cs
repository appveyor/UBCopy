using System;
using System.IO;
using System.Threading;
using log4net;

namespace UBCopy
{
    class UBCopyProcessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UBCopyProcessor));

        private readonly ManualResetEvent _doneEvent;

        private static readonly object Locker = "";

        /// <summary>
        /// called to spool up copy
        /// </summary>
        /// <param name="doneEvent"></param>
        public UBCopyProcessor(ManualResetEvent doneEvent)
        {
            _doneEvent = doneEvent;
        }
        /// <summary>
        /// setup callback
        /// </summary>
        /// <param name="threadContext"></param>
        public void UBCopyThreadPoolCallback(Object threadContext)
        {
            UBCopyFile();
            _doneEvent.Set();
        }

        /// <summary>
        /// handles the file copy
        /// </summary>
        private static void UBCopyFile()
        {

            FileToFolder fileToFolder;

            lock (UBCopySetup.DictonaryLocker)
            {

                fileToFolder = UBCopySetup.FileToFolderList.Pop();
                Log.DebugFormat("POP FILE: {0} TO FOLDER {1}", fileToFolder.SourceFile, fileToFolder.DestinationFolder);
            }
            if (fileToFolder == null)
            {
                throw new Exception("File Name Cannot Be Null");
            }
            //var destinationfile = Path.Combine(UBCopySetup.Destinationfile, file.Replace(Path.GetPathRoot(file), ""));
            var destinationfile = Path.Combine(fileToFolder.DestinationFolder, Path.GetFileName(fileToFolder.SourceFile));
            var fileSize = new FileInfo(fileToFolder.SourceFile);

            Log.DebugFormat("File Size: {0}", fileSize.Length);
            lock (Locker)
            {
                UBCopySetup.BytesCopied += fileSize.Length;
            }
            if (fileSize.Length < UBCopySetup.SynchronousFileCopySize)
            {
                var asyncUnbufferedCopy = new AsyncUnbuffCopy();
                asyncUnbufferedCopy.AsyncCopyFileUnbuffered(fileToFolder.SourceFile, destinationfile, UBCopySetup.Overwritedestination,
                                                            UBCopySetup.Movefile,
                                                            UBCopySetup.Checksumfiles, UBCopySetup.Buffersize,
                                                            UBCopySetup.Reportprogres,UBCopySetup.BytesSecond);
            }
            else
            {
                AsyncUnbuffCopyStatic.AsyncCopyFileUnbuffered(fileToFolder.SourceFile, destinationfile, UBCopySetup.Overwritedestination,
                                                            UBCopySetup.Movefile,
                                                            UBCopySetup.Checksumfiles, UBCopySetup.Buffersize,
                                                            UBCopySetup.Reportprogres, UBCopySetup.BytesSecond);
            }
        }
    }
}
