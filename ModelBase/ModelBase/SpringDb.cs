#region Auto-generated classes for spring database on 2009-06-01 04:36:21Z

//
//  ____  _     __  __      _        _
// |  _ \| |__ |  \/  | ___| |_ __ _| |
// | | | | '_ \| |\/| |/ _ \ __/ _` | |
// | |_| | |_) | |  | |  __/ || (_| | |
// |____/|_.__/|_|  |_|\___|\__\__,_|_|
//
// Auto-generated from spring on 2009-06-01 04:36:21Z
// Please visit http://linq.to/db for more information

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using DbLinq.Data.Linq;

namespace ModelBase
{
	public partial class Spring : DbLinq.Data.Linq.DataContext
	{
		public Spring(System.Data.IDbConnection connection)
		: base(connection, new DbLinq.MySql.MySqlVendor())
		{
		}

		public Spring(System.Data.IDbConnection connection, DbLinq.Vendor.IVendor vendor)
		: base(connection, vendor)
		{
		}

	
		public Table<PhPbB3Posts> PhPbB3Posts { get { return GetTable<PhPbB3Posts>(); } }
		public Table<PhPbB3Topics> PhPbB3Topics { get { return GetTable<PhPbB3Topics>(); } }
		public Table<PhPbB3TopicsPosted> PhPbB3TopicsPosted { get { return GetTable<PhPbB3TopicsPosted>(); } }
		public Table<PhPbB3TopicsTrack> PhPbB3TopicsTrack { get { return GetTable<PhPbB3TopicsTrack>(); } }
	
	}




	
	
	[Table(Name = "spring.phpbb3_posts")]
	public partial class PhPbB3Posts : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged handling

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		#region string BbcOdeBitField

		private string _bbcOdeBitField;
		[DebuggerNonUserCode]
		[Column(Storage = "_bbcOdeBitField", Name = "bbcode_bitfield", DbType = "varchar(255)", CanBeNull = false)]
		public string BbcOdeBitField
		{
			get
			{
				return _bbcOdeBitField;
			}
			set
			{
				if (value != _bbcOdeBitField)
				{
					_bbcOdeBitField = value;
					OnPropertyChanged("BbcOdeBitField");
				}
			}
		}

		#endregion

		#region string BbcOdeUID

		private string _bbcOdeUid;
		[DebuggerNonUserCode]
		[Column(Storage = "_bbcOdeUid", Name = "bbcode_uid", DbType = "varchar(8)", CanBeNull = false)]
		public string BbcOdeUID
		{
			get
			{
				return _bbcOdeUid;
			}
			set
			{
				if (value != _bbcOdeUid)
				{
					_bbcOdeUid = value;
					OnPropertyChanged("BbcOdeUID");
				}
			}
		}

		#endregion

		#region byte EnableBbcOde

