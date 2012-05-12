import time

from sqlalchemy import create_engine, Table, Column, Integer, String, MetaData, ForeignKey, Boolean, Text
from sqlalchemy.orm import mapper, sessionmaker, relation

metadata = MetaData()

class User(object):
	def __init__(self, name, username, password, last_ip, access='agreement'):
		self.lowername = name
		self.username = username
		self.password = password
		self.last_login = int(time.time()*1000)
		self.register_date = int(time.time()*1000)
		self.last_ip = last_ip
		self.ingame_time = 0
		self.bot = 0
		self.access = access # user, moderator, admin, bot, agreement
		self.hook_chars = ''
		self.mapgrades = ''

	def __repr__(self):
		return "<User('%s', '%s')>" % (self.lowername, self.password)

class Login(object):
	def __init__(self, now, ip_address, lobby_id, user_id, cpu, local_ip, country):
		self.time = now
		self.ip_address = ip_address
		self.lobby_id = lobby_id
		self.user_id = user_id
		self.cpu = cpu
		self.local_ip = local_ip
		self.country = country
		self.end = 0

	def __repr__(self):
		return "<Login('%s', '%s')>" % (self.ip_address, self.time)

class Rename(object):
	def __init__(self, original, new):
		self.original = original
		self.new = new
		self.time = int(time.time()*1000)
		
	def __repr__(self):
		return "<Rename('%s')>" % self.ip_address

class Channel(object):
	def __init__(self, name, key='', chanserv=False, owner='', topic='', topic_time=0, topic_owner='', antispam=False, admins='', autokick='ban', censor=False, antishock=False):
		self.lowername = name
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
		return "<Channel('%s')>" % self.lowername

class ChanUser(object):
	def __init__(self, name, channel, admin=False, banned='', allowed=False, mute=0):
		self.lowername = name
		self.channel = channel
		self.admin = admin
		self.banned = banned
		self.allowed = allowed
		self.mute = mute

	def __repr__(self):
		return "<ChanUser('%s')>" % self.lowername

class Ban(object):
	def __init__(self, reason, end_time):
		self.reason = reason
		self.end_time = end_time
	
	def __repr__(self):
		return "<Ban('%s')>" % self.end_time

class AggregateBan(object):
	def __init__(self, type, data):
		self.type = type
		self.data = data
	
	def __repr__(self):
		return "<AggregateBan('%s')('%s')>" % (self.type, self.data)

users_table = Table('users', metadata,
	Column('id', Integer, primary_key=True),
	Column('lowername', String(40)),
	Column('username', String(40)),
	Column('password', String(32)),
	Column('register_date', Integer),
	Column('last_login', Integer), # use milliseconds since unix epoch # should replace these with last_session or just remove them
	Column('last_ip', String(15)), # would need update for ipv6   # 
	Column('last_id', String(128)),
	Column('ingame_time', Integer),
	Column('access', String(32)),
	Column('bot', Integer),
	Column('hook_chars', String(4)),
	Column('mapgrades', Text),
	)

logins_table = Table('logins', metadata, 
	Column('id', Integer, primary_key=True),
	Column('ip_address', String(15), nullable=False),
	Column('time', Integer),
	Column('lobby_id', String(128)),
	Column('cpu', Integer),
	Column('local_ip', String(15)),
	Column('country', String(4)),
	Column('end', Integer),
	Column('user_id', String(128)),
	Column('user_dbid', Integer, ForeignKey('users.id')),
	)

renames_table = Table('renames', metadata,
	Column('id', Integer, primary_key=True),
	Column('user_id', Integer, ForeignKey('users.id')),
	Column('original', String(40)),
	Column('new', String(40)),
	Column('time', Integer),
	)

