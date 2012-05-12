import inspect, time, re
import base64
try: from hashlib import md5
except: md5 = __import__('md5').new

import traceback, sys, os
import socket

ranks = (5, 15, 30, 100, 300, 1000, 3000, 10000)

restricted = {
'disabled':[],
'everyone':['TOKENIZE','TELNET','HASH','EXIT','PING'],
'fresh':['LOGIN','REGISTER','REQUESTUPDATEFILE'],
'agreement':['CONFIRMAGREEMENT'],
'user':[
	########
	# battle
	'ADDBOT',
	'ADDSTARTRECT',
	'DISABLEUNITS',
	'ENABLEUNITS',
	'ENABLEALLUNITS',
	'FORCEALLYNO',
	'FORCESPECTATORMODE',
	'FORCETEAMCOLOR',
	'FORCETEAMNO',
	'FORCEJOINBATTLE',
	'HANDICAP',
	'JOINBATTLE',
	'JOINBATTLEACCEPT',
	'JOINBATTLEDENY',
	'KICKFROMBATTLE',
	'LEAVEBATTLE',
	'MAPGRADES',
	'MYBATTLESTATUS',
	'OPENBATTLE',
	'OPENBATTLEEX',
	'REMOVEBOT',
	'REMOVESTARTRECT',
	'RING',
	'SAYBATTLE',
	'SAYBATTLEHOOKED',
	'SAYBATTLEEX',
	'SAYBATTLEPRIVATE',
	'SAYBATTLEPRIVATEEX',
	'SCRIPT',
	'SCRIPTEND',
	'SCRIPTSTART',
	'SETSCRIPTTAGS',
	'UPDATEBATTLEINFO',
	'UPDATEBOT',
	'UPDATEBATTLEDETAILS',
	#########
	# channel
	'CHANNELMESSAGE',
	'CHANNELS',
	'CHANNELTOPIC',
	'FORCELEAVECHANNEL',
	'JOIN',
	'LEAVE',
	'MUTE',
	'MUTELIST',
	'SAY',
	'SAYHOOKED',
	'SAYEX',
	'SAYPRIVATE',
	'SAYPRIVATEEX',
	'SAYPRIVATEHOOKED',
	'SETCHANNELKEY',
	'UNMUTE',
	########
	# meta
	'CHANGEPASSWORD',
	'GETINGAMETIME',
	'GETREGISTRATIONDATE',
	'HOOK',
	'KILLALL',
	'MYSTATUS',
	'PORTTEST',
	'UPTIME',
	'RENAMEACCOUNT',
	'USERID'],
'mod':[
	'BAN', 'BANUSER', 'BANIP', 'UNBAN', 'BANLIST',
	'CHANGEACCOUNTPASS',
	'KICKUSER', 'FINDIP', 'GETIP', 'GETLASTLOGINTIME','GETUSERID'
	'FORCECLOSEBATTLE', 'SETBOTMODE', 'TESTLOGIN', 'GENERATEUSERID'
	],
'admin':[
	#########
	# channel
	'ALIAS','UNALIAS','ALIASLIST',
	#########
	# server
	'ADMINBROADCAST', 'BROADCAST','BROADCASTEX','RELOAD',
	#########
	# users
	'FORGEMSG','FORGEREVERSEMSG',
	'GETLOBBYVERSION', 'GETSENDBUFFERSIZE',
	'GETACCOUNTINFO', 'GETLASTLOGINTIME',
	'GETACCOUNTACCESS', 
	'SETACCESS','DEBUG','PYTHON',
	'SETINGAMETIME',],
}

restricted_list = []
for level in restricted:
	restricted_list += restricted[level]

ipRegex = r"^([01]?\d\d?|2[0-4]\d|25[0-5])\.([01]?\d\d?|2[0-4]\d|25[0-5])\.([01]?\d\d?|2[0-4]\d|25[0-5])\.([01]?\d\d?|2[0-4]\d|25[0-5])$"
re_ip = re.compile(ipRegex)

def validateIP(ipAddress):
	return re_ip.match(ipAddress)

class AutoDict:
# method 1
#	def __getitem__(self, item):
#		return self.__getattribute__(item)
#	
#	def __setitem__(self, item, value):
#		return self.__setattr__(item, value)
	
# method 2
#	def __getitem__(self, item):
#		item = str(item)
#		if not '__' in item and hasattr(self, item):
#			return getattr(self, item)
#
#	def __setitem__(self, item, value):
#		item = str(item)
#		if not '__' in item and hasattr(self, item):
#			setattr(self, item, value)
	
	def keys(self):
		return filter(lambda x: not '__' in x, self.dir)
	
	def update(self, **kwargs):
		keys = self.keys()
		for key in kwargs:
			if key in keys:
				setattr(self, key, kwargs[key])
	
	def copy(self):
		d = {}
		for key in self.keys():
			d[key] = getattr(self, key)
		return d
	
	def __AutoDictInit__(self):
		self.dir = dir(self)
		for key in self.keys():
			new = getattr(self, key)
			ntype = type(new)
			if ntype in (list, dict, set):
				new = ntype(new)
				setattr(self, key, new)

class Battle(AutoDict):
	def __init__(self, root, id, type, natType, password, port, maxplayers,
						hashcode, rank, maphash, map, title, modname,
						passworded, host, users, spectators=0,
						startrects={}, disabled_units=[], pending_users=set(),
						authed_users=set(), bots={}, script_tags={},
						replay_script={}, replay=False,
						sending_replay_script=False, locked=False,
						engine=None, version=None, extended=False):
		self._root = root
		self.id = id
		self.type = type
		self.natType = natType
		self.password = password
		self.port = port
		self.maxplayers = maxplayers
		self.spectators = spectators
		self.hashcode = hashcode
		self.rank = rank
		self.maphash = maphash
		self.map = map
		self.title = title
		self.modname = modname
		self.passworded = passworded
		self.users = users
		self.host = host
		self.startrects = startrects
		self.disabled_units = disabled_units
		
		self.pending_users = pending_users
		self.authed_users = authed_users

		self.engine = (engine or 'spring').lower()
		self.version = version or root.latestspringversion
		self.extended = extended

		self.bots = bots
		self.script_tags = script_tags
		self.replay_script = replay_script
		self.replay = replay
		self.sending_replay_script = sending_replay_script
		self.locked = locked
		self.spectators = 0
		self.__AutoDictInit__()

class Channel(AutoDict):
	def __init__(self, root, chan, users=[], blindusers=[], admins=[],
						ban={}, allow=[], autokick='ban', chanserv=False,
						owner='', mutelist={}, antispam=False,
						censor=False, antishock=False, topic=None,
						key=None, **kwargs):
		self._root = root
		self.chan = chan
		self.users = users
		self.blindusers = blindusers
		self.admins = admins
		self.ban = ban
		self.allow = allow
		self.autokick = autokick
		self.chanserv = chanserv
		self.owner = owner
		self.mutelist = mutelist
		self.antispam = antispam
		self.censor = censor
		self.antishock = antishock
		self.topic = topic
		self.key = key
		self.__AutoDictInit__()
		
		if chanserv and self._root.chanserv and not chan in self._root.channels:
			self._root.chanserv.Send('JOIN %s' % self.chan)
	
	def broadcast(self, message):
		self._root.broadcast(message, self.chan)
	
	def channelMessage(self, message):
		self.broadcast('CHANNELMESSAGE %s %s' % (self.chan, message))
	
	def register(self, client, owner):
		self.owner = owner.db_id
	
	def addUser(self, client):
		username = client.username
		if not username in self.users:
			self.users.append(username)
			self.broadcast('JOINED %s %s' % (self.chan, username))
	
	def removeUser(self, client, reason=''):
		chan = self.chan
		username = client.username
		
		if username in self.users:
			self.users.remove(username)
			if username in self.blindusers:
				self.blindusers.remove(username)
				
			if self.chan in client.channels:
				client.channels.remove(chan)
			
			self._root.broadcast('LEFT %s %s' % (chan, username), chan, self.blindusers)
	
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
			topicdict = {'user':client.username, 'text':topic, 'time':'%s'%(int(time.time())*1000)}
			self.broadcast('CHANNELTOPIC %s %s %s %s'%(self.chan, client.username, topicdict['time'], topic))
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
			target.Send('FORCELEAVECHANNEL %s %s %s' % (self.chan, client.username, reason))
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

