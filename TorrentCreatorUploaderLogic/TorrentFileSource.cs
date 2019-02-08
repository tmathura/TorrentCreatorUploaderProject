﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoTorrent.Common;

namespace TorrentCreatorUploaderLogic
{
    public class TorrentFileSource : ITorrentFileSource
    {
        public TorrentFileSource(string path)
            : this(path, true)
        {
        }

        public TorrentFileSource(string path, bool ignoreHidden)
            : this(path, true, "*")
        {
            IgnoreHidden = ignoreHidden;
        }

        public TorrentFileSource(string path, bool ignoreHidden, string filter)
        {
            //Error check filter and use default if it is invaild
            if (string.IsNullOrEmpty(filter) || filter.EndsWith("..", StringComparison.Ordinal) ||
                filter.Contains(".." + System.IO.Path.DirectorySeparatorChar) ||
                filter.Contains(".." + System.IO.Path.AltDirectorySeparatorChar) ||
                System.IO.Path.GetInvalidPathChars().Any(x => x != '|' && filter.Contains(x)))
                Filter = "*";
            else
                Filter = filter;

            IgnoreHidden = ignoreHidden;
            Path = path;
            LoadFiles();
        }

        public string Filter { get; }

        public string Path { get; }

        public IEnumerable<FileMapping> Files { get; private set; }

        public bool IgnoreHidden { get; }

        public string TorrentName { get; set; }

        private void LoadFiles()
        {
            var sep = System.IO.Path.DirectorySeparatorChar;
            var fullPath = System.IO.Path.GetFullPath(Path);
            if (File.Exists(fullPath))
            {
                TorrentName = System.IO.Path.GetFileName(fullPath);
                Files = new List<FileMapping> {new FileMapping(fullPath, TorrentName)};
                return;
            }

            if (!fullPath.EndsWith(sep.ToString()))
                fullPath += sep.ToString();

            // Process all directories and subdirectories of this folder
            // and add all the files to the 'files' list.
            var files = new List<string>();
            var directories = new Queue<string>();
            directories.Enqueue(fullPath);
            while (directories.Count > 0)
            {
                var current = directories.Dequeue();
                if (IgnoreHidden)
                {
                    var info = new DirectoryInfo(current);
                    if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;
                }

                foreach (var s in Directory.GetDirectories(current))
                    directories.Enqueue(s);

                files.AddRange(Filter.Split('|').SelectMany(x => Directory.GetFiles(current, x)).Distinct());
            }

            // If we're ignoring hidden files, remove all the files with the hidden attribute
            if (IgnoreHidden)
                files.RemoveAll(
                    file => (new FileInfo(file).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);

            // Turn the full path of each file into a full path + relative path. The relative path is the 'path'
            // which the file will have within the .torrent metadata.
            Files = files.ConvertAll(file => new FileMapping(file, file.Substring(fullPath.Length)));

            // Set the torrent name (user can change it later)
            TorrentName = new DirectoryInfo(fullPath).Name;
        }
    }
}