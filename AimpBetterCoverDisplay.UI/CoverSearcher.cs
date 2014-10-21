using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LordJZ.Collections;
using LordJZ.Presentation;
using TagLib;

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
            ImageSource result = null;

            try
            {
                result = GetFromDirectory(Path.GetDirectoryName(np.FileName));
            }
            catch
            {
            }

            if (result != null)
                return result;

            try
            {
                result = GetFromTags(np.FileName);
            }
            catch
            {
            }

            return result;
        }

        static ImageSource GetFromDirectory(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return null;

            string[] patterns = { "cover.*", "*.jpg", "*.png", "*.jpeg" };
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
                }

                // WPF can't load some images for some reason, so try GDI+
                try
                {
                    return GetBitmapOnUIThread(filename);
                }
                catch
                {
                }
            }

            return null;
        }

        static ImageSource GetFromTags(string filename)
        {
            using (TagLib.File file = TagLib.File.Create(filename))
            {
                foreach (IPicture pic in file.Tag.Pictures)
                {
                    Stream stream;
                    try
                    {
                        stream = new MemoryStream(pic.Data.Data);
                    }
                    catch
                    {
                        continue;
                    }

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
