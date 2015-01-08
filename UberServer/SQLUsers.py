from datetime import datetime, timedelta
import json, urllib2


class OfflineClient:
	def __init__(self, sqluser):
		self.id = sqluser.id
		self.username = sqluser.username
		self.password = sqluser.password
		self.bot = sqluser.bot
		self.last_id = sqluser.last_id
		self.access = sqluser.access

class UsersHandler:
	def __init__(self, root, engine):
		return
	
	def clientFromID(self, db_id):
		#LICHO implement todo
		return OfflineClient(entry)
	
	def clientFromUsername(self, username):
		#LICHO todo
		return OfflineClient(entry)
	
	
	def login_user(self, username, password, ip, lobby_id, user_id, cpu, local_ip, country):
		        
		url = "{0}/Login?login={1}&password={2}".format(self._root.lobby_service_url, username, password)
		data = json.load(urllib2.urlopen(url))
		print json.dumps(data)


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
		if good:
			reason = User(dbuser.username, password)
			reason.access = dbuser.access
			reason.id = dbuser.id
			reason.bot = dbuser.bot
			reason.lobby_id = lobby_id

		session.commit()
		session.close()
		return good, reason
	
	def end_session(self, db_id):
		#LICHO todo log logout
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
	
	

	
	


class ChannelsHandler:
	def __init__(self, root, engine):
		#LICHO TODO implement
		return
	
	def load_channels(self):
		channels = {}
		return channels
		#LICHO todo implement
		for chan in response:
			channels[chan.name] = {'owner':chan.owner, 'key':chan.key, 'topic':chan.topic or '', 'antispam':chan.antispam, 'admins':[]}
		return channels

	
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
	

