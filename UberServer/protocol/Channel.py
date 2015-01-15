from AutoDict import AutoDict
import time

class Channel(AutoDict):
	def __init__(self, root, name, users=[], admins=[],
						ban={}, allow=[], autokick='ban', 
						owner='', mutelist={}, antispam=False,
						censor=False, antishock=False, topic=None,
						key=None, **kwargs):
		self._root = root
		self.name = name
		self.users = users
		self.admins = admins
		self.ban = ban
		self.allow = allow
		self.autokick = autokick
		self.owner = owner
		self.mutelist = mutelist
		self.antispam = antispam
		self.censor = censor
		self.antishock = antishock
		self.topic = topic
		self.key = key
		self.__AutoDictInit__()


	def broadcast(self, message):
		self._root.broadcast(message, self.name)

	def channelMessage(self, message):
		self.broadcast('CHANNELMESSAGE %s %s' % (self.name, message))

	def register(self, client, owner):
		self.owner = owner.db_id

	def addUser(self, client):
		username = client.username
		if not username in self.users:
			self.users.append(username)
			self.broadcast('JOINED %s %s' % (self.name, username))

	def removeUser(self, client, reason=None):
		chan = self.name
		username = client.username

		if username in self.users:
			self.users.remove(username)

			if self.name in client.channels:
				client.channels.remove(chan)
			if reason and len(reason) > 0:
				self._root.broadcast('LEFT %s %s %s' % (chan, username, reason), chan)
			else:
				self._root.broadcast('LEFT %s %s' % (chan, username), chan)

	def isAdmin(self, client):
		return client and ('admin' in client.accesslevels)

	def isMod(self, client):
		return client and (('mod' in client.accesslevels) or self.isAdmin(client))

	def isFounder(self, client):
		return client and ((client.db_id == self.owner) or self.isMod(client))

	def isOp(self, client):
		return client and ((client.db_id in self.admins) or self.isFounder(client))

	def getAccess(self, client): # return client's security clearance
		return 'mod' if self.isMod(client) else\
				('founder' if self.isFounder(client) else\
				('op' if self.isOp(client) else\
				'normal'))

	def isMuted(self, client):
		return client.db_id in self.mutelist

	def getMuteMessage(self, client):
		if self.isMuted(client):
			m = self.mutelist[client.db_id]
			if m['expires'] == 0:
				return 'muted forever'
			else:
				 # TODO: move format_time, bin2dec, etc to a utilities class or module
				return 'muted for the next %s.' % (client._protocol._time_until(m['expires']))
		else:
			return 'not muted'

	def isAllowed(self, client):
		if self.autokick == 'allow':
			return (self.isOp(client) or (client.db_id in self.allow)) or 'not allowed here'
		elif self.autokick == 'ban':
			return (self.isOp(client) or (client.db_id not in self.ban)) or self.ban[client.db_id]

	def setTopic(self, client, topic):
		self.topic = topic

		if topic in ('*', None):
			if self.topic:
				self.channelMessage('Topic disabled.')
				topicdict = {}
		else:
			self.channelMessage('Topic changed.')
			topicdict = {'user':client.username, 'text':topic, 'time':time.time()}
			self.broadcast('CHANNELTOPIC %s %s %s %s'%(self.name, client.username, topicdict['time'], topic))
		self.topic = topicdict

	def setKey(self, client, key):
		if key in ('*', None):
			if self.key:
				self.key = None
				self.channelMessage('<%s> unlocked this channel' % client.username)
		else:
			self.key = key
			self.channelMessage('<%s> locked this channel with a password' % client.username)

	def setFounder(self, client, target):
		if not target: return
		self.owner = target.db_id
		self.channelMessage("<%s> has just been set as this channel's founder by <%s>" % (target.username, client.username))

	def opUser(self, client, target):
		if target and not target.db_id in self.admins:
			self.admins.append(target.db_id)
			self.channelMessage("<%s> has just been added to this channel's operator list by <%s>" % (target.username, client.username))

	def deopUser(self, client, target):
		if target and target.db_id in self.admins:
			self.admins.remove(target.db_id)
			self.channelMessage("<%s> has just been removed from this channel's operator list by <%s>" % (target.username, client.username))

	def kickUser(self, client, target, reason=''):
		if self.isFounder(target): return
		if target and target.username in self.users:
			target.Send('FORCELEAVECHANNEL %s %s %s' % (self.name, client.username, reason))
			self.channelMessage('<%s> has kicked <%s> from the channel%s' % (client.username, target.username, (' (reason: %s)'%reason if reason else '')))
			self.removeUser(target, 'kicked from channel%s' % (' (reason: %s)'%reason if reason else ''))

	def banUser(self, client, target, reason=''):
		if self.isFounder(target): return
		if target and not target.db_id in self.ban:
			self.ban[target.db_id] = reason
			self.kickUser(client, target, reason)
			self.channelMessage('<%s> has been banned from this channel by <%s>' % (target.username, client.username))

	def unbanUser(self, client, target):
		if target and target.db_id in self.ban:
			del self.ban[target.db_id]
			self.channelMessage('<%s> has been unbanned from this channel by <%s>' % (target.username, client.username))

	def allowUser(self, client, target):
		if target and not client.db_id in self.allow:
			self.allow.append(client.db_id)
			self.channelMessage('<%s> has been allowed in this channel by <%s>' % (target.username, client.username))

	def disallowUser(self, client, target):
		if target and client.db_id in self.allow:
			self.allow.remove(client.db_id)
			self.channelMessage('<%s> has been disallowed in this channel by <%s>' % (target.username, client.username))

	def muteUser(self, client, target, duration=0, ip=False, quiet=False):
		if self.isFounder(target): return
		if target and not client.db_id in self.mutelist:
			if not quiet:
				self.channelMessage('<%s> has muted <%s>' % (client.username, target.username))
			try:
				duration = float(duration)*60
				if duration < 1:
					duration = 0
				else:
					duration = time.time() + duration
			except: duration = 0
			self.mutelist[target.db_id] = {'expires':duration, 'ip':ip, 'quiet':quiet}

	def unmuteUser(self, client, target):
		if target and target.db_id in self.mutelist:
			del self.mutelist[target.db_id]
			self.channelMessage('<%s> has unmuted <%s>' % (client.username, target.username))