		private byte _enableBbcOde;
		[DebuggerNonUserCode]
		[Column(Storage = "_enableBbcOde", Name = "enable_bbcode", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte EnableBbcOde
		{
			get
			{
				return _enableBbcOde;
			}
			set
			{
				if (value != _enableBbcOde)
				{
					_enableBbcOde = value;
					OnPropertyChanged("EnableBbcOde");
				}
			}
		}

		#endregion

		#region byte EnableMagicURL

		private byte _enableMagicUrl;
		[DebuggerNonUserCode]
		[Column(Storage = "_enableMagicUrl", Name = "enable_magic_url", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte EnableMagicURL
		{
			get
			{
				return _enableMagicUrl;
			}
			set
			{
				if (value != _enableMagicUrl)
				{
					_enableMagicUrl = value;
					OnPropertyChanged("EnableMagicURL");
				}
			}
		}

		#endregion

		#region byte EnableSiG

		private byte _enableSiG;
		[DebuggerNonUserCode]
		[Column(Storage = "_enableSiG", Name = "enable_sig", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte EnableSiG
		{
			get
			{
				return _enableSiG;
			}
			set
			{
				if (value != _enableSiG)
				{
					_enableSiG = value;
					OnPropertyChanged("EnableSiG");
				}
			}
		}

		#endregion

		#region byte EnableSmILies

		private byte _enableSmIlIes;
		[DebuggerNonUserCode]
		[Column(Storage = "_enableSmIlIes", Name = "enable_smilies", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte EnableSmILies
		{
			get
			{
				return _enableSmIlIes;
			}
			set
			{
				if (value != _enableSmIlIes)
				{
					_enableSmIlIes = value;
					OnPropertyChanged("EnableSmILies");
				}
			}
		}

		#endregion

		#region uint ForumID

		private uint _forumID;
		[DebuggerNonUserCode]
		[Column(Storage = "_forumID", Name = "forum_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint ForumID
		{
			get
			{
				return _forumID;
			}
			set
			{
				if (value != _forumID)
				{
					_forumID = value;
					OnPropertyChanged("ForumID");
				}
			}
		}

		#endregion

		#region uint IconID

		private uint _iconID;
		[DebuggerNonUserCode]
		[Column(Storage = "_iconID", Name = "icon_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint IconID
		{
			get
			{
				return _iconID;
			}
			set
			{
				if (value != _iconID)
				{
					_iconID = value;
					OnPropertyChanged("IconID");
				}
			}
		}

		#endregion

		#region byte PostApproved

		private byte _postApproved;
		[DebuggerNonUserCode]
		[Column(Storage = "_postApproved", Name = "post_approved", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte PostApproved
		{
			get
			{
				return _postApproved;
			}
			set
			{
				if (value != _postApproved)
				{
					_postApproved = value;
					OnPropertyChanged("PostApproved");
				}
			}
		}

		#endregion

		#region byte PostAttachment

		private byte _postAttachment;
		[DebuggerNonUserCode]
		[Column(Storage = "_postAttachment", Name = "post_attachment", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte PostAttachment
		{
			get
			{
				return _postAttachment;
			}
			set
			{
				if (value != _postAttachment)
				{
					_postAttachment = value;
					OnPropertyChanged("PostAttachment");
				}
			}
		}

		#endregion

		#region ushort PostEditCount

		private ushort _postEditCount;
		[DebuggerNonUserCode]
		[Column(Storage = "_postEditCount", Name = "post_edit_count", DbType = "smallint(4) unsigned", CanBeNull = false)]
		public ushort PostEditCount
		{
			get
			{
				return _postEditCount;
			}
			set
			{
				if (value != _postEditCount)
				{
					_postEditCount = value;
					OnPropertyChanged("PostEditCount");
				}
			}
		}

		#endregion

		#region byte PostEditLocked

		private byte _postEditLocked;
		[DebuggerNonUserCode]
		[Column(Storage = "_postEditLocked", Name = "post_edit_locked", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte PostEditLocked
		{
			get
			{
				return _postEditLocked;
			}
			set
			{
				if (value != _postEditLocked)
				{
					_postEditLocked = value;
					OnPropertyChanged("PostEditLocked");
				}
			}
		}

		#endregion

		#region string PostEditReason

		private string _postEditReason;
		[DebuggerNonUserCode]
		[Column(Storage = "_postEditReason", Name = "post_edit_reason", DbType = "varchar(255)", CanBeNull = false)]
		public string PostEditReason
		{
			get
			{
				return _postEditReason;
			}
			set
			{
				if (value != _postEditReason)
				{
					_postEditReason = value;
					OnPropertyChanged("PostEditReason");
				}
			}
		}

		#endregion

		#region uint PostEditTime

		private uint _postEditTime;
		[DebuggerNonUserCode]
		[Column(Storage = "_postEditTime", Name = "post_edit_time", DbType = "int unsigned", CanBeNull = false)]
		public uint PostEditTime
		{
			get
			{
				return _postEditTime;
			}
			set
			{
				if (value != _postEditTime)
				{
					_postEditTime = value;
					OnPropertyChanged("PostEditTime");
				}
			}
		}

		#endregion

		#region uint PostEditUser

		private uint _postEditUser;
		[DebuggerNonUserCode]
		[Column(Storage = "_postEditUser", Name = "post_edit_user", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint PostEditUser
		{
			get
			{
				return _postEditUser;
			}
			set
			{
				if (value != _postEditUser)
				{
					_postEditUser = value;
					OnPropertyChanged("PostEditUser");
				}
			}
		}

		#endregion

		#region uint PosterID

		private uint _posterID;
		[DebuggerNonUserCode]
		[Column(Storage = "_posterID", Name = "poster_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint PosterID
		{
			get
			{
				return _posterID;
			}
			set
			{
				if (value != _posterID)
				{
					_posterID = value;
					OnPropertyChanged("PosterID");
				}
			}
		}

		#endregion

		#region string PosterIP

		private string _posterIp;
		[DebuggerNonUserCode]
		[Column(Storage = "_posterIp", Name = "poster_ip", DbType = "varchar(40)", CanBeNull = false)]
		public string PosterIP
		{
			get
			{
				return _posterIp;
			}
			set
			{
				if (value != _posterIp)
				{
					_posterIp = value;
					OnPropertyChanged("PosterIP");
				}
			}
		}

		#endregion

		#region string PostChecksum

		private string _postChecksum;
		[DebuggerNonUserCode]
		[Column(Storage = "_postChecksum", Name = "post_checksum", DbType = "varchar(32)", CanBeNull = false)]
		public string PostChecksum
		{
			get
			{
				return _postChecksum;
			}
			set
			{
				if (value != _postChecksum)
				{
					_postChecksum = value;
					OnPropertyChanged("PostChecksum");
				}
			}
		}

		#endregion

		#region uint PostID

		private uint _postID;
		[DebuggerNonUserCode]
		[Column(Storage = "_postID", Name = "post_id", DbType = "mediumint unsigned", IsPrimaryKey = true, IsDbGenerated = true, CanBeNull = false)]
		public uint PostID
		{
			get
			{
				return _postID;
			}
			set
			{
				if (value != _postID)
				{
					_postID = value;
					OnPropertyChanged("PostID");
				}
			}
		}

		#endregion

		#region byte PostPostCount

		private byte _postPostCount;
		[DebuggerNonUserCode]
		[Column(Storage = "_postPostCount", Name = "post_postcount", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte PostPostCount
		{
			get
			{
				return _postPostCount;
			}
			set
			{
				if (value != _postPostCount)
				{
					_postPostCount = value;
					OnPropertyChanged("PostPostCount");
				}
			}
		}

		#endregion

		#region byte PostReported

		private byte _postReported;
		[DebuggerNonUserCode]
		[Column(Storage = "_postReported", Name = "post_reported", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte PostReported
		{
			get
			{
				return _postReported;
			}
			set
			{
				if (value != _postReported)
				{
					_postReported = value;
					OnPropertyChanged("PostReported");
				}
			}
		}

		#endregion

		#region string PostSubject

		private string _postSubject;
		[DebuggerNonUserCode]
		[Column(Storage = "_postSubject", Name = "post_subject", DbType = "varchar(255)", CanBeNull = false)]
		public string PostSubject
		{
			get
			{
				return _postSubject;
			}
			set
			{
				if (value != _postSubject)
				{
					_postSubject = value;
					OnPropertyChanged("PostSubject");
				}
			}
		}

		#endregion

		#region  PostText

		private string _postText;
		[DebuggerNonUserCode]
		[Column(Storage = "_postText", Name = "post_text", DbType = "mediumtext", CanBeNull = false)]
		public string PostText
		{
			get
			{
				return _postText;
			}
			set
			{
				if (value != _postText)
				{
					_postText = value;
					OnPropertyChanged("PostText");
				}
			}
		}

		#endregion

		#region uint PostTime

		private uint _postTime;
		[DebuggerNonUserCode]
		[Column(Storage = "_postTime", Name = "post_time", DbType = "int unsigned", CanBeNull = false)]
		public uint PostTime
		{
			get
			{
				return _postTime;
			}
			set
			{
				if (value != _postTime)
				{
					_postTime = value;
					OnPropertyChanged("PostTime");
				}
			}
		}

		#endregion

		#region string PostUserName

		private string _postUserName;
		[DebuggerNonUserCode]
		[Column(Storage = "_postUserName", Name = "post_username", DbType = "varchar(255)", CanBeNull = false)]
		public string PostUserName
		{
			get
			{
				return _postUserName;
			}
			set
			{
				if (value != _postUserName)
				{
					_postUserName = value;
					OnPropertyChanged("PostUserName");
				}
			}
		}

		#endregion

		#region uint TopicID

		private uint _topicID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicID", Name = "topic_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicID
		{
			get
			{
				return _topicID;
			}
			set
			{
				if (value != _topicID)
				{
					_topicID = value;
					OnPropertyChanged("TopicID");
				}
			}
		}

		#endregion

	}




	[Table(Name = "spring.phpbb3_topics")]
	public partial class PhPbB3Topics : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged handling

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		#region uint ForumID

		private uint _forumID;
		[DebuggerNonUserCode]
		[Column(Storage = "_forumID", Name = "forum_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint ForumID
		{
			get
			{
				return _forumID;
			}
			set
			{
				if (value != _forumID)
				{
					_forumID = value;
					OnPropertyChanged("ForumID");
				}
			}
		}

		#endregion

		#region uint IconID

		private uint _iconID;
		[DebuggerNonUserCode]
		[Column(Storage = "_iconID", Name = "icon_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint IconID
		{
			get
			{
				return _iconID;
			}
			set
			{
				if (value != _iconID)
				{
					_iconID = value;
					OnPropertyChanged("IconID");
				}
			}
		}

		#endregion

		#region uint PollLastVote

		private uint _pollLastVote;
		[DebuggerNonUserCode]
		[Column(Storage = "_pollLastVote", Name = "poll_last_vote", DbType = "int unsigned", CanBeNull = false)]
		public uint PollLastVote
		{
			get
			{
				return _pollLastVote;
			}
			set
			{
				if (value != _pollLastVote)
				{
					_pollLastVote = value;
					OnPropertyChanged("PollLastVote");
				}
			}
		}

		#endregion

		#region uint PollLength

		private uint _pollLength;
		[DebuggerNonUserCode]
		[Column(Storage = "_pollLength", Name = "poll_length", DbType = "int unsigned", CanBeNull = false)]
		public uint PollLength
		{
			get
			{
				return _pollLength;
			}
			set
			{
				if (value != _pollLength)
				{
					_pollLength = value;
					OnPropertyChanged("PollLength");
				}
			}
		}

		#endregion

		#region byte PollMaXOptions

		private byte _pollMaXoPtions;
		[DebuggerNonUserCode]
		[Column(Storage = "_pollMaXoPtions", Name = "poll_max_options", DbType = "tinyint(4)", CanBeNull = false)]
		public byte PollMaXOptions
		{
			get
			{
				return _pollMaXoPtions;
			}
			set
			{
				if (value != _pollMaXoPtions)
				{
					_pollMaXoPtions = value;
					OnPropertyChanged("PollMaXOptions");
				}
			}
		}

		#endregion

		#region uint PollStart

		private uint _pollStart;
		[DebuggerNonUserCode]
		[Column(Storage = "_pollStart", Name = "poll_start", DbType = "int unsigned", CanBeNull = false)]
		public uint PollStart
		{
			get
			{
				return _pollStart;
			}
			set
			{
				if (value != _pollStart)
				{
					_pollStart = value;
					OnPropertyChanged("PollStart");
				}
			}
		}

		#endregion

		#region string PollTitle

		private string _pollTitle;
		[DebuggerNonUserCode]
		[Column(Storage = "_pollTitle", Name = "poll_title", DbType = "varchar(255)", CanBeNull = false)]
		public string PollTitle
		{
			get
			{
				return _pollTitle;
			}
			set
			{
				if (value != _pollTitle)
				{
					_pollTitle = value;
					OnPropertyChanged("PollTitle");
				}
			}
		}

		#endregion

		#region byte PollVoteChange

		private byte _pollVoteChange;
		[DebuggerNonUserCode]
		[Column(Storage = "_pollVoteChange", Name = "poll_vote_change", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte PollVoteChange
		{
			get
			{
				return _pollVoteChange;
			}
			set
			{
				if (value != _pollVoteChange)
				{
					_pollVoteChange = value;
					OnPropertyChanged("PollVoteChange");
				}
			}
		}

		#endregion

		#region byte TopicApproved

		private byte _topicApproved;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicApproved", Name = "topic_approved", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte TopicApproved
		{
			get
			{
				return _topicApproved;
			}
			set
			{
				if (value != _topicApproved)
				{
					_topicApproved = value;
					OnPropertyChanged("TopicApproved");
				}
			}
		}

		#endregion

		#region byte TopicAttachment

		private byte _topicAttachment;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicAttachment", Name = "topic_attachment", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte TopicAttachment
		{
			get
			{
				return _topicAttachment;
			}
			set
			{
				if (value != _topicAttachment)
				{
					_topicAttachment = value;
					OnPropertyChanged("TopicAttachment");
				}
			}
		}

		#endregion

		#region byte TopicBumped

		private byte _topicBumped;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicBumped", Name = "topic_bumped", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte TopicBumped
		{
			get
			{
				return _topicBumped;
			}
			set
			{
				if (value != _topicBumped)
				{
					_topicBumped = value;
					OnPropertyChanged("TopicBumped");
				}
			}
		}

		#endregion

		#region uint TopicBumper

		private uint _topicBumper;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicBumper", Name = "topic_bumper", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicBumper
		{
			get
			{
				return _topicBumper;
			}
			set
			{
				if (value != _topicBumper)
				{
					_topicBumper = value;
					OnPropertyChanged("TopicBumper");
				}
			}
		}

		#endregion

		#region string TopicFirstPosterColour

		private string _topicFirstPosterColour;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicFirstPosterColour", Name = "topic_first_poster_colour", DbType = "varchar(6)", CanBeNull = false)]
		public string TopicFirstPosterColour
		{
			get
			{
				return _topicFirstPosterColour;
			}
			set
			{
				if (value != _topicFirstPosterColour)
				{
					_topicFirstPosterColour = value;
					OnPropertyChanged("TopicFirstPosterColour");
				}
			}
		}

		#endregion

		#region string TopicFirstPosterName

		private string _topicFirstPosterName;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicFirstPosterName", Name = "topic_first_poster_name", DbType = "varchar(255)", CanBeNull = false)]
		public string TopicFirstPosterName
		{
			get
			{
				return _topicFirstPosterName;
			}
			set
			{
				if (value != _topicFirstPosterName)
				{
					_topicFirstPosterName = value;
					OnPropertyChanged("TopicFirstPosterName");
				}
			}
		}

		#endregion

		#region uint TopicFirstPostID

		private uint _topicFirstPostID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicFirstPostID", Name = "topic_first_post_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicFirstPostID
		{
			get
			{
				return _topicFirstPostID;
			}
			set
			{
				if (value != _topicFirstPostID)
				{
					_topicFirstPostID = value;
					OnPropertyChanged("TopicFirstPostID");
				}
			}
		}

		#endregion

		#region uint TopicID

		private uint _topicID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicID", Name = "topic_id", DbType = "mediumint unsigned", IsPrimaryKey = true, IsDbGenerated = true, CanBeNull = false)]
		public uint TopicID
		{
			get
			{
				return _topicID;
			}
			set
			{
				if (value != _topicID)
				{
					_topicID = value;
					OnPropertyChanged("TopicID");
				}
			}
		}

		#endregion

		#region string TopicLastPosterColour

		private string _topicLastPosterColour;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastPosterColour", Name = "topic_last_poster_colour", DbType = "varchar(6)", CanBeNull = false)]
		public string TopicLastPosterColour
		{
			get
			{
				return _topicLastPosterColour;
			}
			set
			{
				if (value != _topicLastPosterColour)
				{
					_topicLastPosterColour = value;
					OnPropertyChanged("TopicLastPosterColour");
				}
			}
		}

		#endregion

		#region uint TopicLastPosterID

		private uint _topicLastPosterID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastPosterID", Name = "topic_last_poster_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicLastPosterID
		{
			get
			{
				return _topicLastPosterID;
			}
			set
			{
				if (value != _topicLastPosterID)
				{
					_topicLastPosterID = value;
					OnPropertyChanged("TopicLastPosterID");
				}
			}
		}

		#endregion

		#region string TopicLastPosterName

		private string _topicLastPosterName;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastPosterName", Name = "topic_last_poster_name", DbType = "varchar(255)", CanBeNull = false)]
		public string TopicLastPosterName
		{
			get
			{
				return _topicLastPosterName;
			}
			set
			{
				if (value != _topicLastPosterName)
				{
					_topicLastPosterName = value;
					OnPropertyChanged("TopicLastPosterName");
				}
			}
		}

		#endregion

		#region uint TopicLastPostID

		private uint _topicLastPostID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastPostID", Name = "topic_last_post_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicLastPostID
		{
			get
			{
				return _topicLastPostID;
			}
			set
			{
				if (value != _topicLastPostID)
				{
					_topicLastPostID = value;
					OnPropertyChanged("TopicLastPostID");
				}
			}
		}

		#endregion

		#region string TopicLastPostSubject

		private string _topicLastPostSubject;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastPostSubject", Name = "topic_last_post_subject", DbType = "varchar(255)", CanBeNull = false)]
		public string TopicLastPostSubject
		{
			get
			{
				return _topicLastPostSubject;
			}
			set
			{
				if (value != _topicLastPostSubject)
				{
					_topicLastPostSubject = value;
					OnPropertyChanged("TopicLastPostSubject");
				}
			}
		}

		#endregion

		#region uint TopicLastPostTime

		private uint _topicLastPostTime;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastPostTime", Name = "topic_last_post_time", DbType = "int unsigned", CanBeNull = false)]
		public uint TopicLastPostTime
		{
			get
			{
				return _topicLastPostTime;
			}
			set
			{
				if (value != _topicLastPostTime)
				{
					_topicLastPostTime = value;
					OnPropertyChanged("TopicLastPostTime");
				}
			}
		}

		#endregion

		#region uint TopicLastViewTime

		private uint _topicLastViewTime;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicLastViewTime", Name = "topic_last_view_time", DbType = "int unsigned", CanBeNull = false)]
		public uint TopicLastViewTime
		{
			get
			{
				return _topicLastViewTime;
			}
			set
			{
				if (value != _topicLastViewTime)
				{
					_topicLastViewTime = value;
					OnPropertyChanged("TopicLastViewTime");
				}
			}
		}

		#endregion

		#region uint TopicMovedID

		private uint _topicMovedID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicMovedID", Name = "topic_moved_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicMovedID
		{
			get
			{
				return _topicMovedID;
			}
			set
			{
				if (value != _topicMovedID)
				{
					_topicMovedID = value;
					OnPropertyChanged("TopicMovedID");
				}
			}
		}

		#endregion

		#region uint TopicPoster

		private uint _topicPoster;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicPoster", Name = "topic_poster", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicPoster
		{
			get
			{
				return _topicPoster;
			}
			set
			{
				if (value != _topicPoster)
				{
					_topicPoster = value;
					OnPropertyChanged("TopicPoster");
				}
			}
		}

		#endregion

		#region uint TopicReplies

		private uint _topicReplies;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicReplies", Name = "topic_replies", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicReplies
		{
			get
			{
				return _topicReplies;
			}
			set
			{
				if (value != _topicReplies)
				{
					_topicReplies = value;
					OnPropertyChanged("TopicReplies");
				}
			}
		}

		#endregion

		#region uint TopicRepliesReal

		private uint _topicRepliesReal;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicRepliesReal", Name = "topic_replies_real", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicRepliesReal
		{
			get
			{
				return _topicRepliesReal;
			}
			set
			{
				if (value != _topicRepliesReal)
				{
					_topicRepliesReal = value;
					OnPropertyChanged("TopicRepliesReal");
				}
			}
		}

		#endregion

		#region byte TopicReported

		private byte _topicReported;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicReported", Name = "topic_reported", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte TopicReported
		{
			get
			{
				return _topicReported;
			}
			set
			{
				if (value != _topicReported)
				{
					_topicReported = value;
					OnPropertyChanged("TopicReported");
				}
			}
		}

		#endregion

		#region byte TopicStatus

		private byte _topicStatus;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicStatus", Name = "topic_status", DbType = "tinyint(3)", CanBeNull = false)]
		public byte TopicStatus
		{
			get
			{
				return _topicStatus;
			}
			set
			{
				if (value != _topicStatus)
				{
					_topicStatus = value;
					OnPropertyChanged("TopicStatus");
				}
			}
		}

		#endregion

		#region uint TopicTime

		private uint _topicTime;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicTime", Name = "topic_time", DbType = "int unsigned", CanBeNull = false)]
		public uint TopicTime
		{
			get
			{
				return _topicTime;
			}
			set
			{
				if (value != _topicTime)
				{
					_topicTime = value;
					OnPropertyChanged("TopicTime");
				}
			}
		}

		#endregion

		#region uint TopicTimeLimit

		private uint _topicTimeLimit;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicTimeLimit", Name = "topic_time_limit", DbType = "int unsigned", CanBeNull = false)]
		public uint TopicTimeLimit
		{
			get
			{
				return _topicTimeLimit;
			}
			set
			{
				if (value != _topicTimeLimit)
				{
					_topicTimeLimit = value;
					OnPropertyChanged("TopicTimeLimit");
				}
			}
		}

		#endregion

		#region string TopicTitle

		private string _topicTitle;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicTitle", Name = "topic_title", DbType = "varchar(255)", CanBeNull = false)]
		public string TopicTitle
		{
			get
			{
				return _topicTitle;
			}
			set
			{
				if (value != _topicTitle)
				{
					_topicTitle = value;
					OnPropertyChanged("TopicTitle");
				}
			}
		}

		#endregion

		#region byte TopicType

		private byte _topicType;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicType", Name = "topic_type", DbType = "tinyint(3)", CanBeNull = false)]
		public byte TopicType
		{
			get
			{
				return _topicType;
			}
			set
			{
				if (value != _topicType)
				{
					_topicType = value;
					OnPropertyChanged("TopicType");
				}
			}
		}

		#endregion

		#region uint TopicViews

		private uint _topicViews;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicViews", Name = "topic_views", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint TopicViews
		{
			get
			{
				return _topicViews;
			}
			set
			{
				if (value != _topicViews)
				{
					_topicViews = value;
					OnPropertyChanged("TopicViews");
				}
			}
		}

		#endregion

	}

	[Table(Name = "spring.phpbb3_topics_posted")]
	public partial class PhPbB3TopicsPosted : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged handling

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		#region uint TopicID

		private uint _topicID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicID", Name = "topic_id", DbType = "mediumint unsigned", IsPrimaryKey = true, CanBeNull = false)]
		public uint TopicID
		{
			get
			{
				return _topicID;
			}
			set
			{
				if (value != _topicID)
				{
					_topicID = value;
					OnPropertyChanged("TopicID");
				}
			}
		}

		#endregion

		#region byte TopicPosted

		private byte _topicPosted;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicPosted", Name = "topic_posted", DbType = "tinyint(1) unsigned", CanBeNull = false)]
		public byte TopicPosted
		{
			get
			{
				return _topicPosted;
			}
			set
			{
				if (value != _topicPosted)
				{
					_topicPosted = value;
					OnPropertyChanged("TopicPosted");
				}
			}
		}

		#endregion

		#region uint UserID

		private uint _userID;
		[DebuggerNonUserCode]
		[Column(Storage = "_userID", Name = "user_id", DbType = "mediumint unsigned", IsPrimaryKey = true, CanBeNull = false)]
		public uint UserID
		{
			get
			{
				return _userID;
			}
			set
			{
				if (value != _userID)
				{
					_userID = value;
					OnPropertyChanged("UserID");
				}
			}
		}

		#endregion

	}

	[Table(Name = "spring.phpbb3_topics_track")]
	public partial class PhPbB3TopicsTrack : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged handling

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		#region uint ForumID

		private uint _forumID;
		[DebuggerNonUserCode]
		[Column(Storage = "_forumID", Name = "forum_id", DbType = "mediumint unsigned", CanBeNull = false)]
		public uint ForumID
		{
			get
			{
				return _forumID;
			}
			set
			{
				if (value != _forumID)
				{
					_forumID = value;
					OnPropertyChanged("ForumID");
				}
			}
		}

		#endregion

		#region uint MarkTime

		private uint _markTime;
		[DebuggerNonUserCode]
		[Column(Storage = "_markTime", Name = "mark_time", DbType = "int unsigned", CanBeNull = false)]
		public uint MarkTime
		{
			get
			{
				return _markTime;
			}
			set
			{
				if (value != _markTime)
				{
					_markTime = value;
					OnPropertyChanged("MarkTime");
				}
			}
		}

		#endregion

		#region uint TopicID

		private uint _topicID;
		[DebuggerNonUserCode]
		[Column(Storage = "_topicID", Name = "topic_id", DbType = "mediumint unsigned", IsPrimaryKey = true, CanBeNull = false)]
		public uint TopicID
		{
			get
			{
				return _topicID;
			}
			set
			{
				if (value != _topicID)
				{
					_topicID = value;
					OnPropertyChanged("TopicID");
				}
			}
		}

		#endregion

		#region uint UserID

		private uint _userID;
		[DebuggerNonUserCode]
		[Column(Storage = "_userID", Name = "user_id", DbType = "mediumint unsigned", IsPrimaryKey = true, CanBeNull = false)]
		public uint UserID
		{
			get
			{
				return _userID;
			}
			set
			{
				if (value != _userID)
				{
					_userID = value;
					OnPropertyChanged("UserID");
				}
			}
		}

		#endregion

	}


}
