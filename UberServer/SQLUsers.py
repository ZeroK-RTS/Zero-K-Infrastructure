from datetime import datetime, timedelta
import json, urllib2


class User:
	def __init__(self, username, password, lobby_id, dbuser):
		self.username = username
		self.password = password
		self.lobby_id = lobby_id
		self.access = dbuser['Access']
		self.id = dbuser['AccountID']
		self.bot = dbuser['IsBot']

class UsersHandler:
	def __init__(self, root, engine):
		self._root = root
		return
	
	def clientFromID(self, db_id):
		#LICHO implement todo
		return OfflineClient(entry)
	
	def clientFromUsername(self, username):
		#LICHO todo
		return OfflineClient(entry)
	
	
	def login_user(self, username, password, ip, lobby_id, user_id, cpu, local_ip, country):
		url = "{0}/Login?login={1}&password={2}&lobby_name={3}&user_id={4}&cpu={5}&ip={6}&country={7}".format(self._root.lobby_service_url, username, password,lobby_id,user_id, cpu, ip, country)
		data = json.load(urllib2.urlopen(url))

		if data['Ok']:
			dbuser = data['Account']
			reason = User(username, password, lobby_id, dbuser)
		else:
			reason = data['Reason']

		return data['Ok'], reason
	
	def end_session(self, db_id):
		#possibly log logout here
		return

	def register_user(self, user, password, ip, country): # need to add better ban checks so it can check if an ip address is banned when registering an account :)
		if len(user)>20: return False, 'Username too long'
		
		if self._root.censor:
			if not self._root.SayHooks._nasty_word_censor(user):
				return False, 'Name failed to pass profanity filter.'

		url = "{0}/Register?login={1}&password={2}&ip={3}&country={4}".format(self._root.lobby_service_url, username, password, ip, country)
		data = json.load(urllib2.urlopen(url))
		
		return data['Ok'], data['Reason']



class ChannelsHandler:
	def __init__(self, root, engine):
		self._root = root
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
	

