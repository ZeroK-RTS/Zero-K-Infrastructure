from datetime import datetime, timedelta

try:
	from sqlalchemy import create_engine, Table, Column, Integer, String, MetaData, ForeignKey, Boolean, Text, DateTime
	from sqlalchemy.orm import mapper, sessionmaker, relation
	from sqlalchemy.exc import IntegrityError
except ImportError, e:
	print("ERROR: sqlalchemy isn't installed: " + str(e))
	print("ERROR: please install sqlalchemy, on debian the command is sth. like: ")
	print("sudo apt-get install python-sqlalchemy")
	import sys
	sys.exit(1)

metadata = MetaData()
##########################################
users_table = Table('users', metadata,
	Column('id', Integer, primary_key=True),
	Column('username', String(40), unique=True),
	Column('password', String(64)),
	Column('register_date', DateTime),
	Column('last_login', DateTime),
	Column('last_ip', String(15)), # would need update for ipv6
	Column('last_id', String(128)),
	Column('ingame_time', Integer),
	Column('access', String(32)),
	Column('email', String(254)), # http://www.rfc-editor.org/errata_search.php?rfc=3696&eid=1690
	Column('bot', Integer),
	)
class User(object):
	def __init__(self, username, password, last_ip, access='agreement'):
		self.username = username
		self.password = password
		self.last_login = datetime.now()
		self.register_date = datetime.now()
		self.last_ip = last_ip
		self.ingame_time = 0
		self.bot = 0
		self.access = access # user, moderator, admin, bot, agreement
		self.last_id = 0
		self.email = ""

	def __repr__(self):
		return "<User('%s', '%s')>" % (self.username, self.password)
##########################################
class Login(object):
	def __init__(self, now, ip_address, lobby_id, user_id, cpu, local_ip, country):
		self.time = now
		self.ip_address = ip_address
		self.lobby_id = lobby_id
		self.user_id = user_id
		self.cpu = cpu
		self.local_ip = local_ip
		self.country = country
		#self.end = 0

	def __repr__(self):
		return "<Login('%s', '%s')>" % (self.ip_address, self.time)
##########################################
renames_table = Table('renames', metadata,
	Column('id', Integer, primary_key=True),
	Column('user_id', Integer, ForeignKey('users.id', onupdate='CASCADE', ondelete='CASCADE')),
	Column('original', String(40)),
	Column('new', String(40)), # FIXME: not needed
	Column('time', DateTime),
	)
class Rename(object):
	def __init__(self, original, new):
		self.original = original
		self.new = new
		self.time = datetime.now()
		
	def __repr__(self):
		return "<Rename('%s')>" % self.ip_address
mapper(Rename, renames_table)
##########################################
mapper(User, users_table, properties={
	#'logins':relation(Login, backref='user', cascade="all, delete, delete-orphan"),
	'renames':relation(Rename, backref='user', cascade="all, delete, delete-orphan"),
	})

##########################################
channels_table = Table('channels', metadata,
	Column('id', Integer, primary_key=True),
	Column('name', String(40), unique=True),
	Column('key', String(32)),
	Column('owner', String(40)), #FIXME: delete, use owner_userid
	Column('owner_userid', Integer, ForeignKey('users.id', onupdate='CASCADE', ondelete='SET NULL')), # user owner id
	Column('topic', Text),
	Column('topic_time', DateTime),
	Column('topic_owner', String(40)), #FIXME: delete, use topic_userid
	Column('topic_userid', Integer, ForeignKey('users.id', onupdate='CASCADE', ondelete='SET NULL')), # topic owner id
	Column('antispam', Boolean),
	Column('autokick', String(5)),
	Column('censor', Boolean),
	Column('antishock', Boolean),
	)
class Channel(object):
	def __init__(self, name, key='', chanserv=False, owner='', topic='', topic_time=0, topic_owner='', antispam=False, admins='', autokick='ban', censor=False, antishock=False):
		self.name = name
		self.key = key
		self.chanserv = chanserv
		self.owner = owner
		self.topic = topic
		self.topic_time = topic_time
		self.topic_owner = topic_owner
		self.antispam = antispam
		self.admins = admins
		self.autokick = autokick
		self.censor = censor
		self.antishock = antishock

	def __repr__(self):
		return "<Channel('%s')>" % self.name
mapper(Channel, channels_table)
##########################################
banip_table = Table('ban_ip', metadata, # server bans
	Column('id', Integer, primary_key=True),
	Column('issuer_id', Integer, ForeignKey('users.id', onupdate='CASCADE', ondelete='CASCADE')), # user which set ban
	Column('ip', String(60)), #ip which is banned
	Column('reason', Text),
	Column('end_time', DateTime),
	Column('updated', DateTime),
	)
