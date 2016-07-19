namespace Wukong.Models
{
    public class ClientSong
    {
        public string SiteId { get; set; }
        public string SongId { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return SongId == ((ClientSong)obj).SongId && SiteId == ((ClientSong)obj).SiteId;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + SongId.GetHashCode();
            hash = hash * 23 + SiteId.GetHashCode();
            return hash;
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
        public double Length;
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