channels_table = Table('channels', metadata,
	Column('id', Integer, primary_key=True),
	Column('name', String(40)),
	Column('key', String(32)),
	Column('owner', String(40)),
	Column('topic', Integer),
	Column('topic_time', Integer),
	Column('topic_owner', String(40)),
	Column('antispam', Boolean),
	Column('autokick', String(5)),
	Column('censor', Boolean),
	Column('antishock', Boolean),
	)

chanuser_table = Table('chanuser', metadata,
	Column('id', Integer, primary_key=True),
	Column('name', String(40)),
	Column('channel', String(40)),
	Column('admin', Boolean),
	Column('banned', Boolean),
	Column('allowed', Boolean),
	Column('mute', Integer),
	)

bans_table = Table('ban_groups', metadata, # server bans
	Column('id', Integer, primary_key=True),
	Column('reason', Text),
	Column('end_time', Integer), # seconds since unix epoch
	)

aggregatebans_table = Table('ban_items', metadata, # server bans
	Column('id', Integer, primary_key=True),
	Column('type', String(10)), # what exactly is banned (username, ip, subnet, hostname, (ip) range, userid, )
	Column('data', String(60)), # regex would be cool
	Column('ban_id', Integer, ForeignKey('ban_groups.id')),
	)

mapper(User, users_table, properties={
	'logins':relation(Login, backref='user', cascade="all, delete, delete-orphan"),
	'renames':relation(Rename, backref='user', cascade="all, delete, delete-orphan"),
	})
mapper(Login, logins_table)
mapper(Rename, renames_table)
mapper(Channel, channels_table)
mapper(ChanUser, chanuser_table)
mapper(Ban, bans_table, properties={
	'entries':relation(AggregateBan, backref='ban', cascade="all, delete, delete-orphan"),
	})
mapper(AggregateBan, aggregatebans_table)

#metadata.create_all(engine)

class OfflineClient:
	def __init__(self, sqluser):
		self.db_id = sqluser.id
		self.username = sqluser.username
		self.password = sqluser.password
		self.ingame_time = sqluser.ingame_time
		self.bot = sqluser.bot
		self.last_login = sqluser.last_login
		self.register_date = sqluser.register_date
		self.hook = sqluser.hook_chars

