using System.Diagnostics.CodeAnalysis;

namespace Wukong.Models
{
    public class ClientSong
    {
        public string SiteId { get; set; }
        public string SongId { get; set; }
        public string WithCookie { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            var song = obj as ClientSong;
            if (null == song)
            {
                return false;
            }

            return SiteId == song.SiteId && SongId == song.SongId;
        }

        public bool IsEmpty() => SiteId == null || SongId == null;

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 23 + SongId.GetHashCode();
            hash = hash * 23 + SiteId.GetHashCode();
            return hash;
        }

        public static bool operator ==(ClientSong a, ClientSong b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if ((object) a == null || (object) b == null)
            {
                return false;
            }
            return a.Equals(b);
        }

        public static bool operator !=(ClientSong a, ClientSong b)
        {
            return !(a == b);
        }
    }

    public class Song : SongInfo
    {
        public Files[] Musics;
        public double Length;
        public int Bitrate;
        public Files Mv;
    }

    public class SongInfo : ClientSong
    {
        public string Artist;
        public string Album;
        public Files Artwork;
        public string Title;
        public Lyric[] Lyrics;
        public string WebUrl;
        public string MvId;
        public string MvWebUrl;
    }

    public class Lyric
    {
        public bool Lrc;
        public bool Translated;
        public string Data;
    }

    public class Files
    {
        public string AudioQuality;
        public int AudioBitrate;
        public string Format;
        public string File;
        public string FileViaCdn;
    }
}