class BanIP(object):
	def __init__(self, ip = None, issuer_id = None, reason = "", end_time = datetime.now()):
		self.issuer_id = issuer_id
		self.ip = ip
		self.reason = reason
		self.end_time = end_time
		self.updated = datetime.now()
mapper(BanIP, banip_table)
##########################################
banuser_table = Table('ban_user', metadata, # server bans
	Column('id', Integer, primary_key=True),
	Column('user_id', Integer, ForeignKey('users.id', onupdate='CASCADE', ondelete='CASCADE')), # user id which is banned
	Column('issuer_id', Integer, ForeignKey('users.id', onupdate='CASCADE', ondelete='CASCADE')), # user which set ban
	Column('reason', Text),
	Column('end_time', DateTime),
	Column('updated', DateTime),
	)
class BanUser(object):
	def __init__(self, user_id = None, issuer_id = None, reason = "", end_time = datetime.now()):
		self.user_id = user_id
		self.issuer_id = issuer_id
		self.reason = reason
		self.end_time = end_time
		self.updated = datetime.now()
mapper(BanUser, banuser_table)
##########################################

#metadata.create_all(engine)

class OfflineClient:
	def __init__(self, sqluser):
		self.id = sqluser.id
		self.username = sqluser.username
		self.password = sqluser.password
		self.ingame_time = sqluser.ingame_time
		self.bot = sqluser.bot
		self.last_login = sqluser.last_login
		self.register_date = sqluser.register_date
		self.last_id = sqluser.last_id
		self.access = sqluser.access
		self.email = sqluser.email