class UsersHandler:
	def __init__(self, root, engine):
		self._root = root
		metadata.create_all(engine)
		self.sessionmaker = sessionmaker(bind=engine, autoflush=True, transactional=True)
	
	def clientFromID(self, db_id):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.id==db_id).first()
		session.close()
		if not entry: return None
		return OfflineClient(entry)
	
	def clientFromUsername(self, username):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.lowername==username.lower()).first()
		session.close()
		if not entry: return None
		return OfflineClient(entry)
	
	def check_ban(self, user=None, ip=None, userid=None):
		return True, None
		session = self.sessionmaker()
		results = session.query(AggregateBan)
		subnetbans = results.filter(AggregateBan.type=='subnet')
		userban = results.filter(AggregateBan.type=='user').filter(AggregateBan.data==user)
		ipban = results.filter(AggregateBan.type=='ip').filter(AggregateBan.data==ip)
		useridban = results.filter(AggregateBan.type=='userid').filter(AggregateBan.data==userid)
		session.close()
		
	def login_user(self, user, password, ip, lobby_id, user_id, cpu, local_ip, country):
		session = self.sessionmaker()
		name = user.lower()

		lanadmin = self._root.lanadmin
		if user == lanadmin['username'] and password == lanadmin['password']:
		 	sqluser = User(name, lanadmin['username'], password, ip, 'admin')
		 	return True, sqluser
		good = True
		now = int(time.time()*1000)
		entry = session.query(User).filter(User.lowername==name).first() # should only ever be one user with each name so we can just grab the first one :)
		reason = entry
		if not entry:
			return False, 'No user named %s'%user
		if not password == entry.password:
			good = False
			reason = 'Invalid password'
		#if entry.banned > 0: # update with _time_remaining() from protocol or wherever it is
			#if entry.banned > now:
				#good = False
				#timeleft = entry.banned - now
				#daysleft = '%.2f'%(float(seconds) / 60 / 60 / 24)
				#if daysleft >= 1:
					#reason = 'You are banned: (%s) days remaining.' % daysleft
				#else:
					#reason = 'You are banned: (%s) hours remaining.' % (float(seconds) / 60 / 60)
		entry.logins.append(Login(now, ip, lobby_id, user_id, cpu, local_ip, country))
		entry.last_login = now
		entry.last_ip = ip
		entry.last_id = user_id
		session.commit()
		session.close()
		return good, reason
	
	def end_session(self, username):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		if not entry.logins[-1].end: entry.logins[-1].end = int(time.time()*1000)
		session.commit()
		session.close()

	def register_user(self, user, password, ip, country): # need to add better ban checks so it can check if an ip address is banned when registering an account :)
		if len(user)>20: return False, 'Username too long'
		session = self.sessionmaker()
		if self._root.censor:
			if not self._root.SayHooks._nasty_word_censor(user):
				return False, 'Name failed to pass profanity filter.'
		name = user.lower()
		results = session.query(User).filter(User.lowername==name).first()
		lanadmin = self._root.lanadmin
		if name == lanadmin['username'].lower():
			if password == lanadmin['password']: # if you register a lanadmin account with the right user and pass combo, it makes it into a normal admin account
				if user in self._root.usernames:
					self._root.usernames[user] # what the *********************
				entry = User(name, lanadmin['username'], password, ip, 'admin')
				now = time.time()
				entry.logins.append(Login(now, ip, None, None, None, None, country))
				session.save(entry)
				session.commit()
				session.close()
				return True, 'Account registered successfully.'
			else: return False, 'Username already exists.'
		if results:
			return False, 'Username already exists.'
		entry = User(name, user, password, ip)
		session.save(entry)
		session.commit()
		session.close()
		return True, 'Account registered successfully.'
	
	def ban_user(self, username, duration, reason):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		end_time = int(time.time()*1000) + int(duration*1000)
		ban = Ban(reason, end_time)
		session.save(ban)
		ban.entries.append(AggregateBan('user', name))
		ban.entries.append(AggregateBan('ip', entry.last_ip))
		ban.entries.append(AggregateBan('userid', entry.last_id))
		session.commit()
		session.close()
		return 'Successfully banned %s for %s days.' % (username, duration)
	
	def unban_user(self, username):
		session = self.sessionmaker()
		results = session.query(AggregateBan).filter(AggregateBan.type=='user').filter(AggregateBan.data==username)
		if results:
			for result in results:
				session.delete(result.ban)
			session.commit()
			session.close()
			return 'Successfully unbanned %s.' % username
		else:
			session.close()
			return 'No matching bans for %s.' % username
	
	def banlist(self):
		session = self.sessionmaker()
		banlist = []
		for ban in session.query(Ban):
			current_ban = '%s (%s)' % (ban.end_time, ban.reason)
			for entry in ban.entries:
				current_ban += ' (%s - %s)' % (entry.type, entry.data)
			banlist.append(current_ban)
		session.close()
		return banlist

	def rename_user(self, user, newname):
		if len(newname)>20: return False, 'Username too long'
		session = self.sessionmaker()
		if self._root.censor:
			if not self._root.SayHooks._nasty_word_censor(user):
				return False, 'New username failed to pass profanity filter.'
		lnewname = newname.lower()
		if not lnewname == user.lower(): # this makes it so people can rename to a different case of the same name
			results = session.query(User).filter(User.lowername==lnewname).first()
			if results:
				return False, 'Username already exists.'
		entry = session.query(User).filter(User.lowername==user.lower()).first()
		if not entry: return False, 'You don\'t seem to exist anymore. Contact an admin or moderator.'
		entry.renames.append(Rename(user, newname))
		entry.lowername = lnewname
		entry.username = newname
		session.commit()
		session.close()
		# need to iterate through channels and rename junk there...
		# it might actually be a lot easier to use userids in the server... # later.
		return True, 'Account renamed successfully.'
	
	def save_user(self, client):
		session = self.sessionmaker()
		name = client.username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		if entry:
			entry.ingame_time = client.ingame_time
			entry.access = client.access
			entry.bot = client.bot
			entry.hook_chars = client.hook
			entry.last_id = client.last_id
		session.commit()
		session.close()
	
	def confirm_agreement(self, client):
		session = self.sessionmaker()
		name = client.username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		if entry: entry.access = 'user'
		session.commit()
		session.close()
	
	def get_lastlogin(self, username):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		session.close()
		if entry: return True, entry.last_login / 1000
		else: return False, 'User not found.'
	
	def get_registration_date(self, username):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		session.close()
		if entry: return True, entry.register_date
		else: return False, 'user not found in database'
	
	def get_ingame_time(self, username):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		session.close()
		if entry: return True, entry.ingame_time
		else: return False, 'user not found in database'
	
	def get_account_info(self, username):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		session.close()
		if entry:
			data = '%s %s %s %s %s %s %s %s %s %s %s %s %s' % (entry.lowername, entry.username, entry.password, entry.register_date, entry.last_login, entry.last_ip, entry.ingame_time, entry.last_id, entry.access, entry.bot, entry.access, entry.hook_chars)
			return True, data
		else: return False, 'user not found in database'
		
	
	def get_account_access(self, username):
		session = self.sessionmaker()
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
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
		name = username.lower()
		entry = session.query(User).filter(User.lowername==name).first()
		session.close()
		return entry.ip

	def remove_user(self, user):
		session = self.sessionmaker()
		entry = session.query(User).filter(User.lowername==user).first()
		if not entry:
			return False, 'User not found.'
		session.delete(entry)
		session.commit()
		session.close()
		return True, 'Success.'
	
	def load_channels(self):
		session = self.sessionmaker()
		response = session.query(Channel)
		channels = {}
		for chan in response:
			channels.append({'owner':chan.owner, 'key':chan.key, 'topic':chan.topic or '', 'antispam':chan.antispam, 'admins':[]})
		session.close()
		return channels
	
	def save_channel(self, channel):
		session = self.sessionmaker()
		entry = session.query(Channel)
		entry.key = channel.key
		entry.chanserv = channel.chanserv
		entry.owner = channel.owner
		topic = channel.topic
		if topic:
			topic = topic['text']
			topic_time = topic['time']
			topic_owner = topic['owner']
		else:
			topic, topic_time, topic_owner = ('', 0, '')
		entry.topic = topic
		entry.topic_time = topic_time
		entry.topic_owner = topic_owner
		entry.antispam = channel.antispam
		entry.autokick = channel.autokick
		entry.censor = channel.censor
		entry.antishock = channel.antishock
		session.save(entry)
		session.commit()
		session.close()

	def save_channels(self, channels):
		for channel in channels:
			self.save_channel(channel)

	def inject_user(self, user, password, ip, last_login, register_date, uid, ingame, country, bot, access):
		name = user.lower()
		entry = User(name, user, password, ip)
		entry.last_login = last_login
		entry.last_id = uid
		entry.ingame_time = ingame
		entry.register_date = register_date
		entry.access = access
		entry.bot = bot
		return entry
	
	def inject_users(self, accounts):
		session = self.sessionmaker()
		count = 0
		for user in accounts:
			count += 1
			if not count % 500:
				session.commit()
				session.close()
				session = self.sessionmaker()
			entry = self.inject_user(user['user'], user['pass'], user['last_ip'], user['last_login'], user['register_date'],
												user['uid'], user['ingame'], user['country'], user['bot'], user['access'])
			session.save(entry)
		session.commit()
		session.close()