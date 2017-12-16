using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData {
    public class LobbyNews
    {
        public int LobbyNewsID { get; set; }

        public DateTime? EventTime { get; set; }
        public DateTime Created { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }


        [StringLength(50)]
        public string ImageExtension { get; set; }


        public int AuthorAccountID { get; set; }
        [ForeignKey(nameof(AuthorAccountID))]
        public virtual Account Author { get; set; }

        [NotMapped]
        public string ImageRelativeUrl
        {
            get { if (ImageExtension == null) return null; return string.Format("/img/lobbynews/{0}{1}", LobbyNewsID, ImageExtension); }
        }


    }
}