class UsersHandler:
	def __init__(self, root, engine):
		self._root = root
		metadata.create_all(engine)
		self.sessionmaker = sessionmaker(bind=engine, autoflush=True)
	
	def clientFromID(self, db_id):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.id==db_id).first()
		session.close()
		if not entry: return None
		return OfflineClient(entry)
	
	def clientFromUsername(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		session.close()
		if not entry: return None
		return OfflineClient(entry)
	
	def check_ban(self, user, ip, userid, now):
		session = self.sessionmaker()
		userban = session.query(BanUser).filter(BanUser.user_id==userid, now <= BanUser.end_time).first()
		if not userban:
			ipban = session.query(BanIP).filter(BanIP.ip==ip, now <= BanIP.end_time).first()
		session.close()
		if userban: return True, userban
		if ipban: return True, ipban
		return False, ""
		
	def login_user(self, username, password, ip, lobby_id, user_id, cpu, local_ip, country):

		if self._root.censor and not self._root.SayHooks._nasty_word_censor(username):
			return False, 'Name failed to pass profanity filter.'

		session = self.sessionmaker()
		good = True
		dbuser = session.query(User).filter(User.username==username, User.password == password).first() # should only ever be one user with each name so we can just grab the first one :)
		if not dbuser:
			session.close()
			return False, 'Invalid username or password'


		now = datetime.now()
		banned, dbban = self.check_ban(username, ip, dbuser.id, now)
		if banned:
			good = False
			timeleft = int((dbban.end_time - now).total_seconds())
			reason = 'You are banned: (%s) ' %(dbban.reason)
			if timeleft > 60 * 60 * 24 * 1000:
				reason += 'forever!'
			elif timeleft > 60 * 60 * 24:
				reason += 'days remaining: %s' % (timeleft / (60 * 60 * 24))
			else:
				reason += 'hours remaining: %s' % (timeleft / (60 * 60))
		#licho removing login tracking
        #dbuser.logins.append(Login(now, ip, lobby_id, user_id, cpu, local_ip, country))
		#dbuser.last_login = now
		#dbuser.time = now
		#dbuser.last_ip = ip
		#dbuser.last_id = user_id
		if good:
			reason = User(dbuser.username, password, ip, now)
			reason.access = dbuser.access
			reason.id = dbuser.id
			reason.ingame_time = dbuser.ingame_time
			reason.bot = dbuser.bot
			reason.last_login = dbuser.last_login
			reason.register_date = dbuser.register_date
			reason.lobby_id = lobby_id

		session.commit()
		session.close()
		return good, reason
	
	def end_session(self, db_id):
        #licho removing login tracking
		#session = self.sessionmaker()
		#entry = session.query(User).filter(User.id==db_id).first()
		#if entry and not entry.logins[-1].end:
		#	entry.logins[-1].end = datetime.now()
		#	session.commit()
		#session.close()
         return

	def register_user(self, user, password, ip, country): # need to add better ban checks so it can check if an ip address is banned when registering an account :)
		if len(user)>20: return False, 'Username too long'
		if self._root.censor:
			if not self._root.SayHooks._nasty_word_censor(user):
				return False, 'Name failed to pass profanity filter.'
		session = self.sessionmaker()
		results = session.query(User).filter(User.username==user).first()
		if results:
			session.close()
			return False, 'Username already exists.'
		entry = User(user, password, ip)
		session.add(entry)
		session.commit()
		session.close()
		return True, 'Account registered successfully.'
	
	def ban_user(self, owner, username, duration, reason):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		if not entry:
			session.close()
			return "Couldn't ban %s, user doesn't exist" % (username)
		end_time = datetime.now() + timedelta(duration)
		ban = BanUser(entry.id, owner.db_id, reason, end_time)
		session.add(ban)
		session.commit()
		session.close()
		return 'Successfully banned %s for %s days.' % (username, duration)
	
	def unban_user(self, username):
		session = self.sessionmaker()
		client = self.clientFromUsername(username)
		if not client:
			return "User %s doesn't exist" % username
		results = session.query(BanUser).filter(BanUser.user_id==client.id)
		if results:
			for result in results:
				session.delete(result)
			session.commit()
			session.close()
			return 'Successfully unbanned %s.' % username
		else:
			session.close()
			return 'No matching bans for %s.' % username

	def ban_ip(self, owner, ip, duration, reason):
		# TODO: add owner field to the database for bans
		session = self.sessionmaker()
		end_time = datetime.now() + timedelta(duration)
		ban = BanIP(ip, owner.db_id, reason, end_time)
		session.add(ban)
		session.commit()
		session.close()
		return 'Successfully banned %s for %s days.' % (ip, duration)

	def unban_ip(self, ip):
		session = self.sessionmaker()
		results = session.query(BanIP).filter(BanIP.ip==ip)
		if results:
			for result in results:
				session.delete(result)
			session.commit()
			session.close()
			return 'Successfully unbanned %s.' % ip
		else:
			session.close()
			return 'No matching bans for %s.' % ip
	
	def banlist(self):
		session = self.sessionmaker()
		banlist = []
		for ban in session.query(BanIP):
			banlist.append('ip: %s end: %s reason: %s' % (ban.ip, ban.end_time, ban.reason))
		for ban in session.query(BanUser):
			banlist.append('userid: %s end: %s reason: %s' % (ban.user_id, ban.end_time, ban.reason))
		session.close()
		return banlist

	def rename_user(self, user, newname):
		if len(newname)>20: return False, 'Username too long'
		session = self.sessionmaker()
		if self._root.censor:
			if not self._root.SayHooks._nasty_word_censor(user):
				return False, 'New username failed to pass profanity filter.'
		if not newname == user:
			results = session.query(User).filter(User.username==newname).first()
			if results:
				return False, 'Username already exists.'
		entry = session.query(User).filter(User.username==user).first()
		if not entry: return False, 'You don\'t seem to exist anymore. Contact an admin or moderator.'
		entry.renames.append(Rename(user, newname))
		entry.username = newname
		session.commit()
		session.close()
		# need to iterate through channels and rename junk there...
		# it might actually be a lot easier to use userids in the server... # later.
		return True, 'Account renamed successfully.'

	def save_user(self, client):
		session = self.sessionmaker()
		name = client.username
		entry = session.query(User).filter(User.username==name).first()
		if entry:
			entry.ingame_time = client.ingame_time
			entry.access = client.access
			entry.bot = client.bot
			entry.last_id = client.last_id
			entry.password = client.password
			entry.email = client.email
		session.commit()
		session.close()
	
	def confirm_agreement(self, client):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==client.username).first()
		if entry: entry.access = 'user'
		session.commit()
		session.close()
	
	def get_lastlogin(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		session.close()
		if entry: return True, entry.last_login
		else: return False, 'User not found.'
	
	def get_registration_date(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		session.close()
		if entry and entry.register_date: return True, entry.register_date
		else: return False, 'user or date not found in database'
	
	def get_ingame_time(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		session.close()
		if entry: return True, entry.ingame_time
		else: return False, 'user not found in database'
	
	def get_account_access(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		session.close()
		if entry:
			return True, entry.access
		else: return False, 'user not found in database'
	
	def find_ip(self, ip):
		session = self.sessionmaker()
		results = session.query(User).filter(User.last_ip==ip)
		session.close()
		return results
		
	def get_ip(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==username).first()
		session.close()
		if not entry:
			return None
		return entry.last_ip

	def remove_user(self, user):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.username==user).first()
		if not entry:
			return False, 'User not found.'
		session.delete(entry)
		session.commit()
		session.close()
		return True, 'Success.'

	def inject_user(self, user, password, ip, last_login, register_date, uid, ingame, country, bot, access, id):
		entry = User(user, password, ip)
		entry.last_login = last_login
		entry.last_id = uid
		entry.ingame_time = ingame
		entry.register_date = register_date
		entry.access = access
		entry.bot = bot
		entry.id = id
		return entry
	
	def inject_users(self, accounts):
		session = self.sessionmaker()
		for user in accounts:
			try:
				entry = self.inject_user(user['user'], user['pass'], user['last_ip'], user['last_login'], user['register_date'],
							user['uid'], user['ingame'], user['country'], user['bot'], user['access'], user['id'])
				session.add(entry)
				session.commit()
				print("Inserted: " + user['user'])
			except IntegrityError:
				session.rollback()
				#print("Duplicate Entry: " + user['user'])
		session.commit()
		session.close()

	def clean_users(self):
		''' delete old user accounts (very likely unused) '''
		session = self.sessionmaker()
		now = datetime.now()
		#delete users:
		# which didn't accept aggreement after one day
		session.query(User).filter(User.register_date < now - timedelta(hours=1)).filter(User.access == "agreement").delete()

		# which have no ingame time, last login > 90 days and no bot
		session.query(User).filter(User.ingame_time == 0).filter(User.last_login < now - timedelta(days=90)).filter(User.bot == 0).filter(User.access == "user").delete()

		# last login > 3 years
		session.query(User).filter(User.last_login < now - timedelta(days=1095)).delete()

		session.commit()
		session.close()


class ChannelsHandler:
	def __init__(self, root, engine):
		self._root = root
		metadata.create_all(engine)
		self.sessionmaker = sessionmaker(bind=engine, autoflush=True)
	
	def load_channels(self):
		session = self.sessionmaker()
		response = session.query(Channel)
		channels = {}
		for chan in response:
			channels[chan.name] = {'owner':chan.owner, 'key':chan.key, 'topic':chan.topic or '', 'antispam':chan.antispam, 'admins':[]}
		session.close()
		return channels

	def inject_channel(self, channel):
		session = self.sessionmaker()
		session.add(channel)
		session.commit()
		session.close()
	
	def save_channel(self, channel):

		if channel.topic:
			topic_text = channel.topic['text']
			topic_time = channel.topic['time']
			if 'owner' in channel.topic:
				topic_owner = channel.topic['owner']
			else:
				topic_owner = ''
		else:
			topic_text, topic_time, topic_owner = ('', 0, '')
		
		session = self.sessionmaker()
		entry = session.query(Channel)
		entry.name = channel.chan
		entry.key = channel.key
		entry.chanserv = channel.chanserv
		entry.owner = channel.owner
		entry.topic = topic_text
		entry.topic_time = topic_time
		entry.topic_owner = topic_owner
		entry.antispam = channel.antispam
		entry.autokick = channel.autokick
		entry.censor = channel.censor
		entry.antishock = channel.antishock

		session.add(entry)
		session.commit()
		session.close()

	def save_channels(self, channels):
		for channel in channels:
			self.save_channel(channel)

	def setTopic(self, user, chan, topic):
		session = self.sessionmaker()
		entry = session.query(Channel).filter(Channel.name == chan.name).first()
		if entry:
			entry.topic = topic
			entry.topic_time = datetime.now()
			entry.topic_owner = user
			session.commit()
		session.close()

	def setKey(self, chan, key):
		session = self.sessionmaker()
		entry = session.query(Channel).filter(Channel.name == chan.name).first()
		if entry:
			entry.key = key
			session.commit()
		session.close()

	def register(self, channel, client, target):
		session = self.sessionmaker()
		entry = session.query(Channel).filter(Channel.name == channel.name)
		if entry and not entry.first():
			entry = Channel(channel.name)
			session.add(entry)
			if channel.topic:
				entry.topic = channel.topic['text']
				entry.topic_time =  datetime.fromtimestamp(channel.topic['time'])
				entry.topic_owner = channel.topic['user']
			else:
				entry.topic_time = datetime.now()
		entry.owner = target.username
		session.commit()
		session.close()

	def unRegister(self, client, channel):
		session = self.sessionmaker()
		entry = session.query(Channel).filter(Channel.name == channel.name).first()
		if entry:
			session.delete(entry)
			session.commit()
		session.close()
	

