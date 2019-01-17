//
// UBCopySetup.cs
//
// Authors:
//  Wesley D. Brown <wes@planetarydb.com>
//
// Copyright (C) 2010 SQLServerIO (http://www.SQLServerIO.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace UBCopy
{
    public static class UBCopySetup
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UBCopySetup));

        public static Stack<string> FileList = new Stack<string>(50000);

        public static Stack<FileToFolder> FileToFolderList = new Stack<FileToFolder>(50000);        

        //hold command line options
        //public static string Sourcefile;
        public static string Destinationfile;
        public static bool Overwritedestination = true;
        //we set an inital buffer size to be on the safe side.
        public static int Buffersize = 1;
        public static bool Checksumfiles;
        public static bool Reportprogres;
        public static bool Movefile;
        public static int SynchronousFileCopySize = 32;
        public static Int64 BytesCopied;
        public static int BytesSecond;
        public static bool ForceCopy;
        public static bool SyncFolders;
        public static string Exclusionlist;
        public static bool Listlocked;

        public static readonly object DictonaryLocker = new object();
        /// <summary>
        /// get a list of all files and folders and push them into the queue
        /// </summary>
        /// <param name="root"></param>
        public static void TraverseTree(string root)
        {
            string[] destinationFolderList = Destinationfile.Contains(",") || Destinationfile.Contains(";") ? Destinationfile.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[] { Destinationfile };

            string[] exclusionlist = Exclusionlist != null ? Exclusionlist.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToArray() : null;

            var dirs = new Stack<string>(20);

            if (!Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                var currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Log.Error(e);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Log.Error(e);
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {
                    Log.Error(e);
                    continue;
                }

                catch (DirectoryNotFoundException e)
                {
                    Log.Error(e);
                    continue;
                }
                foreach (var str in subDirs)
                    dirs.Push(str);

                foreach (var file in files)
                {
                    if (exclusionlist!= null && exclusionlist.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (file.Length < 261)
                    {
                        try
                        {
                            FileList.Push(file);
                            
                            foreach (var folder in destinationFolderList)
                            {
                                FileToFolderList.Push(new FileToFolder(file, folder));
                            }

                        }
                        catch (FileNotFoundException)
                        {
                            Log.ErrorFormat("File Not Found:{0} ", file);
                            continue;
                        }
                    }
                    else
                    {
                        Log.ErrorFormat("File Name or Path is too long:{0}", file);
                    }
                }
            }
        }
    }

    public class FileToFolder
    {
        public string SourceFile { get; set; }
        public string DestinationFolder { get; set; }

        public FileToFolder(string sourceFile, string destinationFolder)
        {
            SourceFile = sourceFile;
            DestinationFolder = destinationFolder;
        }
    }
}