class Protocol:
	def __init__(self, root, handler):
		self._root = root
		self.handler = handler
		self.userdb = root.getUserDB()
		self.SayHooks = root.SayHooks
		self.dir = dir(self)

	def _new(self, client):
		if self._root.dbtype == 'lan': lan = '1'
		else: lan = '0'
		login_string = ' '.join((self._root.server, str(self._root.server_version), self._root.latestspringversion, str(self._root.natport), lan))
		client.SendNow(login_string)
		
	def _remove(self, client, reason='Quit'):
		if client.username and client.username in self._root.usernames:
			if client.removing: return
			if client.static: return # static clients don't disconnect
			client.removing = True
			user = client.username
			if not client == self._root.usernames[user]:
				client.removing = False # 'cause we really aren't anymore
				return
				
			self.userdb.end_session(user)
			
			channels = list(client.channels)
			del self._root.usernames[user]
			if client.db_id in self._root.db_ids:
				del self._root.db_ids[client.db_id]
			
			for chan in channels:
				channel = self._root.channels[chan]
				if user in channel.users:
					channel.users.remove(user)
					if user in channel.blindusers:
						channel.blindusers.remove(user)
				self._root.broadcast('LEFT %s %s %s'%(chan, user, reason), chan, user)
				
			battle_id = client.current_battle
			if battle_id:
				self.in_LEAVEBATTLE(client)
			
			self.broadcast_RemoveUser(client)
		if client.session_id in self._root.clients: del self._root.clients[client.session_id]

	def _handle(self, client, msg):
		if msg.startswith('#'):
			test = msg.split(' ')[0][1:]
			if test.isdigit():
				msg_id = '#%s '%test
				msg = ' '.join(msg.split(' ')[1:])
			else:
				msg_id = ''
		else:
			msg_id = ''
		# client.Send() prepends client.msg_id if the current thread
		# is the same thread as the client's handler.
		# this works because handling is done in order for each ClientHandler thread
		# so we can be sure client.Send() was performed in the client's own handling code.

		client.msg_id = msg_id
		numspaces = msg.count(' ')
		if numspaces:
			command,args = msg.split(' ',1)
		else:
			command = msg
		command = command.upper()

		access = []
		for level in client.accesslevels:
			access += restricted[level]
		
		if command in restricted_list:
			if not command in access:
				client.Send('SERVERMSG %s failed. Insufficient rights.'%command)
				return False
		else:
			if not 'user' in client.accesslevels:
				client.Send('SERVERMSG %s failed. Insufficient rights.'%command)
				return False
		
		command = 'in_%s' % command
		if command in self.dir:
			function = getattr(self, command)
		else:
			client.Send('SERVERMSG %s failed. Command does not exist.'%(command.split('_',1)[1]))
			return False
		function_info = inspect.getargspec(function)
		total_args = len(function_info[0])-2

		# if there are no arguments, just call the function
		if not total_args:
			function(client)
			return True

		# check for optional arguments
		optional_args = 0
		if function_info[3]:
			optional_args = len(function_info[3])

		# check if we've got enough words for filling the required args
		required_args = total_args - optional_args
		if numspaces < required_args:
			client.Send('SERVERMSG %s failed. Incorrect arguments.'%('_'.join(command.split('_')[1:])))
			return False
		if required_args == 0 and numspaces == 0:
			function(client)
			return True

		# bunch the last words together if there are too many of them
		if numspaces > total_args-1:
			arguments = args.split(' ',total_args-1)
		else:
			arguments = args.split(' ')
		function(*([client]+arguments))
		# TODO: check the exception line... if it's "function(*([client]+arguments))"
		# then it was incorrect arguments. if not, log the error, as it was a code problem
		#try:
		#	function(*([client]+arguments))
		#except TypeError:
		#	client.Send('SERVERMSG %s failed. Incorrect arguments.'%command.partition('in_')[2])
		return True

	def _bin2dec(self, s):
		return int(s, 2)

	def _dec2bin(self, i, bits=None):
		i = int(i)
		b = ''
		while i > 0:
			j = i & 1
			b = str(j) + b
			i >>= 1
		if bits:
			b = b.rjust(bits,'0')
		return b

	def _udp_packet(self, username, ip, udpport):
		if username in self._root.usernames:
			client = self._root.usernames[username]
			if ip == client.local_ip or ip == client.ip_address:
				client.Send('UDPSOURCEPORT %i'%udpport)
				battle_id = client.current_battle
				if not battle_id in self._root.battles: return
				battle = self._root.battles[battle_id]
				if battle:
					client.udpport = udpport
					client.hostport = udpport
					host = battle.host
					if not host == username:
						self._root.usernames[host].SendBattle(battle, 'CLIENTIPPORT %s %s %s'%(username, ip, udpport))
				else:
					client.udpport = udpport
			else:
				self._root.admin_broadcast('NAT spoof from %s pretending to be <%s>'%(ip,username))

	def _calc_access_status(self, client):
		self._calc_access(client)
		self._calc_status(client, client.status)

	def _calc_access(self, client):
		userlevel = client.access
		inherit = {'mod':['user'], 'admin':['mod', 'user']}
		
		if userlevel in inherit:
			inherited = inherit[userlevel]
		else:
			inherited = [userlevel]
		if not client.access in inherited: inherited.append(client.access)
		client.accesslevels = inherited+['everyone']

	def _calc_status(self, client, status):
		status = self._dec2bin(status, 7)
		bot, access, rank1, rank2, rank3, away, ingame = status[-7:]
		rank1, rank2, rank3 = self._dec2bin(6, 3)
		accesslist = {'user':0, 'mod':1, 'admin':1}
		access = client.access
		if access in accesslist:
			access = accesslist[access]
		else:
			access = 0
		bot = int(client.bot)
		ingame_time = float(client.ingame_time/60) # hours
		
		rank = 0
		for t in ranks:
			if ingame_time >= t:
				rank += 1
		
		rank1, rank2, rank3 = self._dec2bin(rank, 3)
		client.is_ingame = (ingame == '1')
		client.away = (away == '1')
		status = self._bin2dec('%s%s%s%s%s%s%s'%(bot, access, rank1, rank2, rank3, away, ingame))
		client.status = status
		return status

	def _calc_battlestatus(self, client):
		battlestatus = client.battlestatus
		status = self._bin2dec('0000%s%s0000%s%s%s%s%s0'%(battlestatus['side'],
								battlestatus['sync'], battlestatus['handicap'],
								battlestatus['mode'], battlestatus['ally'],
								battlestatus['id'], battlestatus['ready']))
		return status
	
	def _new_channel(self, chan, **kwargs):
		# any updates to channels from the SQL database from a web interface
		# would possibly need to call a RELOAD-type function
		# unless we want to do way more SQL lookups for channel info
		try:
			if not kwargs: raise KeyError
			channel = Channel(self._root, chan, **kwargs)
		except: channel = Channel(self._root, chan)
		return channel
	
	def _time_format(self, seconds):
		'given a duration in seconds, returns a human-readable relative time'
		minutesleft = float(seconds) / 60
		hoursleft = minutesleft / 60
		daysleft = hoursleft / 24
		if daysleft > 7:
			message = '%0.2f weeks' % (daysleft / 7)
		elif daysleft == 7:
			message = 'a week'
		elif daysleft > 1:
			message = '%0.2f days' % daysleft
		elif daysleft == 1:
			message = 'a day'
		elif hoursleft > 1:
			message = '%0.2f hours' % hoursleft
		elif hoursleft == 1:
			message = 'an hour'
		elif minutesleft > 1:
			message = '%0.1f minutes' % minutesleft
		elif minutesleft == 1:
			message = 'a minute'
		else:
			message = '%0.0f second(s)'%(float(seconds))
		return message
	
	def _time_until(self, timestamp):
		'given a future timestamp, as returned by time.time(), returns a human-readable relative time'
		seconds = timestamp - time.time()
		if seconds <= 0:
			return 'forever'
		else:
			seconds = seconds - time.time()
			return self._time_format(seconds)
	
	def _time_since(self, timestamp):
		'given a past timestamp, as returned by time.time(), returns a readable relative time as a string'
		seconds = time.time() - timestamp
		return self._time_format(seconds)

	def clientFromID(self, db_id):
		'given a user database id, returns a client object from memory or the database'
		return self._root.clientFromID(db_id) or self.userdb.clientFromID(db_id)
	
	def clientFromUsername(self, username):
		'given a username, returns a client object from memory or the database'
		client = self._root.clientFromUsername(username)
		if not client:
			client = self.userdb.clientFromUsername(username)
			if client:
				client.db_id = client.id
				self._calc_access(client)
		return client

	def broadcast_AddBattle(self, battle):
		'queues the protocol for adding a battle - experiment in loose thread-safety'
		users = dict(self._root.usernames)
		for name in users:
			users[name].AddBattle(battle)
		
	def broadcast_RemoveBattle(self, battle):
		'queues the protocol for removing a battle - experiment in loose thread-safety'
		users = dict(self._root.usernames)
		for name in users:
			users[name].RemoveBattle(battle)
	
	def broadcast_SendBattle(self, battle, data):
		'queues the protocol for sending text in a battle - experiment in loose thread-safety'
		users = list(battle.users)
		for name in users:
			if name in self._root.usernames:
				self._root.usernames[name].SendBattle(battle, data)
	
	def broadcast_AddUser(self, user):
		'queues the protocol for adding a user - experiment in loose thread-safety'
		users = dict(self._root.usernames)
		for name in users:
			if not name == user.username:
				users[name].AddUser(user)
	
	def broadcast_RemoveUser(self, user):
		'queues the protocol for removing a user - experiment in loose thread-safety'
		users = dict(self._root.usernames)
		for name in users:
			if not name == user.username:
				users[name].RemoveUser(user)

	def broadcast_SendUser(self, user, data):
		'queues the protocol for receiving a user-specific message - experiment in loose thread-safety'
		users = dict(self._root.usernames)
		for name in users:
			users[name].SendUser(user, data)

	def client_AddUser(self, client, user):
		'sends the protocol for adding a user'
		if client.compat_accountIDs:
			client.Send('ADDUSER %s %s %s %s' % (user.username, user.country_code, user.cpu, user.db_id))
		else:
			client.Send('ADDUSER %s %s %s' % (user.username, user.country_code, user.cpu))
	
	def client_RemoveUser(self, client, user):
		'sends the protocol for removing a user'
		client.Send('REMOVEUSER %s' % user.username)
	
	def client_AddBattle(self, client, battle):
		'sends the protocol for adding a battle'
		ubattle = battle.copy()
		if not battle.host in self._root.usernames: return

		host = self._root.usernames[battle.host]
		if host.ip_address == client.ip_address: # translates the ip to always be compatible with the client
			translated_ip = host.local_ip
		else:
			translated_ip = host.ip_address
		
		ubattle.update({'ip':translated_ip})
		if client.compat_extendedBattles:
			client.Send('BATTLEOPENEDEX %(id)s %(type)s %(natType)s %(host)s %(ip)s %(port)s %(maxplayers)s %(passworded)s %(rank)s %(maphash)s %(engine)s %(version)s %(map)s\t%(title)s\t%(modname)s' % ubattle)
		else:
			if not (battle.engine == 'spring' and (battle.version == self._root.latestspringversion or battle.version == self._root.latestspringversion + '.0')):
				ubattle['title'] = 'Incompatible (%(engine)s %(version)s) %(title)s' % ubattle

			client.Send('BATTLEOPENED %(id)s %(type)s %(natType)s %(host)s %(ip)s %(port)s %(maxplayers)s %(passworded)s %(rank)s %(maphash)s %(map)s\t%(title)s\t%(modname)s' % ubattle)
	
	def client_RemoveBattle(self, client, battle):
		'sends the protocol for removing a battle'
		client.Send('BATTLECLOSED %s' % battle.id)

	# Begin incoming protocol section #
	#
	# any function definition beginning with in_ and ending with capital letters
	# is a definition of an incoming command.
	#
	# any text arguments passed by the client are automatically split and passed to the method
	# keyword arguments are treated as optional
	# this is done in the _handle() method above
	#
	# example (note, this is not the actual in_SAY method used in the server):
	#
	# def in_SAY(self, client, channel, message=None):
	#     if message:
	#         sendToChannel(channel, message)
	#     else:
	#         sendToChannel(channel, "I'm too cool to send a message")
	#
	# if the client sends "SAY foo bar", the server calls in_SAY(client, "foo", "bar")
	# if the client sends "SAY foo", the server will call in_SAY(client, "foo")
	#
	# however, if the client sends "SAY",
	# the server will notice the client didn't send enough text to fill the arguments
	# and return an error message to the client

	def in_PING(self, client, reply=None):
		'''
		Tell the server you are in fact still connected.
		The server will reply with PONG, useful for testing latency.

		@optional.str reply: Reply to send client
		'''
		if reply:
			client.Send('PONG %s'%reply)
		else:
			client.Send('PONG')
	
	def in_PORTTEST(self, client, port):
		'''
		Connect to client on specified UDP port and send the string 'Port testing...'


		@required.int port: UDP port to connect to for port testing
		'''
		host = client.ip_address
		port = int(port)
		sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		sock.sendto('Port testing...', (host, port))

	def in_REGISTER(self, client, username, password):
		'''
		Register a new user in the account database.

		@required.str username: Username to register
		@required.str password: Password to use, usually encoded md5+base64
		'''
		for char in username:
			if not char.lower() in 'abcdefghijklmnopqrstuvwzyx[]_1234567890':
				client.Send('REGISTRATIONDENIED Unicode names are currently disallowed.')
				return
		if len(username) > 20:
			client.Send('REGISTRATIONDENIED Username is too long.')
			return
		good, reason = self.userdb.register_user(username, password, client.ip_address, client.country_code)
		if good:
			self._root.console_write('Handler %s: Successfully registered user <%s> on session %s.'%(client.handler.num, username, client.session_id))
			client.Send('REGISTRATIONACCEPTED')
			self.userdb.clientFromUsername(username).access = 'agreement'
		else:
			self._root.console_write('Handler %s: Registration failed for user <%s> on session %s.'%(client.handler.num, username, client.session_id))
			client.Send('REGISTRATIONDENIED %s'%reason)
	
	def in_TELNET(self, client):
		'''
		Set the client to a telnet client, which provides a simple interface for speaking in one channel.
		'''
		client.telnet = True
		client.Send('Welcome, telnet user.')
	
	def in_HASH(self, client):
		'''
		After this command has been used, the password argument to LOGIN will be automatically hashed with md5+base64.
		'''
		client.hashpw = True
		if client.telnet:
			client.Send('Your password will be hashed for you when you login.')

	def in_LOGIN(self, client, username, password='', cpu='0', local_ip='', sentence_args=''):
		'''
		Attempt to login the active client.

		@required.str username: Username
		@required.str password: Password, usually encoded md5+base64
		@optional.int cpu: CPU speed
		@optional.ip local_ip: LAN IP address, sent to clients when they have the same WAN IP as host
		@optional.sentence.str lobby_id: Lobby name and version
		@optional.sentence.int user_id: User ID provided by lobby
		@optional.sentence.str compat_flags: Compatibility flags, sent in space-separated form, as follows:

		flag: description
		-----------------
		a: Send account IDs as an additional parameter to ADDUSER. Account IDs persist across renames.
		b: If client is hosting a battle, prompts them with JOINBATTLEREQUEST when a user tries to join their battle
		sp: If client is hosting a battle, sends them other clients' script passwords as an additional argument to JOINEDBATTLE.
		et: When client joins a channel, sends NOCHANNELTOPIC if the channel has no topic.
		eb: Enables receiving extended battle commands, like BATTLEOPENEDEX
		'''
		if not username:
			client.Send('DENIED Invalid username.')
			return

		try: int(cpu)
		except: cpu = '0'

		if not validateIP(local_ip): local_ip = client.ip_address
		if '\t' in sentence_args:
			lobby_id, user_id = sentence_args.split('\t',1)
			if '\t' in user_id:
				user_id, compFlags = user_id.split('\t', 1)
				
				flags = set()
				
				for flag in compFlags.split(' '):
					if flag in ('ab', 'ba'):
						flags.add('a')
						flags.add('b')
					else:
						flags.add(flag)
				
				for flag in flags:
					if flag == 'a': # send account IDs in ADDUSER
						client.compat_accountIDs = True
					elif flag == 'b': # JOINBATTLEREQUEST/ACCEPT/DENY
						client.compat_battleAuth = True
					elif flag == 'sp': # scriptPassword in JOINEDBATTLE
						client.compat_scriptPassword = True
					elif flag == 'et': # send NOCHANNELTOPIC on join if channel has no topic
						client.compat_sendEmptyTopic = True
					elif flag == 'eb': # extended battle commands with support for engine/version
						client.compat_extendedBattles = True
						
			if user_id.replace('-','',1).isdigit():
				user_id = int(user_id)
				if user_id > 2147483647:
					user_id &= 2147483647
					user_id *= -1
		else:
			lobby_id = sentence_args
			user_id = 0
		if client.hashpw:
			m = md5(password)
			password = base64.b64encode(m.digest())
		good, reason = self.userdb.login_user(username, password, client.ip_address, lobby_id, user_id, cpu, local_ip, client.country_code)
		if good: username = reason.username
		if not username in self._root.usernames:
			if good:
				client.logged_in = True
				client.access = reason.access
				self._calc_access(client)
				client.username = username
				if client.access == 'agreement':
					self._root.console_write('Handler %s: Sent user <%s> the terms of service on session %s.'%(client.handler.num, username, client.session_id))
					agreement = ['AGREEMENT {\\rtf1\\ansi\\ansicpg1250\\deff0\\deflang1060{\\fonttbl{\\f0\\fswiss\\fprq2\\fcharset238 Verdana;}{\\f1\\fswiss\\fprq2\\fcharset238{\\*\\fname Arial;}Arial CE;}{\\f2\\fswiss\\fcharset238{\\*\\fname Arial;}Arial CE;}}',
					'AGREEMENT {\\*\\generator Msftedit 5.41.15.1507;}\\viewkind4\\uc1\\pard\\ul\\b\\f0\\fs22 Terms of Use\\ulnone\\b0\\f1\\fs20\\par',
					'AGREEMENT \\f2\\par',
					'AGREEMENT \\f0\\fs16 While the administrators and moderators of this server will attempt to keep spammers and players violating this agreement off the server, it is impossible for them to maintain order at all times. Therefore you acknowledge that any messages in our channels express the views and opinions of the author and not the administrators or moderators (except for messages by these people) and hence will not be held liable.\\par',
					'AGREEMENT \\par',
					'AGREEMENT You agree not to use any abusive, obscene, vulgar, slanderous, hateful, threatening, sexually-oriented or any other material that may violate any applicable laws. Doing so may lead to you being immediately and permanently banned (and your service provider being informed). You agree that the administrators and moderators of this server have the right to mute, kick or ban you at any time should they see fit. As a user you agree to any information you have entered above being stored in a database. While this information will not be disclosed to any third party without your consent administrators and moderators cannot be held responsible for any hacking attempt that may lead to the data being compromised. Passwords are sent and stored in encoded form. Any personal information such as personal statistics will be kept privately and will not be disclosed to any third party.\\par',
					'AGREEMENT \\par',
					'AGREEMENT By using this service you hereby agree to all of the above terms.\\fs18\\par',
					'AGREEMENT \\f2\\fs20\\par',
					'AGREEMENT }',
					'AGREEMENTEND']
					for line in agreement: client.Send(line)
					return
				self._root.console_write('Handler %s: Successfully logged in user <%s> on session %s.'%(client.handler.num, username, client.session_id))
				
				if client.ip_address in self._root.trusted_proxies:
					client.setFlagByIP(local_ip, False)
				
				if reason.id == None:
					client.db_id = client.session_id
				else:
					client.db_id = reason.id
				self._root.db_ids[client.db_id] = client
				
				client.ingame_time = int(reason.ingame_time)
				client.bot = reason.bot
				client.last_login = reason.last_login
				client.register_date = reason.register_date
				client.hook = reason.hook_chars
				client.username = username
				client.password = password
				client.cpu = cpu
				client.local_ip = None
				if local_ip.startswith('127.') or not validateIP(local_ip):
					client.local_ip = client.ip_address
				else:
					client.local_ip = local_ip
				client.lobby_id = lobby_id
				self._root.usernames[username] = client
				client.Send('ACCEPTED %s'%username)
				
				client.Send('MOTD Welcome, %s!' % username)
				client.Send('MOTD There are currently %i clients connected' % len(self._root.clients))
				client.Send('MOTD to the server talking in %i open channels' % len(self._root.channels))
				client.Send('MOTD and participating in %i battles.' % len(self._root.battles))
				client.Send('MOTD Server\'s uptime is %s' % self._time_since(self._root.start_time))
				
				if self._root.motd:
					client.Send('MOTD')
					for line in list(self._root.motd):
						client.Send('MOTD %s' % line)
				
				self.broadcast_AddUser(client)
				
				usernames = dict(self._root.usernames) # cache them here in case anyone joins/leaves or hosts/closes a battle
				for user in usernames:
						addclient = usernames[user]
						client.AddUser(addclient)
				
				battles = dict(self._root.battles)
				for battle in battles:
					battle = battles[battle]
					ubattle = battle.copy()
					client.AddBattle(battle)
					client.SendBattle(battle, 'UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % ubattle)
					for user in battle.users:
						if not user == battle.host:
							client.SendBattle(battle, 'JOINEDBATTLE %s %s' % (battle.id, user))
				
				client.status = self._calc_status(client, 0)
				self.broadcast_SendUser(client, 'CLIENTSTATUS %s %s'%(username, client.status))
				for user in usernames:
					if user == username: continue # potential problem spot, might need to check to make sure username is still in user db
					client.SendUser(user, 'CLIENTSTATUS %s %s'%(user, usernames[user].status))
					
				client.Send('LOGININFOEND')
			else:
				self._root.console_write('Handler %s: Failed to log in user <%s> on session %s. (rejected by database)'%(client.handler.num, username, client.session_id))
				client.Send('DENIED %s'%reason)
		else:
			oldclient = self._root.usernames[username]
			if oldclient.static:
				client.Send('DENIED Cannot ghost static users.')

			if time.time() - oldclient.lastdata > 15:
				if self._root.dbtype == 'lan' and not oldclient.password == password:
					client.Send('DENIED Would ghost old user, but we are in LAN mode and your password does not match.')
					return
				
				# kicks old user and logs in new user
				oldclient.Remove('Ghosted')
				self._root.console_write('Handler %s: Old client inactive, ghosting user <%s> from session %s.'%(client.handler.num, username, client.session_id))
				self.in_LOGIN(client, username, password, cpu, local_ip, sentence_args)
			else:
				self._root.console_write('Handler %s: Failed to log in user <%s> on session %s. (already logged in)'%(client.handler.num, username, client.session_id))
				client.Send('DENIED Already logged in.')

	def in_CONFIRMAGREEMENT(self, client):
		'Confirm the terms of service as shown with the AGREEMENT commands. Users must accept the terms of service to use their account.'
		if client.access == 'agreement':
			client.access = 'user'
			self.userdb.save_user(client)
			client.access = 'fresh'
			self._calc_access_status(client)

	def in_HOOK(self, client, chars=''):
		'''
		Enable SAY hooking for this client session.

		@required.str chars: When a SAY command in a channel is prefixed with these characters, the server will intercept and pass to a command hook system.
		'''
		chars = chars.strip()
		if chars.count(' '): return
		client.hook = chars
		if chars:
			client.Send('SERVERMSG Hooking commands enabled. Use help if you don\'t know what you\'re doing. Prepend commands with "%s"'%chars)
		elif client.hook:
			client.Send('SERVERMSG Hooking commands disabled.')
		self.userdb.save_user(client)
	
	def in_SAYHOOKED(self, client, chan, msg):
		'''
		Execute a hooked command in a channel.
		This allows clients to decide when to hook commands instead of depending on the server's default method.

		@required.str channel: The channel in which to run the command.
		@required.str message: The hooked text to parse as a command.
		'''
		if not msg: return
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			user = client.username
			if user in channel.users:
				self.SayHooks.hook_SAY(self, client, chan, msg)
	
	#def in_SAYEXHOOKED(self, client, chan, msg): # sayex hook was only for filtering
	#	if not msg: return
	#	if chan in self._root.channels:
	#		channel = self._root.channels[chan]
	#		user = client.username
	#		if user in channel.users:
	#			self.SayHooks.hook_SAYEX(self,client,chan,msg)
	
	def in_SAYPRIVATEHOOKED(self, client, user, msg):
		'''
		Execute a hooked command in a private message.
		This allows clients to decide when to hook commands instead of depending on the server's default method.

		@required.str user: The user for which to run the command.
		@required.str message: The hooked text to parse as a command.
		'''
		if not msg: return
		user = client.username
		self.SayHooks.hook_SAYPRIVATE(self, client, user, msg)
	
	def in_SAYBATTLEHOOKED(self, client, msg):
		'''
		Execute a hooked command in a battle.
		This allows clients to decide when to hook commands instead of depending on the server's default method.

		@required.str message: The hooked text to parse as a command.
		'''
		battle_id = client.current_battle
		if not battle_id in self._root.battles: return
		if not client in self._root.battles['users']: return
		self.SayHooks.hook_SAYBATTLE(self, client, battle_id, msg)

	def in_SAY(self, client, chan, msg):
		'''
		Send a message to all users in specified channel.
		The client must be in the channel to send it a message.

		@required.str channel: The target channel.
		@required.str message: The message to send.
		'''
		if not msg: return
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			user = client.username
			if user in channel.users:
				msg = self.SayHooks.hook_SAY(self, client, chan, msg) # comment out to remove sayhook # might want at the beginning in case someone needs to unban themselves from a channel # nevermind, i just need to add inchan :>
				if not msg or not msg.strip(): return
				if channel.isMuted(client):
					client.Send('CHANNELMESSAGE %s You are %s.' % (chan, channel.getMuteMessage(client)))
				else:
					self._root.broadcast('SAID %s %s %s' % (chan, client.username, msg), chan, client.reverse_ignore)

	def in_SAYEX(self, client, chan, msg):
		'''
		Send an action to all users in specified channel.
		The client must be in the channel to show an action.

		@required.str channel: The target channel.
		@required.str message: The action to send.
		'''
		if not msg: return
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			user  = client.username
			if user in channel.users:
				msg = self.SayHooks.hook_SAYEX(self, client, chan, msg) # comment out to remove sayhook # might want at the beginning in case someone needs to unban themselves from a channel
				if not msg or not msg.strip(): return
				if channel.isMuted(client):
					client.Send('CHANNELMESSAGE %s You are %s.' % (chan, channel.getMuteMessage(client)))
				else:
					self._root.broadcast('SAIDEX %s %s %s' % (chan, client.username, msg), chan, client.reverse_ignore)

	def in_SAYPRIVATE(self, client, user, msg):
		'''
		Send a message in private to another user.

		@required.str user: The target user.
		@required.str message: The message to send.
		'''
		if not msg: return
		if user in self._root.usernames:
			msg = self.SayHooks.hook_SAYPRIVATE(self, client, user, msg) # comment out to remove sayhook
			if not msg or not msg.strip(): return
			client.Send('SAYPRIVATE %s %s'%(user, msg))
			self._root.usernames[user].Send('SAIDPRIVATE %s %s'%(client.username, msg))

	def in_SAYPRIVATEEX(self, client, user, msg):
		'''
		Send an action in private to another user.

		@required.str user: The target user.
		@required.str message: The action to send.
		'''
		if not msg: return
		if user in self._root.usernames:
			msg = self.SayHooks.hook_SAYPRIVATE(self, client, user, msg) # comment out to remove sayhook
			if not msg or not msg.strip(): return
			client.Send('SAYPRIVATEEX %s %s'%(user, msg))
			self._root.usernames[user].Send('SAIDPRIVATEEX %s %s'%(client.username, msg))

	def in_MUTE(self, client, chan, user, duration=None, args=''):
		'''
		Mute target user in target channel.
		[operator]

		@required.str channel: The target channel.
		@required.str user: The user to mute.
		@optional.float duration: The duration for which to mute the user. Defaults to forever.
		@optional.str args: Space-separated additional arguments to the mute, as follows:

		arg: description
		--------------------
		quiet: doesn't send a message to the channel about the mute
		ip: mutes by IP address
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if channel.isOp(client):
				ip = False
				quiet = False
				if args:
					for arg in args.lower().split(' '):
						if arg == 'ip':
							ip = True
						elif arg == 'quiet':
							quiet = True
				target = self.clientFromUsername(user)
				if target:
					channel.muteUser(client, target, duration, quiet, ip)

	def in_UNMUTE(self, client, chan, user):
		'''
		Unmute target user in target channel.
		[operator]

		@required.str channel: The target channel.
		@required.str user: The user to unmute.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if channel.isOp(client):
				target = self.clientFromUsername(user)
				if target:
					channel.unmuteUser(client, target)

	def in_MUTELIST(self, client, chan): # maybe restrict to open channels and channels you are in - not locked
		'''
		Return the list of muted users in target channel.

		@required.str channel: The target channel.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			mutelist = dict(channel.mutelist)
			client.Send('MUTELISTBEGIN %s' % chan)
			for user in mutelist:
				m = mutelist[user].copy()
				user = self.clientFromID(user).username
				message = self._time_until(m['expires']) + (' by IP.' if m['ip'] else '.')
				client.Send('MUTELIST %s, %s' % (user, message))
			client.Send('MUTELISTEND')

	def in_JOIN(self, client, chan, key=None):
		'''
		Attempt to join target channel.
		
		@required.str channel: The target channel.
		@optional.str password: The password to use for joining if channel is locked.
		'''
		for char in chan:
			if not char.lower() in 'abcdefghijklmnopqrstuvwzyx[]_1234567890':
				client.Send('JOINFAILED %s Unicode channels are not allowed.' % chan)
				return
		if len(chan) > 20:
			client.Send('JOINFAILED %s Channel name is too long.' % chan)
			return
		
		alreadyaliased = []
		run = True
		blind = False
		nolock = False
		while run:
			alreadyaliased.append(chan)
			if chan in self._root.chan_alias:
				alias = self._root.chan_alias[chan]
				chan, blind, nolock = (alias['chan'], alias['blind'], alias['nolock'])
				if chan in alreadyaliased: run = False # hit infinite loop
			else:
				run = False
		user = client.username
		chan = chan.lstrip('#')
		if not chan: return
		if not chan in self._root.channels:
			channel = self._new_channel(chan)
			self._root.channels[chan] = channel
		else:
			channel = self._root.channels[chan]
		if user in channel.users:
			if user in channel.blindusers and not blind:
				channel.blindusers.remove(user)
				client.Send('FORCELEAVECHANNEL %s server Vision restored.' % chan)
				client.Send('JOIN %s' % chan)
				client.Send('CLIENTS %s %s'%(chan, ' '.join(channel.users)))
			elif user not in channel.blindusers and blind:
				channel.blindusers.append(user)
				client.Send('FORCELEAVECHANNEL %s server Going blind.' % chan)
				client.Send('JOIN %s' % chan)
				client.Send('CLIENTS %s %s' % (chan, user))
		else:
			if not channel.isFounder(client):
				if channel.key and not nolock and not channel.key == key:
					client.Send('JOINFAILED %s Invalid key' % chan)
					return
				elif channel.autokick == 'ban' and client.db_id in channel.ban:
					client.Send('JOINFAILED %s You are banned from the channel %s' % (chan, channel.ban[client.db_id]))
					return
				elif channel.autokick == 'allow' and client.db_id not in channel.allow:
					client.Send('JOINFAILED %s You are not allowed' % chan)
					return
			if not chan in client.channels:
				client.channels.append(chan)
			client.Send('JOIN %s'%chan)
			if not blind:
				self._root.broadcast('JOINED %s %s' % (chan, user), chan, channel.blindusers)
				channel.users.append(user)
				client.Send('CLIENTS %s %s'%(chan, ' '.join(channel.users)))
			else:
				self._root.broadcast('JOINED %s %s'%(chan,user), chan, channel.blindusers)
				channel.users.append(user)
				channel.blindusers.append(user)
				client.Send('CLIENTS %s %s'%(chan, user))
				
			topic = channel.topic
			if topic:
				client.Send('CHANNELTOPIC %s %s %s %s'%(chan, topic['user'], topic['time'], topic['text']))
			elif client.compat_sendEmptyTopic:
				client.Send('NOCHANNELTOPIC %s' % chan)
				
		# disabled because irc bridge spams JOIN commands
		#
		# a user can rejoin a channel to get the topic while in it
		#topic = channel.topic
		#if topic and user in channel.users:
		#	client.Send('CHANNELTOPIC %s %s %s %s'%(chan, topic['user'], topic['time'], topic['text']))
	
	def in_SETCHANNELKEY(self, client, chan, key='*'):
		'''
		Lock target channel with a password, or unlocks target channel.

		@required.str channel: The target channel.
		@optional.str password: The password to set. To unlock a channel, leave this blank or set to '*'.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if channel.isOp(client):
				channel.setKey(client, key)
	
	def in_LEAVE(self, client, chan):
		'''
		Leave target channel.
		
		@required.str channel: The target channel.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			channel.removeUser(client)
	
	def in_MAPGRADES(self, client, grades=None):
		'''
		Stub.
		Replies with MAPGRADESFAILED to keep old clients happy.
		'''
		client.Send('MAPGRADESFAILED Not implemented.')

	def in_OPENBATTLE(self, client, type, natType, password, port, maxplayers, hashcode, rank, maphash, sentence_args):
		'''
		Host a new battle with the arguments specified.

		@required.int type: The type of battle to host.
		#0: Battle
		#1: Hosted replay
		
		@required.int natType: The method of NAT transversal to use.
		#0: None
		#1: Hole punching
		#2: Fixed source ports

		@required.str password: The password to use, or "*" to use no password.
		@required.int port: 
		@required.int maxplayers: 
		@required.sint modhash: Mod hash, as returned by unitsync.dll.
		@required.int rank: Recommended minimum rank to join the battle. Current ranks range from 0-7.
		@required.sint maphash: Map hash, as returned by unitsync.dll.
		@required.sentence.str mapName: The map name.
		@required.sentence.str title: The battle's title.
		@required.sentence.str modName: The mod name.
		'''
		if client.current_battle in self._root.battles:
			self.in_LEAVEBATTLE(client)

		if sentence_args.count('\t') > 1:
			map, title, modname = sentence_args.split('\t',2)

			if not modname:
				client.Send('OPENBATTLEFAILED No mod name specified.')
				return
			if not map:
				client.Send('OPENBATTLEFAILED No map name specified.')
				return
		else:
			return False
		battle_id = str(self._root.nextbattle)
		self._root.nextbattle += 1
		client.current_battle = battle_id
		if password == '*':
			passworded = 0
		else:
			passworded = 1
		
		host = client.username
		battle = Battle(
						root=self._root, id=battle_id, type=type, natType=int(natType),
						password=password, port=port, maxplayers=maxplayers, hashcode=hashcode,
						rank=rank, maphash=maphash, map=map, title=title, modname=modname,
						passworded=passworded, host=host, users=[host], extended=False
					)
		ubattle = battle.copy()
		
		try:
			int(battle_id), int(type), int(natType), int(passworded), int(port), int(maphash)
		except:
			client.current_battle = None
			client.Send('OPENBATTLEFAILED Invalid argument type, send this to your lobby dev:'
						'id=%(id)s type=%(type)s natType=%(natType)s passworded=%(passworded)s port=%(port)s maphash=%(maphash)s' % ubattle)
			return
			
		self.broadcast_AddBattle(battle)
		self._root.battles[battle_id] = battle
		client.Send('OPENBATTLE %s'%battle_id)
		client.Send('REQUESTBATTLESTATUS')

	def in_OPENBATTLEEX(self, client, type, natType, password, port, maxplayers, hashcode, rank, maphash, engine, version, sentence_args):
		'''
		Host a new extended battle with the arguments specified.

		@required.int type: The type of battle to host.
		#0: Battle
		#1: Hosted replay
		
		@required.int natType: The method of NAT transversal to use.
		#0: None
		#1: Hole punching
		#2: Fixed source ports

		@required.str password: The password to use, or "*" to use no password.
		@required.int port: 
		@required.int maxplayers: 
		@required.sint modhash: Mod hash, as returned by unitsync.dll.
		@required.int rank: Recommended minimum rank to join the battle. Current ranks range from 0-7.
		@required.sint maphash: Map hash, as returned by unitsync.dll.
		@required.str engine: The engine name, lowercase, with no spaces.
		@required.str version: The engine version.
		@required.sentence.str mapName: The map name.
		@required.sentence.str title: The battle's title.
		@required.sentence.str modName: The mod name.
		'''
		if client.current_battle in self._root.battles:
			self.in_LEAVEBATTLE(client)
		
		if sentence_args.count('\t') > 1:
			map, title, modname = sentence_args.split('\t', 2)

			if not modname:
				client.Send('OPENBATTLEFAILED No mod name specified.')
				return
			if not map:
				client.Send('OPENBATTLEFAILED No map name specified.')
				return
		else:
			return False
		
		battle_id = str(self._root.nextbattle) # not thread-safe
		self._root.nextbattle += 1
		client.current_battle = battle_id
		if password == '*':
			passworded = 0
		else:
			passworded = 1
		
		host = client.username
		battle = Battle(
						root=self._root, id=battle_id, type=type, natType=int(natType),
						password=password, port=port, maxplayers=maxplayers, hashcode=hashcode,
						rank=rank, maphash=maphash, map=map, title=title, modname=modname,
						passworded=passworded, host=host, users=[host],
						engine=engine, version=version, extended=True
					)
		ubattle = battle.copy()

		try:
			int(battle_id), int(type), int(natType), int(passworded), int(port), int(maphash)
		except:
			client.current_battle = None
			client.Send('OPENBATTLEFAILED Invalid argument type, send this to your lobby dev:'
						'id=%(id)s type=%(type)s natType=%(natType)s passworded=%(passworded)s port=%(port)s maphash=%(maphash)s' % ubattle)
			return
		
		self.broadcast_AddBattle(battle)
		self._root.battles[battle_id] = battle
		client.Send('OPENBATTLE %s' % battle_id)
		client.Send('REQUESTBATTLESTATUS')

	def in_SAYBATTLE(self, client, msg):
		'''
		Send a message to all users in your current battle.

		@required.str message: The message to send.
		'''
		if not msg: return
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			user = client.username
			msg = self.SayHooks.hook_SAYBATTLE(self, client, battle_id, msg)
			if not msg or not msg.strip(): return
			self.broadcast_SendBattle(battle, 'SAIDBATTLE %s %s' % (user, msg))

	def in_SAYBATTLEEX(self, client, msg):
		'''
		Send an action to all users in your current battle.

		@required.str message: The action to send.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			self.broadcast_SendBattle(battle, 'SAIDBATTLEEX %s %s' % (client.username, msg))
	
	def in_SAYBATTLEPRIVATE(self, client, username, msg):
		'''
		Send a message to one target user in your current battle.
		[host]

		@required.str username: The user to receive your message.
		@required.str message: The message to send.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host and username in battle.users:
				user = self.clientFromUsername(username)
				if user:
					user.Send('SAIDBATTLE %s %s' % (client.username, msg))
	
	def in_SAYBATTLEPRIVATEEX(self, client, username, msg):
		'''
		Send an action to one target user in your current battle.
		[host]

		@required.str username: The user to receive your action.
		@required.str message: The action to send.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host and username in battle.users:
				user = self.clientFromUsername(username)
				if user:
					user.Send('SAIDBATTLEEX %s %s' % (client.username, msg))

	def in_JOINBATTLEACCEPT(self, client, username):
		'''
		Allow a user to join your battle, sent as a response to JOINBATTLEREQUEST.
		[host]

		@required.str username: The user to allow into your battle.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if not client.username == battle.host: return
			if username in battle.pending_users:
				battle.pending_users.remove(username)
				battle.authed_users.add(username)
				user = self._root.clientFromUsername(username)
				if user:
					self.in_JOINBATTLE(user, battle_id)
	
	def in_JOINBATTLEDENY(self, client, username, reason=None):
		'''
		Deny a user from joining your battle, sent as a response to JOINBATTLEREQUEST.
		[host]

		@required.str username: The user to deny from joining your battle.
		@optional.str reason: The reason to provide to the user.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if not client.username == battle.host: return
			if username in battle.pending_users:
				battle.pending_users.remove(username)
				user = self._root.clientFromUsername(username)
				if user:
					user.Send('JOINBATTLEFAILED %s%s' % ('Denied by host', (' ('+reason+')' if reason else '')))

	def in_JOINBATTLE(self, client, battle_id, password=None, scriptPassword=None):
		'''
		Attempt to join target battle.

		@required.int battleID: The ID of the battle to join.
		@optional.str password: The password to use if the battle requires one.
		@optional.str scriptPassword: A password unique to your user, to verify users connecting to the actual game.
		'''
		if scriptPassword: client.scriptPassword = scriptPassword
		
		username = client.username
		if client.current_battle in self._root.battles:
			client.Send('JOINBATTLEFAILED You are already in a battle.')
			return
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if not username in battle.users:
				host = self._root.clientFromUsername(battle.host)
				if battle.passworded == 1 and not battle.password == password:
					if not (host.compat_battleAuth and username in battle.authed_users):
						client.Send('JOINBATTLEFAILED Incorrect password.')
						return
				if battle.locked:
					client.Send('JOINBATTLEFAILED Battle is locked.')
					return
				if username in host.battle_bans: # TODO: make this depend on db_id instead
					client.Send('JOINBATTLEFAILED <%s> has banned you from their battles.' % battle.host)
					return
				if host.compat_battleAuth and not username in battle.authed_users:
					battle.pending_users.add(username)
					host.Send('JOINBATTLEREQUEST %s %s' % (username, client.ip_address))
					return
				battle_users = battle.users
				battle_bots = battle.bots
				startrects = battle.startrects
				client.Send('JOINBATTLE %s %s' % (battle_id, battle.hashcode))
				battle.users.append(username)
				scripttags = []
				script_tags = dict(battle.script_tags)
				for tag in script_tags:
					scripttags.append('%s=%s'%(tag, script_tags[tag]))
				client.Send('SETSCRIPTTAGS %s'%'\t'.join(scripttags))
				if battle.disabled_units:
					client.Send('DISABLEUNITS %s' % ' '.join(battle.disabled_units))
				self._root.broadcast('JOINEDBATTLE %s %s' % (battle_id, username), ignore=(battle.host, username))
				
				scriptPassword = client.scriptPassword
				if host.compat_scriptPassword and scriptPassword:
					host.Send('JOINEDBATTLE %s %s %s' % (battle_id, username, scriptPassword))
					client.Send('JOINEDBATTLE %s %s %s' % (battle_id, username, scriptPassword))
				else:
					host.Send('JOINEDBATTLE %s %s' % (battle_id, username))
					client.Send('JOINEDBATTLE %s %s' % (battle_id, username))
				
				if battle.natType > 0:
					host = battle.host
					if host == username:
						raise NameError, '%s is having an identity crisis' % (host)
					if client.udpport:
						self._root.usernames[host].Send('CLIENTIPPORT %s %s %s' % (username, client.ip_address, client.udpport))
				
				specs = 0
				for username in battle.users:
					user = self.clientFromUsername(username)
					if user and user.battlestatus['mode'] == '0':
						specs += 1
				
				for user in battle_users:
					battle_user = self._root.usernames[user]
					battlestatus = self._calc_battlestatus(battle_user)
					teamcolor = battle_user.teamcolor
					if battlestatus and teamcolor:
						client.Send('CLIENTBATTLESTATUS %s %s %s' % (user, battlestatus, teamcolor))
				for iter in battle_bots:
					bot = battle_bots[iter]
					client.Send('ADDBOT %s %s' % (battle_id, iter)+' %(owner)s %(battlestatus)s %(teamcolor)s %(AIDLL)s' % (bot))
				for allyno in startrects:
					rect = startrects[allyno]
					client.Send('ADDSTARTRECT %s' % (allyno)+' %(left)s %(top)s %(right)s %(bottom)s' % (rect))
				client.battlestatus = {'ready':'0', 'id':'0000', 'ally':'0000', 'mode':'0', 'sync':'00', 'side':'00', 'handicap':'0000000'}
				client.teamcolor = '0'
				client.current_battle = battle_id
				client.Send('REQUESTBATTLESTATUS')
				return
		client.Send('JOINBATTLEFAILED Unable to join battle.')

	def in_SETSCRIPTTAGS(self, client, scripttags):
		'''
		Set script tags and send them to all clients in your battle.

		@required.str scriptTags: A tab-separated list of key=value pairs.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				setscripttags = {}
				for tagpair in scripttags.split('\t'):
					if not '=' in tagpair:
						continue # this fails; tag isn't split by anything
					(tag, value) = tagpair.split('=',1)
					setscripttags.update({tag:value})
				scripttags = []
				for tag in setscripttags:
					scripttags.append('%s=%s'%(tag.lower(), setscripttags[tag]))
				battle.script_tags.update(setscripttags)
				if not scripttags:
					return
				self._root.broadcast_battle('SETSCRIPTTAGS %s'%'\t'.join(scripttags), battle_id)
	
	def in_SCRIPTSTART(self, client):
		'''
		Start sending a script to server.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username:
				battle.replay_script = []
				if battle.sending_replay_script:
					battle.sending_replay_script = False
				else:
					battle.sending_replay_script = True
	
	def in_SCRIPT(self, client, scriptline):
		'''
		Send a line of a script to the server.
		Note: Scripts over 512KB will be discarded.

		@required.str line: Another line of the script to save in the server.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username:
				if battle.sending_replay_script:
					if len(battle.replay_script) > 512 and len('\n'.join(battle.replay_script)) > 512*1024:
						battle.sending_replay_script = False
						client.Send('SERVERMSG Script too long (over 512KB). Discarding.')
					else:
						battle.replay_script.append('%s\n'%scriptline)

	def in_SCRIPTEND(self, client):
		'''
		Finish sending the script.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username:
				if battle.sending_replay_script:
					battle.replay = True
					battle.sending_replay_script = False

	def in_LEAVEBATTLE(self, client):
		'''
		Leave current battle.
		'''
		client.scriptPassword = None
		
		username = client.username
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username:
				self.broadcast_RemoveBattle(battle)
				client.hostport = None
				del self._root.battles[battle_id]
				client.current_battle = None
			elif username in battle.users:
				battle.users.remove(username)
				if username in battle.authed_users:
					battle.authed_users.remove(username)
					
				battle_bots = dict(client.battle_bots)
				for bot in battle_bots:
					del client.battle_bots[bot]
					if bot in battle.bots:
						del battle.bots[bot]
						self._root.broadcast_battle('REMOVEBOT %s %s' % (battle_id, bot), battle_id)
				self._root.broadcast('LEFTBATTLE %s %s'%(battle_id, client.username))
				client.current_battle = None
				
				oldspecs = battle.spectators
			
				specs = 0
				for username in battle.users:
					user = self.clientFromUsername(username)
					if user and user.battlestatus['mode'] == '0':
						specs += 1
				
				battle.spectators = specs
				if oldspecs != specs:
					self._root.broadcast('UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % battle.copy())

	def in_MYBATTLESTATUS(self, client, battlestatus, myteamcolor):
		'''
		Set your status in a battle.

		@required.int status: The status to set, formatted as an awesome bitfield.
		@required.sint teamColor: Teamcolor to set. Format is hex 0xBBGGRR represented as decimal.
		'''
		try:
			if int(battlestatus) < 1:
				battlestatus = str(int(battlestatus) + 2147483648)
		except:
			client.Send('SERVERMSG MYBATTLESTATUS failed - invalid status (%s).'%battlestatus)
			return
		if not myteamcolor.isdigit():
			client.Send('SERVERMSG MYBATTLESTATUS failed - invalid teamcolor (%s).'%myteamcolor)
			return
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			spectating = (client.battlestatus['mode'] == '0')

			clients = (self.clientFromUsername(name) for name in battle.users)
			spectators = len([user for user in clients if (user.battlestatus['mode'] == '0')])

			u, u, u, u, side1, side2, side3, side4, sync1, sync2, u, u, u, u, handicap1, handicap2, handicap3, handicap4, handicap5, handicap6, handicap7, mode, ally1, ally2, ally3, ally4, id1, id2, id3, id4, ready, u = self._dec2bin(battlestatus, 32)[-32:]
			# support more allies and ids.
			#u, u, u, u, side1, side2, side3, side4, sync1, sync2, u, u, u, u, handicap1, handicap2, handicap3, handicap4, handicap5, handicap6, handicap7, mode, ally1, ally2, ally3, ally4,ally5, ally6, ally7, ally8, id1, id2, id3, id4,id5, id6, id7, id8, ready, u = self._dec2bin(battlestatus, 40)[-40:]
			
			if spectating:
				if len(battle.users) - spectators >= int(battle.maxplayers):
					mode = '0'
				elif mode == '1':
					spectators -= 1
			elif mode == '0':
				spectators += 1
			
			oldstatus = self._calc_battlestatus(client)
			client.battlestatus.update({'ready':ready, 'id':id1+id2+id3+id4, 'ally':ally1+ally2+ally3+ally4, 'mode':mode, 'sync':sync1+sync2, 'side':side1+side2+side3+side4})
			client.teamcolor = myteamcolor
			
			oldspecs = battle.spectators
			battle.spectators = spectators
			
			if oldspecs != spectators:
				self._root.broadcast('UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % battle.copy())
			
			newstatus = self._calc_battlestatus(client)
			statuscmd = 'CLIENTBATTLESTATUS %s %s %s'%(client.username, newstatus, myteamcolor)
			if oldstatus != newstatus:
				self._root.broadcast_battle(statuscmd, client.current_battle)
			else:
				client.Send(statuscmd) # in case we changed anything

	def in_UPDATEBATTLEINFO(self, client, SpectatorCount, locked, maphash, mapname):
		'''
		Update public properties of your battle.
		[host]

		@required.int spectators: The number of spectators in your battle.
		@required.int locked: A boolean (0 or 1) of whether battle is locked.
		@required.sint mapHash: A 32-bit signed hash of the current map as returned by unitsync.
		@required.str mapName: The name of the current map.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username:
				old = battle.copy()
				updated = {'id':battle_id, 'locked':int(locked), 'maphash':maphash, 'map':mapname}
				battle.update(**updated)
				
				oldstr = 'UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % old
				newstr = 'UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % battle.copy()
				if oldstr != newstr:
					self._root.broadcast(newstr)

	def in_MYSTATUS(self, client, status):
		'''
		Set your client status, to be relayed to all other clients.

		@required.int status: A bitfield of your status. The server forces a few values itself, as well.
		'''
		if not status.isdigit():
			client.Send('SERVERMSG MYSTATUS failed - invalid status.')
			return
		was_ingame = client.is_ingame
		client.status = self._calc_status(client, status)
		if client.is_ingame and not was_ingame:
			battle_id = client.current_battle
			if battle_id in self._root.battles:
				battle = self._root.battles[battle_id]
				host = battle.host
				
				if len(battle.users) > 1:
					client.went_ingame = time.time()
				else:
					client.went_ingame = None
				if client.username == host:
					if client.hostport:
						self._root.broadcast_battle('HOSTPORT %i' % client.hostport, battle_id, host)
					if battle.replay:
						self._root.broadcast_battle('SCRIPTSTART', battle_id, client.username)
						for line in battle.replay_script:
							self._root.broadcast_battle('SCRIPT %s' % line, battle_id, client.username)
						self._root.broadcast_battle('SCRIPTEND', battle_id, client.username)
		elif was_ingame and not client.is_ingame and client.went_ingame:
			ingame_time = (time.time() - client.went_ingame) / 60
			if ingame_time >= 1:
				client.ingame_time += int(ingame_time)
				self.userdb.save_user(client)
		if not client.username in self._root.usernames: return
		self._root.broadcast('CLIENTSTATUS %s %s'%(client.username, client.status))

	def in_CHANNELS(self, client):
		'''
		Return a listing of all channels on the server.
		'''
		channels = []
		for channel in self._root.channels.values():
			if channel.owner and not channel.key:
				channels.append(channel)
		
		if not channels:
			client.Send('SERVERMSG No channels are currently visible (they must be registered and unlocked).')
			return
		
		for channel in channels:
			chaninfo = '%s %s'%(channel.chan, len(channel.users))
			topic = channel.topic
			if topic:
				chaninfo = '%s %s'%(chaninfo, topic['text']) # TASClient doesn't show the topic in CHANNELS unless it has a space, by the way.
			client.Send('CHANNEL %s'%chaninfo)
		client.Send('ENDOFCHANNELS')

	def in_CHANNELTOPIC(self, client, chan, topic):
		'''
		Set the topic in target channel.
		[operator]

		@required.str channel: The target channel.
		@required.str topic: The topic to set.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if channel.isOp(client):
				channel.setTopic(client, topic)

	def in_CHANNELMESSAGE(self, client, chan, message):
		'''
		Send a server message to target channel.

		@required.str channel: The target channel.
		@required.str message: The message to send.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if channel.isOp(client):
				channel.channelMessage(message)

	def in_FORCELEAVECHANNEL(self, client, chan, username, reason=''):
		'''
		Kick target user from target channel.

		@required.str channel: The target channel.
		@required.str username: The target user.
		@optional.str reason: A reason for kicking the user..
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if channel.isOp(client):
				target = self._root.clientFromUsername(username)
				if target and username in channel.users:
					channel.kickUser(client, target, reason)
				else:
					client.Send('SERVERMSG <%s> not in channel #%s' % (username, chan))

	def in_RING(self, client, username):
		'''
		Send target user a ringing notification, normally used for idle users in battle.
		[host]

		@required.str username: The target user.
		'''
		user = self._root.clientFromUsername(username)

		if not user: return
		if not 'mod' in client.accesslevels:
			battle_id = client.current_battle
			if battle_id:
				battle = self._root.battles[battle_id]
				if not battle.host in (client.username, username):
					return
				if not username in battle.users:
					return
			else:
				return

		user.Send('RING %s' % (client.username))


	def in_FORCEJOINBATTLE(self, client, username, battleid):
		user = self._root.clientFromUsername(username)
		
		if not user: return
		if not 'mod' in client.accesslevels:
		    return
		
		user.send('FORCEJOINBATTLE %s' % (battleid))


	def in_ADDSTARTRECT(self, client, allyno, left, top, right, bottom):
		'''
		Add a start rectangle for an ally team.
		[host]

		@required.int allyno: The ally number for the rectangle.
		@required.float left: The left side of the rectangle.
		@required.float top: The top side of the rectangle.
		@required.float right: The right side of the rectangle.
		@required.float bottom: The bottom side of the rectangle.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username:
				rect = {'left':left, 'top':top, 'right':right, 'bottom':bottom}
				battle.startrects[allyno] = rect
				self._root.broadcast_battle('ADDSTARTRECT %s' % (allyno)+' %(left)s %(top)s %(right)s %(bottom)s' %(rect), client.current_battle, [client.username])

	def in_REMOVESTARTRECT(self, client, allyno):
		'''
		Remove a start rectangle for an ally team.
		[host]

		@required.int allyno: The ally number for the rectangle.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if battle.host == client.username and allyno in battle.startrects:
				del battle.startrects[allyno]
				self._root.broadcast_battle('REMOVESTARTRECT %s' % allyno, client.current_battle, [client.username])

	def in_DISABLEUNITS(self, client, units):
		'''
		Add a list of units to disable.
		[host]

		@required.str units: A string-separated list of unit names to disable.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				units = units.split(' ')
				disabled_units = []
				for unit in units:
					if not unit in battle.disabled_units:
						battle.disabled_units.append(unit)
						disabled_units.append(unit)
				if disabled_units:
					disabled_units = ' '.join(disabled_units)
					self._root.broadcast_battle('DISABLEUNITS %s'%disabled_units, battle_id, client.username)

	def in_ENABLEUNITS(self, client, units):
		'''
		Remove units from the disabled unit list.
		[host]

		@required.str units: A string-separated list of unit names to enable.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				units = units.split(' ')
				enabled_units = []
				for unit in units:
					if unit in battle.disabled_units:
						battle.disabled_units.remove(unit)
						enabled_units.append(unit)
				if enabled_units:
					enabled_units = ' '.join(enabled_units)
					self._root.broadcast_battle('ENABLEUNITS %s'%enabled_units, battle_id, client.username)

	def in_ENABLEALLUNITS(self, client):
		'''
		Enable all units.
		[host]
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				battle.disabled_units = []
				self._root.broadcast_battle('ENABLEALLUNITS', battle_id, client.username)

	def in_HANDICAP(self, client, username, value):
		'''
		Change the handicap value for a player.
		[host]

		@required.str username: The player to handicap.
		@required.int handicap: The percentage of handicap to give (1-100).
		'''
		battle_id = client.current_battle
		if not value.isdigit() or not int(value) in range(0, 101):
			return

		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				if username in battle.users:
					client = self._root.usernames[username]
					client.battlestatus['handicap'] = self._dec2bin(value, 7)
					self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), battle_id)

	def in_KICKFROMBATTLE(self, client, username):
		'''
		Kick a player from their battle.
		[host]

		@required.str username: The player to kick.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host or 'mod' in client.accesslevels:
				if username in battle.users:
					kickuser = self._root.usernames[username]
					kickuser.Send('FORCEQUITBATTLE')
					if username == battle.host:
						self.broadcast_RemoveBattle(battle)
						del self._root.battles[battle_id]
					else:
						self.in_LEAVEBATTLE(kickuser)
			else:
				client.Send('SERVERMSG You must be the battle host to kick from a battle.')

	def in_FORCETEAMNO(self, client, username, teamno):
		'''
		Force target player's team number.
		[host]

		@required.str username: The target player.
		@required.int teamno: The team to assign them.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				if username in battle.users:
					client = self._root.usernames[username]
					client.battlestatus['id'] = self._dec2bin(teamno, 4)
					self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), battle_id)

	def in_FORCEALLYNO(self, client, username, allyno):
		'''
		Force target player's ally team number.
		[host]

		@required.str username: The target player.
		@required.int teamno: The ally team to assign them.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				if username in battle.users:
					client = self._root.usernames[username]
					client.battlestatus['ally'] = self._dec2bin(allyno, 4)
					self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), battle_id)

	def in_FORCETEAMCOLOR(self, client, username, teamcolor):
		'''
		Force target player's team color.
		[host]

		@required.str username: The target player.
		@required.sint teamcolor: The color to assign, represented with hex 0xBBGGRR as a signed integer.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				if username in battle.users:
					client = self._root.usernames[username]
					client.teamcolor = teamcolor
					self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), battle_id)

	def in_FORCESPECTATORMODE(self, client, username):
		'''
		Force target player to become a spectator.
		[host]

		@required.str username: The target player.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if client.username == battle.host:
				if username in battle.users:
					client = self._root.usernames[username]
					if client.battlestatus['mode'] == '1':
						battle.spectators += 1
						client.battlestatus['mode'] = '0'
						self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), battle_id)
						self._root.broadcast('UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % battle.copy())

	def in_ADDBOT(self, client, name, battlestatus, teamcolor, AIDLL):
		'''
		Add a bot to the current battle.
		[battle]

		@required.str name: The name of the bot.
		@required.int battlestatus: The battle status of the bot.
		@required.sint teamcolor: The color to assign, represented with hex 0xBBGGRR as a signed integer.
		@required.str AIDLL: The name of the DLL loading the bot.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if not name in battle.bots:
				client.battle_bots[name] = battle_id
				battle.bots[name] = {'owner':client.username, 'battlestatus':battlestatus, 'teamcolor':teamcolor, 'AIDLL':AIDLL}
				self._root.broadcast_battle('ADDBOT %s %s %s %s %s %s'%(battle_id, name, client.username, battlestatus, teamcolor, AIDLL), battle_id)

	def in_UPDATEBOT(self, client, name, battlestatus, teamcolor):
		'''
		Update battle status and teamcolor for a bot.
		[battle]

		@required.str name: The name of the bot.
		@required.int battlestatus: The battle status of the bot.
		@required.sint teamcolor: The color to assign, represented with hex 0xBBGGRR as a signed integer.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if name in battle.bots:
				if client.username == battle.bots[name]['owner'] or client.username == battle.host:
					battle.bots[name].update({'battlestatus':battlestatus, 'teamcolor':teamcolor})
					self._root.broadcast_battle('UPDATEBOT %s %s %s %s'%(battle_id, name, battlestatus, teamcolor), battle_id)
					

					
	
	def in_REMOVEBOT(self, client, name):
		'''
		Remove a bot from the active battle.
		[battle]

		@required.str name: The name of the bot.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			if name in battle.bots:
				if client.username == battle.bots[name]['owner'] or client.username == battle.host:
					del self._root.usernames[battle.bots[name]['owner']].battle_bots[name]
					del battle.bots[name]
				 	self._root.broadcast_battle('REMOVEBOT %s %s'%(battle_id, name), battle_id)
	
	def in_FORCECLOSEBATTLE(self, client, battle_id=None):
		'''
		Force a battle to close.

		@optional.int battle_id: The battle ID to close. Defaults to current battle.
		'''
		if not battle_id: battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			self.in_KICKFROMBATTLE(client, battle.host)
		else:
			client.Send('SERVERMSG Invalid battle ID.')
	
	def in_GETINGAMETIME(self, client, username=None):
		'''
		Get the ingame time for yourself.
		[user]

		Get the ingame time for any user.
		[mod]

		@optional.str username: The target user. Defaults to yourself.
		'''
		if username and 'mod' in client.accesslevels:
			if username in self._root.usernames: # maybe abstract in the datahandler to automatically query SQL for users not logged in.
				ingame_time = int(self._root.usernames[username].ingame_time)
				client.Send('SERVERMSG <%s> has an ingame time of %d minutes (%d hours).'%(username, ingame_time, ingame_time / 60))
			else:
				good, data = self.userdb.get_ingame_time(username)
				if good:
					ingame_time = int(data)
					client.Send('SERVERMSG <%s> has an ingame time of %d minutes (%d hours).'%(username, ingame_time, ingame_time / 60))
				else: client.Send('SERVERMSG Database returned error when retrieving ingame time for <%s> (%s)' % (username, data))
		elif not username:
			ingame_time = int(client.ingame_time)
			client.Send('SERVERMSG Your ingame time is %d minutes (%d hours).'%(ingame_time, ingame_time / 60))
		else:
			client.Send('SERVERMSG You can\'t get the ingame time of other users.')
	
	def in_REQUESTUPDATEFILE(self, client, nameAndVersion):
		'''
		Request the server to send you an update.

		@required.str name: The name of the update to request.
		@optional.str version: The version to request. If not provided or found, the default version will be used.
		'''
		nameAndVersion = nameAndVersion.lower()
		if ' ' in nameAndVersion:
			name, version = nameAndVersion.rsplit(' ',1)
		else:
			name, version = nameAndVersion, 'default'
			
		updates = self._root.updates
		if name in updates:
			update = updates[name]
			if version in updates[name]:
				client.Send('OFFERFILE %s' % update[version])
			elif 'default' in updates[name]:
				client.Send('OFFERFILE %s' % update['default'])
	
	def in_UPTIME(self, client):
		'''
		Get the server's uptime.
		'''
		client.Send('SERVERMSG Server uptime is %s.' % self._time_since(self._root.start_time))
	
	def in_GETLASTLOGINTIME(self, client, username):
		'''
		Get the last login time of target user.

		@required.str username: The target user.
		'''
		if username:
			good, data = self.userdb.get_lastlogin(username)
			if good: client.Send('SERVERMSG <%s> last logged in on %s.' % (username, time.strftime('%a, %d %b %Y %H:%M:%S GMT', time.gmtime(data))))
			else: client.Send('SERVERMSG Database returned error when retrieving last login time for <%s> (%s)' % (username, data))
	
	def in_GETREGISTRATIONDATE(self, client, username=None):
		'''
		Get the registration date of yourself.
		[user]

		Get the registration date of target user.
		[mod]

		@optional.str username: The target user. Defaults to yourself.
		'''
		if username and 'mod' in client.accesslevels:
			if username in self._root.usernames:
				reason = self._root.usernames[username].register_date
				good = True
			else: good, reason = self.userdb.get_registration_date(username)
		else:
			good = True
			username = client.username
			reason = client.register_date
		if good: client.Send('SERVERMSG <%s> registered on %s.' % (username, time.strftime('%a, %d %b %Y %H:%M:%S GMT', time.gmtime(reason))))
		else: client.Send('SERVERMSG Database returned error when retrieving registration date for <%s> (%s)' % (username, reason))
	
	def in_GETUSERID(self, client, username):
		user = self.userdb.clientFromUsername(username)
		if user:
			client.Send('SERVERMSG The ID for <%s> is %s' % (username, user.last_id))
		else:
			client.Send('SERVERMSG User not found.')
			
	def in_GENERATEUSERID(self, client, username):
		user = self._root.clientFromUsername(username)
		if user:
			client.Send('SERVERMSG The ID for <%s> requested' % (username))
			user.Send('ACQUIREUSERID')
		else:
			client.Send('SERVERMSG User not found.')
	

	def in_GETACCOUNTINFO(self, client, username):
		'''
		Get the account information for target user.
		[mod]

		@required.str username: The target user.
		'''
		good, data = self.userdb.get_account_info(username)
		if good:
			client.Send('SERVERMSG Account info for <%s>: %s' % (username, data))
		else: client.Send('SERVERMSG Database returned error when retrieving account info for <%s> (%s)' % (username, data))
	
	def in_GETACCOUNTACCESS(self, client, username):
		'''
		Get the account access bitfield for target user.
		[mod]

		@required.str username: The target user.
		'''
		good, data = self.userdb.get_account_access(username)
		if good:
			client.Send('SERVERMSG Account access for <%s>: %s' % (username, data))
		else:
			client.Send('SERVERMSG Database returned error when retrieving account access for <%s> (%s)' % (username, data))
	
	def in_FINDIP(self, client, address):
		'''
		Get all usernames associated with target IP address.

		@required.str address: The target IP address.
		'''
		results = self.userdb.find_ip(address)
		for entry in results:
			if entry.username in self._root.usernames:
				client.Send('SERVERMSG <%s> is currently bound to %s.' % (entry.username, address))
			else:
				client.Send('SERVERMSG <%s> was recently bound to %s at %s' % (entry.username, address, time.strftime('%a, %d %b %Y %H:%M:%S GMT', time.gmtime(entry.last_login / 1000))))
	
	def in_GETLASTIP(self, client, username):
		'''
		An alias for GETIP.
		'''
		return self.in_GETIP(client, username)
	
	def in_GETIP(self, client, username):
		'''
		Get the current or last IP address for target user.

		@required.str username: The target user.
		'''
		if username in self._root.usernames:
			client.Send('SERVERMSG <%s> is currently bound to %s' % (username, self._root.usernames[username].ip_address))
			return
			
		ip = self.userdb.get_ip(username)
		if ip:
			client.Send('SERVERMSG <%s> was recently bound to %s' % (username, ip))
	
	def in_RENAMEACCOUNT(self, client, newname):
		'''
		Change the name of current user.

		@required.str username: The new username to apply.
		'''
		for char in newname:
			if not char.lower() in 'abcdefghijklmnopqrstuvwzyx[]_1234567890':
				client.Send('REGISTRATIONDENIED Unicode names are currently disallowed.')
				return
		if len(newname) > 20:
			client.Send('REGISTRATIONDENIED Username is too long.')
			return
			
		user = client.username
		if user == newname:
			client.Send('SERVERMSG You already have that username.')
			return
		good, reason = self.userdb.rename_user(user, newname)
		if good:
			client.SendNow('SERVERMSG Your account has been renamed to <%s>. Reconnect with the new username (you will now be automatically disconnected).' % newname)
			client.Remove('renaming')
		else:
			client.Send('SERVERMSG Failed to rename to <%s>: %s' % (newname, reason))
	
	def in_CHANGEPASSWORD(self, client, oldpassword, newpassword):
		'''
		Change the password of current user.

		@required.str oldpassword: The previous password.
		@required.str newpassword: The new password.
		'''
		user = self.userdb.clientFromUsername(client.username)
		if user:
			if user.password == oldpassword:
				user.password = newpassword
				self.userdb.save_user(user)
				client.Send('SERVERMSG Password changed successfully to %s' % newpassword)
			else:
				client.Send('SERVERMSG Incorrect old password.')

	def in_FORGEMSG(self, client, user, msg):
		'''
		Forge a message to target user.
		Note: this is currently disabled.

		@required.str username: The target user.
		@required.str message: The raw message to send to them.
		'''
		if not 'admin' in client.accesslevels:
		    return

		if user in self._root.usernames:
		    self._root.usernames[user].Send(msg)

	def in_USERID(self, client, user_id):
		client.last_id = user_id
		self.userdb.save_user(client)
			
	def in_FORGEREVERSEMSG(self, client, user, msg):
		'''
		Forge a message from target user.
		Note: this is currently disabled.

		@required.str username: The target user.
		@required.str message: The message to forge from them.
		'''
		#client.Send('SERVERMSG Forging messages is disabled.')
		
		if not 'admin' in client.accesslevels:
		    return
		
		if user in self._root.usernames:
		    self._handle(self._root.usernames[user], msg)

	def in_GETLOBBYVERSION(self, client, username):
		'''
		Get the lobby version of target user.

		@required.str username: The target user.
		'''
		user = self.clientFromUsername(username)
		if user and 'lobby_id' in dir(user):
			client.Send('SERVERMSG <%s> is using %s'%(user.username, user.lobby_id))
	
	def in_GETSENDBUFFERSIZE(self, client, username):
		'''
		Get the size in bytes of target user's send buffer.

		@required.str username: The target user.
		'''
		if username in self._root.usernames:
			client.Send('SERVERMSG <%s> has a sendbuffer size of %s'%(username, len(self._root.usernames[username].sendbuffer)))

	def in_SETINGAMETIME(self, client, username, minutes):
		'''
		Set the ingame time of target user.

		@required.str username: The target user.
		@required.int minutes: The new ingame time to set.
		'''
		user = self.clientFromUsername(username)
		if user:
			user.ingame_time = int(minutes)
			self.userdb.save_user(user)
			client.Send('SERVERMSG Ingame time for <%s> successfully set to %s' % (username, minutes))
			self.in_GETINGAMETIME(client, username)

	def in_SETBOTMODE(self, client, username, mode):
		'''
		Set the bot flag of target user.

		@required.str username: The target user.
		@required.bool mode: The resulting bot mode.
		'''
		user = self.clientFromUsername(username)
		if user:
			bot = (mode.lower() in ('true', 'yes', '1'))
			user.bot = bot
			self.userdb.save_user(user)
			client.Send('SERVERMSG Botmode for <%s> successfully changed to %s' % (username, bot))
	
	def in_CHANGEACCOUNTPASS(self, client, username, newpass):
		'''
		Set the password for target user.
		[mod]

		@required.str username: The target user.
		@required.str password: The new password.
		'''
		user = self.userdb.clientFromUsername(username)
		if user:
			if user.access in ('mod', 'admin') and not client.access == 'admin':
				client.Send('SERVERMSG You have insufficient access to change moderator passwords.')
			else:
				user.password = newpass
				self.userdb.save_user(user)
				client.Send('SERVERMSG Password for <%s> successfully changed to %s' % (username, newpass))
	
	def in_BROADCAST(self, client, msg):
		'''
		Broadcast a message.

		@required.str message: The message to broadcast.
		'''
		self._root.broadcast('BROADCAST %s'%msg)
	
	def in_BROADCASTEX(self, client, msg):
		'''
		Broadcast a message to be shown especially by lobby clients.

		@required.str message: The message to broadcast.
		'''
		self._root.broadcast('SERVERMSGBOX %s'%msg)
	
	def in_ADMINBROADCAST(self, client, msg):
		'''
		Broadcast a message to administrative users.

		@required.str message: The message to broadcast.
		'''
		self._root.admin_broadcast(msg)
	
	def in_SETLATESTSPRINGVERSION(self, client, version):
		'''
		Set a new version of Spring as the latest.

		@required.str version: The new version to apply.
		'''
		self._root.latestspringversion = version
		client.Send('SERVERMSG Latest spring version is now set to: %s' % version)

	def in_KICKUSER(self, client, user, reason=''):
		'''
		Kick target user from the server.

		@required.str username: The target user.
		@optional.str reason: The reason to be shown.
		'''
		if reason.startswith('quiet'):
			reason = reason.split('quiet')[1].lstrip()
			quiet = True
		else: quiet = False
		if user in self._root.usernames:
			kickeduser = self._root.usernames[user]
			if reason: reason = ' (reason: %s)' % reason
			if not quiet:
				for chan in list(kickeduser.channels):
					self._root.broadcast('CHANNELMESSAGE %s <%s> kicked <%s> from the server%s'%(chan, client.username, user, reason),chan)
			client.Send('SERVERMSG You\'ve kicked <%s> from the server.' % user)
			kickeduser.SendNow('SERVERMSG You\'ve been kicked from server by <%s>%s' % (client.username, reason))
			kickeduser.Remove('Kicked from server')
	
	def in_KILLALL(self, client):
		'''
		Kick all non-admins from the server.
		'''
		for client in self._root.clients.values():
			if not client.isAdmin():
				client.Remove('all clients killed')
	
	def in_TESTLOGIN(self, client, username, password):
		'''
		Test logging in as target user.

		@required.str username: The target user.
		@required.str password: The password to try.
		'''
		user = self.userdb.clientFromUsername(username)
		if user and user.password == password:
			client.Send('TESTLOGINACCEPT %s %s' % (user.username, user.id))
		else:
			client.Send('TESTLOGINDENY')

	def in_EXIT(self, client, reason=('Exiting')):
		'''
		Disconnect from the server, with an optional reason.

		optional.str reason: The reason for exiting.
		'''
		if reason: reason = 'Quit: %s' % reason
		else: reason = 'Quit'
		client.Remove(reason)
	
	def in_BAN(self, client, username, duration, reason):
		'''
		Ban target user from the server.

		@required.str username: The target user.
		@required.float duration: The duration in days.
		@required.str reason: The reason to be shown.
		'''
		try: duration = float(duration)
		except:
			client.Send('SERVERMSG Duration must be a float (it\'s the ban duration in days)')
			return
		response = self.userdb.ban_user(username, duration, reason)
		if response: client.Send('SERVERMSG %s' % response)
	
	def in_UNBAN(self, client, username):
		'''
		Remove all bans for target user from the server.

		@required.str username: The target user.
		'''
		response = self.userdb.unban_user(username)
		if response: client.Send('SERVERMSG %s' % response)
	
	def in_BANLIST(self, client):
		'''
		Retrieve a list of all bans currently active on the server.
		'''
		for entry in self.userdb.banlist():
			client.Send('SERVERMSG %s' % entry)
	
	def in_BANIP(self, client, ip, duration, reason):
		'''
		Ban an IP address from the server.

		@required.str ip: The IP address to ban.
		@required.float duration: The duration in days.
		@required.str reason: The reason to show.
		'''
		client.Send('SERVERMSG BANIP not implemented')

	def in_PYTHON(self, client, code):
		'''
		Execute Python code directly on the server.

		@required.str code: The code to execute.
		Note: \\n and \\t will be escaped and turned directly into newlines and tabs.
		'''
		code = code.replace('\\n', '\n').replace('\\t','\t')
		try:
			exec code
		except:
			client.Send('SERVERMSG %s'%('-'*20))
			for line in traceback.format_exc().split('\n'):
				client.Send('SERVERMSG  %s'%line)
			client.Send('SERVERMSG %s'%('-'*20))
	
	def in_SETACCESS(self, client, username, access):
		'''
		Set the access level of target user.

		@required.str username: The target user.
		@required.str access: The new access to apply.
		Access levels: user, mod, admin
		'''
		user = self.clientFromUsername(username)
		if access in ('user', 'mod', 'admin'):
			if user:
				user.access = access
				self._calc_access_status(user)
				if username in self._root.usernames:
					self._root.broadcast('CLIENTSTATUS %s %s'%(username, user.status))
				self.userdb.save_user(user)

	def in_DEBUG(self, client, enabled=None):
		'''
		Enable or toggle showing debug messages from the server to the current client.
		This allows admins to see exceptions thrown by the server.

		optional.bool enabled: Set the debug mode directly.
		If omitted, debug mode will be toggled.
		'''
		if enabled == 'on':	client.debug = True
		elif enabled == 'off': client.debug = False
		else: client.debug = not client.debug
		
		client.Send('SERVERMSG Debug messages: %s' % ('on' and client.debug or 'off'))
		
	def in_RELOAD(self, client):
		'''
		Reload core parts of the server code from source. This also reparses motd, update list, and trusted proxy file.
		Do not use this for changes unless you are very confident in your ability to recover from a mistake.
		If you use this command to change code on the primary server without talking to aegis, he won't be happy.

		Parts reloaded:
		ChanServ.py
		Protocol.py
		SayHooks.py
		Telnet.py

		User databases reloaded:
		SQLUsers.py
		LanUsers.py
		'''
		if client.username != 'aegis' and client.username != 'Licho[0K]':
			client.Send('SERVERMSG talk to aegis.')
			aegis = self.clientFromUsername('[Dr]E') or self.clientFromUsername('aegis')
			if aegis:
				aegis.Send('SERVERMSG %s is trying to reload the server' % client.username)
			return

		self._root.reload()

def make_docs():
	response = []
	cmdlist = dir(Protocol)
	for cmd in cmdlist:
		if cmd.find('in_') == 0:
			docstr = getattr(Protocol, cmd).__doc__ or ''
			cmd = cmd.split('_',1)[1]
			response.append('%s - %s' % (cmd, docstr))
	return response
	
if __name__ == '__main__':
	if not os.path.exists('docs'):
		os.mkdir('docs')
	f = open('docs/protocol.txt', 'w')
	f.write('\n'.join(make_docs()) + '\n')
	f.close()
	
	print 'Protocol documentation written to docs/protocol.txt'
