using System;
using System.Reflection;

namespace Wukong.Models
{
    public class ClientSong
    {
        public string SiteId { get; set; }
        public string SongId { get; set; }

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

    public class ClientSongData : ClientSong
    {
        public ClientSongData() { }
        public ClientSongData(ClientSong song)
        {
            SiteId = song.SiteId;
            SongId = song.SongId;
        }
        public long SongListId { get; set; }
        public virtual SongListData SongList { get; set; }
    }

    public class Song : SongInfo
    {
        public string File;
        public string FileViaCdn;
        public double Length;
        public int Bitrate;
    }

    public class SongInfo : ClientSong
    {
        public string Artist;
        public string Album;
        public string Artwork;
        public string Title;
        public Lyric[] Lyrics;
    }

    public class Lyric
    {
        public bool withTimeline;
        public bool translate;
        public string lyric;
    }
}