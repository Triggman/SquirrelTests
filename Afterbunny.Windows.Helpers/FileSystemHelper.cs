using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Afterbunny.Windows.Helpers
{
    public static class FileSystemHelper
    {
        public static string GetFileFromDialog(string initialDir = null,
            (string[] extensions, string fileTypePrompt)? pattern = null, string cacheFile = null)
        {
            if (!string.IsNullOrEmpty(cacheFile) && File.Exists(cacheFile))
            {
                initialDir = File.ReadAllText(cacheFile);
            }

            if (string.IsNullOrEmpty(initialDir))
                initialDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


            string filter;
            if (pattern != null)
            {
                var exp = string.Join(";", pattern.Value.extensions.Select(ex => $"*.{ex}"));
                filter = $"{pattern.Value.fileTypePrompt} ({exp})|{exp}";
            }
            else
            {
                filter = "All files (*.*)|*.*";
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = initialDir;
                openFileDialog.Filter = filter;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var selectedPath = openFileDialog.FileName;
                    if (!string.IsNullOrEmpty(cacheFile))
                    {
                        var cacheFileDir = Directory.GetParent(cacheFile).FullName;
                        Directory.CreateDirectory(cacheFileDir);
                        File.WriteAllText(cacheFile, Directory.GetParent(selectedPath).FullName);
                    }


                    //Get the path of specified file
                    return selectedPath;
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// Unhandled exceptions will be logged to <see cref="logDir"/>
        /// </summary>
        /// <param name="logDir"></param>
        public static void SetupUnhandledExceptionLogging(string logDir)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = (Exception)args.ExceptionObject;
                var lines = new[] { DateTime.Now.ToString("yyyy-MMdd-HHmm:ss_fff"), ex.GetType().ToString(), ex.Message, ex.StackTrace };
                Directory.CreateDirectory(logDir);
                var filePath = Path.Combine(logDir, $"UnhandledExceptions_{DateTime.Now:MMdd_HHmm_ss}.txt");
                File.WriteAllLines(filePath, lines);
            };
        }



        public static string GetDirFromDialog(string initialDir = null, string cacheFile = null)
        {
            if (!string.IsNullOrEmpty(cacheFile) && File.Exists(cacheFile))
            {
                initialDir = File.ReadAllText(cacheFile);
            }

            if (string.IsNullOrEmpty(initialDir))
                initialDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = initialDir;
                var result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    var selectedDir = fbd.SelectedPath;
                    if (!string.IsNullOrEmpty(cacheFile))
                    {
                        var cacheFileDir = Directory.GetParent(cacheFile).FullName;
                        Directory.CreateDirectory(cacheFileDir);
                        File.WriteAllText(cacheFile, selectedDir);
                    }

                    return selectedDir;
                }
            }

            return string.Empty;
        }

        public static void RemoveEmptyDirsRecursively(string dir)
        {
            if (String.IsNullOrEmpty(dir))
                throw new ArgumentException(
                    "Starting directory is a null reference or an empty string",
                    "dir");

            try
            {
                foreach (var d in Directory.EnumerateDirectories(dir))
                {
                    RemoveEmptyDirsRecursively(d);
                }

                var entries = Directory.EnumerateFileSystemEntries(dir);

                if (!entries.Any())
                {
                    try
                    {
                        Directory.Delete(dir);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static string GetAssemblyVersionByType(Type type)
        {
            return type.Assembly.GetName().Version.ToString();
        }

        public static DateTime GetLinkerTime(Type type, TimeZoneInfo target = null)
        {
            var assembly = type.Assembly;
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        /// <summary>
        /// Remove outdated files recursively if outdated
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="expireTimeSpan"></param>
        /// <param name="extension">if specified only files with the specified extension will be targeted</param>
        public static void RemoveOutdatedFiles(string dir, TimeSpan expireTimeSpan, string extension = null)
        {
            Directory.CreateDirectory(dir);

            if (!string.IsNullOrEmpty(extension) && extension.StartsWith(".")) extension = extension.Substring(1);
            var searchPattern = string.IsNullOrEmpty(extension) ? "*.*" : $"*.{extension}";
            var files = Directory.EnumerateFiles(dir, searchPattern, SearchOption.AllDirectories);
            foreach (string path in files)
            {
                var timeSpan = DateTime.Now - File.GetCreationTime(path);
                if (timeSpan > expireTimeSpan)
                    try
                    {
                        File.Delete(path);
                    }
                    catch
                    {
                        // Don't care
                    }
            }
        }
    }
}