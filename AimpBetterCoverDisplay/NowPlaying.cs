using System;

namespace AimpBetterCoverDisplay
{
    public class NowPlaying : IEquatable<NowPlaying>
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        public bool Equals(NowPlaying other)
        {
            return this.FileName == other.FileName &&
                   this.Title == other.Title &&
                   this.Artist == other.Artist &&
                   this.Album == other.Album;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as NowPlaying);
        }

        public static bool operator ==(NowPlaying np1, NowPlaying np2)
        {
            if (ReferenceEquals(np1, np2))
                return true;

            if (ReferenceEquals(np1, null) || ReferenceEquals(np2, null))
                return false;

            return np1.Equals(np2);
        }

        public static bool operator !=(NowPlaying np1, NowPlaying np2)
        {
            return !(np1 == np2);
        }

        public override int GetHashCode()
        {
            string filename = this.FileName;
            return filename != null ? filename.GetHashCode() : 0;
        }
    }
}
