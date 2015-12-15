using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LordJZ.Collections;
using LordJZ.Presentation;

namespace AimpBetterCoverDisplay.UI
{
    static class CoverSearcher
    {
        static CancellationTokenSource s_cts;

        public static async void Search(NowPlaying np)
        {
            CancellationTokenSource newCts = new CancellationTokenSource();
            CancellationTokenSource oldCts = Interlocked.Exchange(ref s_cts, newCts);

            if (oldCts != null)
                oldCts.Cancel();

            try
            {
                await Task.Run(() => InternalSearch(np), newCts.Token);
            }
            catch
            {
                // ignore
            }
        }

        static void InternalSearch(NowPlaying np)
        {
            ImageSource bmp = GetBitmap(np);

            Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    // ReSharper disable CompareOfFloatsByEqualityOperator
                    BitmapSource bms = bmp as BitmapSource;
                    if (bms != null && (bms.DpiX != 96.0 || bms.DpiY != 96.0))
                        bms.SetDpi(96.0, 96.0);
                    // ReSharper restore CompareOfFloatsByEqualityOperator

                    MainWindow window = (MainWindow)Application.Current.MainWindow;
                    window.SetCover(bmp);
                });
        }

        static ImageSource GetBitmap(NowPlaying np)
        {
            if (np.FileName.StartsWith("http"))
                np.FileName = Path.Combine(LordJZ.WinAPI.KnownFolders.GetPath(LordJZ.WinAPI.KnownFolder.Downloads), "radio.mp3");

            np.FileName = ExpandCuePath(np.FileName);

            ImageSource result = null;

            try
            {
                result = GetFromDirectory(Path.GetDirectoryName(np.FileName), Path.GetFileName(np.FileName));
            }
            catch
            {
                // ignore
            }

            if (result != null)
                return result;

            try
            {
                result = GetFromTags(np.FileName);
            }
            catch
            {
                // ignore
            }

            return result;
        }

        static readonly Regex s_cuePathRegex = new Regex("^(.+):\\d+$");
        static readonly Regex s_cueFileRegex = new Regex(@"^\s*FILE\s*""(.+)""[^""]*?$", RegexOptions.Multiline);
        static readonly string[] s_reverseSearchExtensions = { "mp3", "m4a", "ogg", "opus", "ape", "flac" };

        static string ExpandCuePath(string path)
        {
            Match match = s_cuePathRegex.Match(path);
            if (!match.Success)
                return path;

            path = match.Groups[1].Value;

            List<string> paths = new List<string>(s_reverseSearchExtensions.Length + 3);

            try
            {
                string cueText = System.IO.File.ReadAllText(path);

                match = s_cueFileRegex.Match(cueText);
                if (match.Success)
                {
                    string filename = match.Groups[1].Value;
                    string folder = Path.GetDirectoryName(path);
                    paths.Add(Path.Combine(folder, filename));
                    if (filename.IndexOf('"') >= 0)
                    {
                        paths.Add(Path.Combine(folder, filename.Replace("\"\"", "\"")));
                        paths.Add(Path.Combine(folder, filename.Replace("\\\"", "\"")));
                    }
                }
            }
            catch
            {
                // ignore
            }

            paths.AddRange(s_reverseSearchExtensions.Select(extension => Path.ChangeExtension(path, extension)));

            foreach (string expandedPath in paths)
            {
                try
                {
                    if (System.IO.File.Exists(expandedPath))
                        return expandedPath;
                }
                catch
                {
                    // ignore
                }
            }

            return path;
        }

        static ImageSource GetFromDirectory(string directoryName, string fileName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return null;

            string[] patterns =
            {
                Path.ChangeExtension(fileName, "jpg"),
                Path.ChangeExtension(fileName, "png"),
                Path.ChangeExtension(fileName, "jpeg"),
                "folder.*",
                "cover.*",
                "*.jpg",
                "*.png",
                "*.jpeg"
            };

            string[] files;
            try
            {
                files = patterns.SelectMany(pattern => Directory.GetFiles(directoryName, pattern)).ToArray();
            }
            catch
            {
                files = EmptyArray<string>.Instance;
            }
            foreach (string filename in files)
            {
                try
                {
                    return BitmapFrame.Create(new Uri(filename),
                                              BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                catch
                {
                    // ignore
                }

                // WPF can't load some images for some reason, so try GDI+
                try
                {
                    return GetBitmapOnUIThread(filename);
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        static ImageSource GetFromTags(string filename)
        {
            using (TagLib.File file = TagLib.File.Create(filename))
            {
                foreach (byte[] data in CollectPictures(file))
                {
                    Stream stream = new MemoryStream(data);

                    try
                    {
                        return BitmapFrame.Create(stream,
                            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                    catch
                    {
                        stream.Position = 0;
                    }

                    // WPF can't load some images for some reason, so try GDI+
                    try
                    {
                        return GetBitmapOnUIThread(stream);
                    }
                    catch
                    {
                        stream.Position = 0;
                    }
                }
            }

            return null;
        }

        static IEnumerable<byte[]> CollectPictures(TagLib.File file)
        {
            List<byte[]> pics = new List<byte[]>();
            try
            {
                foreach (TagLib.IPicture pic in file.Tag.Pictures)
                {
                    try
                    {
                        pics.Add(pic.Data.Data);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                // ignore
            }

            TagLib.Ogg.GroupedComment grouped = file.Tag as TagLib.Ogg.GroupedComment;
            if (grouped != null)
            {
                try
                {
                    ExtractOggPics(grouped, pics);
                }
                catch
                {
                    // ignore
                }
            }

            return pics;
        }

        static void ExtractOggPics(TagLib.Ogg.GroupedComment grouped, List<byte[]> pics)
        {
            foreach (TagLib.Ogg.XiphComment comment in grouped.Comments)
            {
                try
                {
                    string[] fields = comment.GetField("METADATA_BLOCK_PICTURE");
                    if (fields == null)
                        continue;

                    foreach (string field in fields)
                    {
                        try
                        {
                            byte[] flacPic = Convert.FromBase64String(field);
                            TagLib.Flac.Picture pic = new TagLib.Flac.Picture(new TagLib.ByteVector(flacPic));
                            pics.Add(pic.Data.Data);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        static ImageSource GetBitmapOnUIThread(Stream stream)
        {
            return GetBitmapOnUIThread(() => new Bitmap(stream));
        }

        static ImageSource GetBitmapOnUIThread(string filename)
        {
            return GetBitmapOnUIThread(() => new Bitmap(filename));
        }

        static ImageSource GetBitmapOnUIThread(Func<Bitmap> factory)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                using (Bitmap bitmap = factory())
                    return bitmap.ToImageSource();
            });
        }
    }
}
