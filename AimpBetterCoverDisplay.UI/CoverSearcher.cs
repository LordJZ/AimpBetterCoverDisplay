using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LordJZ.Collections;
using TagLib;
using File = System.IO.File;

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
            Bitmap bmp = GetBitmap(np);

            Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    MainWindow window = (MainWindow)Application.Current.MainWindow;
                    window.SetCover(bmp);
                });
        }

        static Bitmap GetBitmap(NowPlaying np)
        {
            Bitmap result = null;

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

        static Bitmap GetFromDirectory(string directoryName)
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
                    MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(filename));
                    return (Bitmap)Image.FromStream(memoryStream);
                }
                catch
                {
                }
            }

            return null;
        }

        static Bitmap GetFromTags(string filename)
        {
            using (TagLib.File file = TagLib.File.Create(filename))
            {
                IPicture pic = file.Tag.Pictures.FirstOrDefault();
                if (pic == null)
                    return null;

                byte[] data = pic.Data.Data;
                return (Bitmap)Image.FromStream(new MemoryStream(data));
            }
        }
    }
}
