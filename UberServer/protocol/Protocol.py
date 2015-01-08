#!/usr/bin/env python
# coding=utf-8

import inspect, time, re
import base64
import json
try: from hashlib import md5
except: md5 = __import__('md5').new

import traceback, sys, os
import socket
from Channel import Channel
from Battle import Battle

# see http://springrts.com/dl/LobbyProtocol/ProtocolDescription.html#MYSTATUS:client
# max. 8 ranks are possible (rank 0 isn't listed)
# rank, ingame time in hours
ranks = (5, 15, 30, 100, 300, 1000, 3000)

restricted = {
'disabled':[],
'everyone':[
	'HASH',
	'EXIT',
	'PING',
	'LISTCOMPFLAGS',
	],
'fresh':['LOGIN','REGISTER'],
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
	'FORCELEAVECHANNEL',
	'HANDICAP',
	'JOINBATTLE',
	'JOINBATTLEACCEPT',
	'JOINBATTLEDENY',
	'KICKFROMBATTLE',
	'LEAVEBATTLE',
	'MYBATTLESTATUS',
	'OPENBATTLE',
	'OPENBATTLEEX',
	'REMOVEBOT',
	'REMOVESCRIPTTAGS',
	'REMOVESTARTRECT',
	'RING',
	'SAYBATTLE',
	'SAYBATTLEEX',
	'SAYBATTLEPRIVATE',
	'SAYBATTLEPRIVATEEX',
	'SETSCRIPTTAGS',
	'UPDATEBATTLEINFO',
	'UPDATEBOT',
	#########
	# channel
	'CHANNELMESSAGE',
	'CHANNELS',
	'CHANNELTOPIC',
	'JOIN',
	'LEAVE',
	'MUTE',
	'MUTELIST',
	'SAY',
	'SAYEX',
	'SAYPRIVATE',
	'SAYPRIVATEEX',
	'SETCHANNELKEY',
	'UNMUTE',
	########
	# meta
	'CHANGEPASSWORD',
	'GETINGAMETIME',
	'MYSTATUS',
	'PORTTEST',
	'SETBATTLE'
	],
'mod':[
	'BAN',
	'BANIP',
	'UNBAN',
	'UNBANIP',
	'BANLIST',
	'CHANGEACCOUNTPASS',
	'KICKUSER',
	'FINDIP',
	'GETIP',
	'GETLASTLOGINTIME',
	'GETUSERID',
	'SETBOTMODE',
	'TESTLOGIN',
	'GETLOBBYVERSION',
	],
'admin':[
	#########
	# server
	'ADMINBROADCAST',
	'BROADCAST',
	'BROADCASTEX',
	'RELOAD',
	'CLEANUP',
	'SETLATESTSPRINGVERSION',
	#########
	# users
	'FORGEREVERSEMSG',
	'GETLASTLOGINTIME',
	'GETACCOUNTACCESS',
	'FORCEJOIN',
	'SETACCESS',
	],
}

restricted_list = []
for level in restricted:
	restricted_list += restricted[level]

ipRegex = r"^([01]?\d\d?|2[0-4]\d|25[0-5])\.([01]?\d\d?|2[0-4]\d|25[0-5])\.([01]?\d\d?|2[0-4]\d|25[0-5])\.([01]?\d\d?|2[0-4]\d|25[0-5])$"
re_ip = re.compile(ipRegex)

def validateIP(ipAddress):
	return re_ip.match(ipAddress)

def int32(x):
	val = int(x)
	if val >  2147483647 : raise OverflowError
	if val < -2147483648 : raise OverflowError
	return val

def uint32(x):
	val = int(x)
	if val > 4294967296 : raise OverflowError
	if val < 0 : raise OverflowError
	return val

flag_map = {
	'a': 'accountIDs',       # send account IDs in ADDUSER
	'b': 'battleAuth',       # JOINBATTLEREQUEST/ACCEPT/DENY
	'sp': 'scriptPassword',  # scriptPassword in JOINEDBATTLE
	'et': 'sendEmptyTopic',  # send NOCHANNELTOPIC on join if channel has no topic
	'eb': 'extendedBattles', # deprecated use cl instead: extended battle commands with support for engine/version
	'm': 'matchmaking',      # FORCEJOINBATTLE from battle hosts for matchmaking
	'cl': 'cleanupBattles',  # BATTLEOPENED / OPENBATTLE with support for engine/version
	'p':  'agreementPlain',  # AGREEMENT is plaintext
}

class Protocol:
	def __init__(self, root):
		self._root = root
		self.userdb = root.getUserDB()
		self.SayHooks = root.SayHooks
		self.dir = dir(self)
		self.agreement = root.agreement
		self.agreementplain = root.agreementplain
		self.stats = {}

	def _new(self, client):
		login_string = ' '.join((self._root.server, str(self._root.server_version), self._root.latestspringversion, str(self._root.natport), '0'))
		if self._root.redirect:
			login_string += "\nREDIRECT " + self._root.redirect

		client.Send(login_string)
		client.FlushBuffer() #FIXME: shouldn't be required

		if self._root.redirect:
			# this will make the server not accepting any commands
			# the client will be disconnected with "Connection timed out, didn't login"
			client.removing = True

	def _remove(self, client, reason='Quit'):
		if client.username and client.username in self._root.usernames:
			if client.removing: return
			if client.static: return # static clients don't disconnect
			client.removing = True
			user = client.username
			if not client == self._root.usernames[user]:
				client.removing = False # 'cause we really aren't anymore
				return

			channels = list(client.channels)
			del self._root.usernames[user]
			if client.db_id in self._root.db_ids:
				del self._root.db_ids[client.db_id]

			for chan in channels:
				channel = self._root.channels[chan]
				if user in channel.users:
					self.in_LEAVE(client, chan, reason)

			battle_id = client.current_battle
			if battle_id:
				self.in_LEAVEBATTLE(client)

			self.broadcast_RemoveUser(client)
			try:
				self.userdb.end_session(client.db_id)
			except Exception, e:
				self._root.console_write('Handler %s:%s <%s> Error writing to db in _remove: %s '%(client.handler.num, client.session_id, client.username, e.message))
		if client.session_id in self._root.clients: del self._root.clients[client.session_id]

	def _handle(self, client, msg):
		try:
			msg = msg.decode('utf-8')
			# TODO: SPADS bug is fixed, remove self.binary / uncomment in half a year or so (abma, 2014.11.04)
			self.binary = False
		except:
			#err = ":".join("{:02x}".format(ord(c)) for c in msg)
			#self.out_SERVERMSG(client, "Invalid utf-8 received, skipped message %s" %(err), True)
			self.binary = True
			#return
			
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

		if self.binary and not command in ("SAYPRIVATE"): #HACK for spads
			err = ":".join("{:02x}".format(ord(c)) for c in msg)
			self.out_SERVERMSG(client, "Invalid utf-8 received, skipped message %s" %(err), True)
			return

		access = []
		for level in client.accesslevels:
			access += restricted[level]
		if not command in restricted_list:
			self.out_SERVERMSG(client, '%s failed. Command does not exist.'%command, True)
			return False
		if not command in access:
			self.out_SERVERMSG(client, '%s failed. Insufficient rights.' % command, True)
			return False

		funcname = 'in_%s' % command
		if funcname in self.dir:
			function = getattr(self, funcname)
		else:
			self.out_SERVERMSG(client, '%s failed. Command does not exist.'%(command), True)
			return False

		# update statistics
		if not command in self.stats: self.stats[command] = 0
		self.stats[command] += 1
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
			self.out_SERVERMSG(client, '%s failed. Incorrect arguments.'%(command))
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
		#	self.out_SERVERMSG(client, '%s failed. Incorrect arguments.'%command.partition('in_')[2])
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

	def _calc_status(self, client, _status):
		status = self._dec2bin(_status, 7)
		bot, access, rank1, rank2, rank3, away, ingame = status[-7:]
		rank1, rank2, rank3 = self._dec2bin(6, 3)
		accesslist = {'user':0, 'mod':1, 'admin':1}
		access = client.access
		if access in accesslist:
			access = accesslist[access]
		else:
			access = 0
		bot = int(client.bot)
		ingame_time = int(client.ingame_time)/60 # hours

		rank = 0
		for t in ranks:
			if ingame_time >= t:
				rank += 1
		rank1 = 0
		rank2 = 0
		rank3 = 0
		try:
			rank1, rank2, rank3 = self._dec2bin(rank, 3)
		except:
			self.out_SERVERMSG(client, "invalid status: %s: %s, decoded: %s" %(_status,rank, status), True)
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
		now = time.time()
		seconds = timestamp - now
		if seconds <= 0:
			return 'forever'
		return self._time_format(seconds)

	def _time_since(self, timestamp):
		'given a past timestamp, as returned by time.time(), returns a readable relative time as a string'
		seconds = time.time() - timestamp
		return self._time_format(seconds)

	def _sendMotd(self, client):
		'send the message of the day to client'
		if not self._root.motd: return
		replace = {"{USERNAME}": str(client.username),
			"{CLIENTS}": str(len(self._root.clients)),
			"{CHANNELS}": str(len(self._root.channels)),
			"{BATTLES}": str(len(self._root.battles)),
			"{UPTIME}": str(self._time_since(self._root.start_time))}
		for line in list(self._root.motd):
			for key, value in replace.iteritems():
				line = line.replace(key, value)
			client.Send('MOTD %s' % line)
	def _checkCompat(self, client):
		missing_flags = ""
		'check the compatibility flags of client and report possible/upcoming problems to it'
		if not client.compat['sp']: # blocks protocol increase to 0.37
			client.Send("MOTD Your client doesn't support the 'sp' compatibility flag, please upgrade it!")
			client.Send("MOTD see http://springrts.com/dl/LobbyProtocol/ProtocolDescription.html#0.36")
			missing_flags += ' sp'
		if not client.compat['cl']: # cl should be used (bugfixed version of eb)
			client.Send("MOTD Your client doesn't support the 'cl' compatibility flag, please upgrade it!")
			client.Send("MOTD see http://springrts.com/dl/LobbyProtocol/ProtocolDescription.html#0.37")
			missing_flags += ' cl'
		if not client.compat['p']:
			client.Send("MOTD Your client doesn't support the 'p' compatibility flag, please upgrade it!")
			client.Send("MOTD see htpp://springrts.com/dl/LobbyProtocol/ProtocolDescription.html#0.37")
			missing_flags += ' p'
		if client.compat['eb']:
			client.Send("MOTD Your client uses the 'eb' compatibility flag, which is replaced by 'cl' please update it!")
			client.Send("MOTD see http://springrts.com/dl/LobbyProtocol/ProtocolDescription.html#0.37")
		if len(missing_flags) > 0:
			self._root.console_write('Handler %s:%s <%s> client "%s" missing compat flags:%s'%(client.handler.num, client.session_id, client.username, client.lobby_id, missing_flags))

	def _validPasswordSyntax(self, password):
		'checks if a password is correctly encoded base64(md5())'
		if not password:
			return False, 'Empty passwords are not allowed.'
		try:
			md5str = base64.b64decode(password)
		except Exception, e:
			return False, "Invalid base64 encoding"
		if len(md5str) != 16:
			return False, "Invalid md5 sum"
		return True, ""

	def _validUsernameSyntax(self, username):
		'checks if usernames syntax is correct / doesn''t contain invalid chars'
		if not username:
			return False, 'Invalid username.'
		for char in username:
			if not char.lower() in 'abcdefghijklmnopqrstuvwzyx[]_1234567890':
				return False, 'Only ASCII chars, [], _, 0-9 are allowed in usernames.'
		if len(username) > 20:
			return False, 'Username is too long, max is 20 chars.'
		return True, ""

	def _validChannelSyntax(self, channel):
		'checks if usernames syntax is correct / doesn''t contain invalid chars'
		for char in channel:
			if not char.lower() in 'abcdefghijklmnopqrstuvwzyx[]_1234567890':
				return False, 'Only ASCII chars, [], _, 0-9 are allowed in channel names.'
		if len(channel) > 20:
			return False, 'Channelname is too long, max is 20 chars.'
		return True, ""

	def _parseTags(self, tagstring):
		'parses tags to a dict, for example user=bla\tcolor=123'
		tags = {}
		for tagpair in tagstring.split('\t'):
			if not '=' in tagpair:
				continue # this fails; tag isn't split by anything
			(tag, value) = tagpair.split('=',1)
			tags.update({tag:value})
		return tags

	def _canForceBattle(self, client, username = None):
		' returns true when client can force sth. to a battle / username in current battle (=client is host & username is in battle)'
		battle_id = client.current_battle
		if not battle_id in self._root.battles:
			return False
		battle = self._root.battles[battle_id]
		if not client.username == battle.host:
			return False
		if username == None:
			return True
		if username in battle.users:
			return True
		return False

	def _informErrors(self, client):
		if client.lobby_id in ("SpringLobby 0.188 (win x32)", "SpringLobby 0.200 (win x32)"):
			client.Send("SAYPRIVATE ChanServ The autoupdater of SpringLobby 0.188 is broken, please manually update: http://springrts.com/phpbb/viewtopic.php?f=64&t=31224")

	def clientFromID(self, db_id, fromdb = False):
		'given a user database id, returns a client object from memory or the database'
		user = self._root.clientFromID(db_id)
		if user: return user
		if not fromdb: return None
		return self.userdb.clientFromID(db_id)

	def clientFromUsername(self, username, fromdb = False):
		'given a username, returns a client object from memory or the database'
		client = self._root.clientFromUsername(username)
		if fromdb and not client:
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

	# the sourceClient is only sent for SAY*, and RING commands
	def broadcast_SendBattle(self, battle, data, sourceClient=None):
		'queues the protocol for sending text in a battle - experiment in loose thread-safety'
		users = list(battle.users)
		for username in users:
			client = self.clientFromUsername(username)
			if client:
				client.SendBattle(battle, data)

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
		if client.compat['a']: #accountIDs
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
		if client.compat['cl']: #supports cleanupBattles
			client.Send('BATTLEOPENED %(id)s %(type)s %(natType)s %(host)s %(ip)s %(port)s %(maxplayers)s %(passworded)s %(rank)s %(maphash)s %(engine)s\t%(version)s\t%(map)s\t%(title)s\t%(modname)s' % ubattle)
		elif client.compat['eb']: #FIXME: this shouldn't be used at all, supports extendedBattles
			if (' ' in ubattle['engine']) or (' ' in  ubattle['version']):
				ubattle['title'] = 'Incompatible (%(engine)s %(version)s) %(title)s' % ubattle
			ubattle['engine'] = ubattle['engine'].split(" ")[0]
			ubattle['version'] = ubattle['version'].split(" ")[0]
			client.Send('BATTLEOPENEDEX %(id)s %(type)s %(natType)s %(host)s %(ip)s %(port)s %(maxplayers)s %(passworded)s %(rank)s %(maphash)s %(engine)s %(version)s %(map)s\t%(title)s\t%(modname)s' % ubattle)
		else:
			# give client without version support a hint, that this battle is incompatible to his version
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
		good, reason = self._validUsernameSyntax(username)
		if not good:
			client.Send("REGISTRATIONDENIED %s" % (reason))
			return

		if client.hashpw:
			m = md5(password)
			password = base64.b64encode(m.digest())

		good, reason = self._validPasswordSyntax(password)
		if not good:
			client.Send("REGISTRATIONDENIED %s" % (reason))
			return

		good, reason = self.userdb.register_user(username, password, client.ip_address, client.country_code)
		if good:
			self._root.console_write('Handler %s:%s Successfully registered user <%s>.'%(client.handler.num, client.session_id, username))
			client.Send('REGISTRATIONACCEPTED')
			self.clientFromUsername(username, True).access = 'agreement'
		else:
			self._root.console_write('Handler %s:%s Registration failed for user <%s>.'%(client.handler.num, client.session_id, username))
			client.Send('REGISTRATIONDENIED %s'%reason)

	def in_HASH(self, client):
		'''
		After this command has been used, the password argument to LOGIN will be automatically hashed with md5+base64.
		'''
		client.hashpw = not client.hashpw
		if client.hashpw:
			self.out_SERVERMSG(client, 'Your password will be hashed for you when you login.')
		else:
			self.out_SERVERMSG(client, 'Auto-Password hashing disabled.')

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

		if username in self._root.usernames: # FIXME: is checked first because of a springie bug
			self.out_DENIED(client, username, 'Already logged in.', False)
			return

		if client.failed_logins > 2:
			self.out_DENIED(client, username, "to many failed logins")
			return
		ok, reason = self._validUsernameSyntax(username)
		if not ok:
			self.out_DENIED(client, username, reason)
			return

		try: int32(cpu)
		except: cpu = '0'
		user_id = 0

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

				unsupported = ""
				for flag in flags:
					client.compat[flag] = True
					if not flag in flag_map:
						unsupported +=  " " +flag
				if len(unsupported)>0:
					self.out_SERVERMSG(client, 'Unsupported/unknown compatibility flag(s) in LOGIN: %s' % (unsupported), True)
			try:
				client.last_id = uint32(user_id)
			except:
				self.out_SERVERMSG(client, 'Invalid userID specified: %s' % (user_id), True)
		else:
			lobby_id = sentence_args

		if not password:
			self.out_DENIED(client, username, "Empty password")
			return

		if client.hashpw:
			origpassword = password # store for later use when ghosting
			m = md5(password)
			password = base64.b64encode(m.digest())

		ok, reason = self._validPasswordSyntax(password)
		if not ok:
			self.out_DENIED(client, username, reason)
			return

		try:
			good, reason = self.userdb.login_user(username, password, client.ip_address, lobby_id, user_id, cpu, local_ip, client.country_code)
		except Exception, e:
			self._root.console_write('Handler %s:%s <%s> Error reading from db in in_LOGIN: %s '%(client.handler.num, client.session_id, client.username, e.message))
			good = False
			reason = "db error"
		if not good:
			self.out_DENIED(client, username, reason)
			return
		username = reason.username
		client.logged_in = True
		client.access = reason.access
		self._calc_access(client)
		client.username = reason.username
		client.password = reason.password
		client.lobby_id = reason.lobby_id
		client.bot = reason.bot
		client.cpu = cpu

		client.local_ip = None
		if local_ip.startswith('127.') or not validateIP(local_ip):
			client.local_ip = client.ip_address
		else:
			client.local_ip = local_ip

		client.ingame_time = reason.ingame_time

		if reason.id == None:
			client.db_id = client.session_id
		else:
			client.db_id = reason.id
		if client.ip_address in self._root.trusted_proxies:
			client.setFlagByIP(local_ip, False)

		if client.access == 'agreement':
			self._root.console_write('Handler %s:%s Sent user <%s> the terms of service on session.'%(client.handler.num, client.session_id, username))
			if client.compat['p']:
				agreement = self.agreementplain
			else:
				agreement = self.agreement
			for line in agreement:
				client.Send("AGREEMENT %s" %(line))
			client.Send('AGREEMENTEND')
			return

		# needs to be checked directly before it is added, to make it somelike atomic as we have no locking over threads
		if username in self._root.usernames:
			self.out_DENIED(client, username, 'Already logged in.', False)
			return

		self._root.console_write('Handler %s:%s Successfully logged in user <%s> %s.'%(client.handler.num, client.session_id, username, client.access))
		self._root.db_ids[client.db_id] = client
		self._root.usernames[username] = client
		client.status = self._calc_status(client, 0)


		client.Send('ACCEPTED %s'%username)

		self._sendMotd(client)
		self._checkCompat(client)
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

		self.broadcast_SendUser(client, 'CLIENTSTATUS %s %s'%(username, client.status))
		for user in usernames:
			if user == username: continue # potential problem spot, might need to check to make sure username is still in user db
			client.SendUser(user, 'CLIENTSTATUS %s %s'%(user, usernames[user].status))

		client.Send('LOGININFOEND')
		self._informErrors(client)

	def in_CONFIRMAGREEMENT(self, client):
		'Confirm the terms of service as shown with the AGREEMENT commands. Users must accept the terms of service to use their account.'
		if client.access == 'agreement':
			client.access = 'user'
			self.userdb.save_user(client)
			client.access = 'fresh'
			self._calc_access_status(client)

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
					self._root.broadcast('SAID %s %s %s' % (chan, client.username, msg), chan, [], client)

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
					self._root.broadcast('SAIDEX %s %s %s' % (chan, client.username, msg), chan, [], client)

	def in_SAYPRIVATE(self, client, user, msg):
		'''
		Send a message in private to another user.

		@required.str user: The target user.
		@required.str message: The message to send.
		'''
		if not msg: return
		receiver = self.clientFromUsername(user)
		if receiver:
			if not self.binary:
				msg = self.SayHooks.hook_SAYPRIVATE(self, client, user, msg) # comment out to remove sayhook
				if not msg or not msg.strip(): return
			client.Send('SAYPRIVATE %s %s'%(user, msg), self.binary)
			receiver.Send('SAIDPRIVATE %s %s' %(client.username, msg), self.binary)

	def in_SAYPRIVATEEX(self, client, user, msg):
		'''
		Send an action in private to another user.

		@required.str user: The target user.
		@required.str message: The action to send.
		'''
		if not msg: return
		receiver = self.clientFromUsername(user)
		if receiver:
			msg = self.SayHooks.hook_SAYPRIVATE(self, client, user, msg) # comment out to remove sayhook
			if not msg or not msg.strip(): return
			client.Send('SAYPRIVATEEX %s %s'%(user, msg))
			receiver.Send('SAIDPRIVATEEX %s %s'%(client.username, msg))

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
				message = self._time_until(m['expires']) + (' by IP.' if m['ip'] else '.')
				user = self.clientFromID(user)
				if user:
					client.Send('MUTELIST %s, %s' % (user.username, message))
			client.Send('MUTELISTEND')


	def in_FORCEJOIN(self, client, user, chan, key=None):
		'''
		Force a user to join a channel.

		@required.str username: user to send to
		@required.str channel: target channel
		@optional.str password: channel password
		'''
		ok, reason = self._validChannelSyntax(chan)
		if not ok:
			self.out_SERVERMSG(client, '%s' % reason)
			return

		if chan in self._root.channels:
			channel = self._root.channels[chan]
			if user in channel.users:
				self.out_SERVERMSG(client, 'FORCEJOIN failed: %s Already in channel!' % chan)
				return

		if user in self._root.usernames:
			self._handle(self._root.usernames[user], "JOIN %s %s" % (chan, key))
		else:
			self.out_SERVERMSG(client, '%s user not found' % user)

	def in_JOIN(self, client, chan, key=None):
		'''
		Attempt to join target channel.

		@required.str channel: The target channel.
		@optional.str password: The password to use for joining if channel is locked.
		'''
		ok, reason = self._validChannelSyntax(chan)
		if not ok:
			client.Send('JOINFAILED %s' % reason)
			return

		user = client.username
		chan = chan.lstrip('#')

		# FIXME: unhardcode this
		if client.bot and chan == "newbies" and client.username != "ChanServ":
			client.Send('JOINFAILED %s No bots allowed in #newbies!' %(chan))
			return

		if not chan: return
		if not chan in self._root.channels:
			channel = self._new_channel(chan)
			self._root.channels[chan] = channel
		else:
			channel = self._root.channels[chan]
		if user in channel.users:
			return
		if not channel.isFounder(client):
			if channel.key and not channel.key in (key, None, '*', ''):
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
		self._root.broadcast('JOINED %s %s' % (chan, user), chan)
		channel.users.append(user)
		client.Send('CLIENTS %s %s'%(chan, ' '.join(channel.users)))

		topic = channel.topic
		if topic:
			if client.compat['et']:
				topictime = int(topic['time'])
			else:
				topictime = int(topic['time'])*1000
			try:
				top = topic['text'].decode("utf-8")
			except:
				top = "Invalid utf-8 encoding"
				self._root.console_write("%s for channel topic: %s" %(top, chan))
			client.Send('CHANNELTOPIC %s %s %s %s'%(chan, topic['user'], topictime, top))
		elif client.compat['et']: # supports sendEmptyTopic
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

	def in_LEAVE(self, client, chan, reason=None):
		'''
		Leave target channel.

		@required.str channel: The target channel.
		'''
		if chan in self._root.channels:
			channel = self._root.channels[chan]
			channel.removeUser(client, reason)
			if len(self._root.channels[chan].users) == 0:
				del self._root.channels[chan]

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
		@required.sentence.str engine: The engine name, lowercase, with no spaces.
		@required.sentence.str version: The engine version.
		@required.sentence.str mapName: The map name.
		@required.sentence.str title: The battle's title.
		@required.sentence.str modName: The mod name.
		'''
		if client.current_battle in self._root.battles:
			self.in_LEAVEBATTLE(client)

		engine = 'spring'
		version = self._root.latestspringversion
		if client.compat['cl']: #supports cleanupBattles
			if sentence_args.count('\t') > 3:
				engine, version, map, title, modname = sentence_args.split('\t', 4)
				if not engine:
					self.out_OPENBATTLEFAILED(client, 'No engine specified.')
					return False
				if not version:
					self.out_OPENBATTLEFAILED(client, 'No engine version specified.')
					return False
				if not map:
					self.out_OPENBATTLEFAILED(client, 'No map name specified.')
					return False
				if not title:
					self.out_OPENBATTLEFAILED(client, 'No title name specified.')
					return False
				if not modname:
					self.out_OPENBATTLEFAILED(client, 'No game name specified.')
					return False
			else:
				self.out_OPENBATTLEFAILED(client, 'To few arguments.')
				return False
		else:
			if sentence_args.count('\t') > 1:
				map, title, modname = sentence_args.split('\t',2)

				if not modname:
					self.out_OPENBATTLEFAILED(client, 'No game name specified.')
					return False
				if not map:
					self.out_OPENBATTLEFAILED(client, 'No map name specified.')
					return False
			else:
				return False

		battle_id = str(self._root.nextbattle)
		self._root.nextbattle += 1

		if password == '*':
			passworded = 0
		else:
			passworded = 1

		try:
			int(battle_id), int(type), int(natType), int(passworded), int(port), int32(maphash), int32(hashcode)
		except Exception, e:
			self.out_OPENBATTLEFAILED(client, 'Invalid argument type, send this to your lobby dev: id=%s type=%s natType=%s passworded=%s port=%s maphash=%s gamehash=%s - %s' %
						(battle_id, type, natType, passworded, port, maphash, hashcode, e.replace("\n", "")))
			return False

		client.current_battle = battle_id

		host = client.username
		battle = Battle(
						root=self._root, id=battle_id, type=type, natType=int(natType),
						password=password, port=port, maxplayers=maxplayers, hashcode=hashcode,
						rank=rank, maphash=maphash, map=map, title=title, modname=modname,
						passworded=passworded, host=host, users=[host],
						engine=engine, version=version
					)
		ubattle = battle.copy()


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
				self.out_OPENBATTLEFAILED(client, 'No game name specified.')
				return
			if not map:
				self.out_OPENBATTLEFAILED(client, 'No map name specified.')
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
						engine=engine, version=version
					)
		ubattle = battle.copy()

		try:
			int(battle_id), int(type), int(natType), int(passworded), int(port), int32(maphash), int32(hashcode)
		except:
			client.current_battle = None
			self.out_OPENBATTLEFAILED(client, 'Invalid argument type, send this to your lobby dev:'
						'id=%(id)s type=%(type)s natType=%(natType)s passworded=%(passworded)s port=%(port)s maphash=%(maphash)s gamehash=%(hashcode)s' % (ubattle))
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
			self.broadcast_SendBattle(battle, 'SAIDBATTLE %s %s' % (user, msg), client)

	def in_SAYBATTLEEX(self, client, msg):
		'''
		Send an action to all users in your current battle.

		@required.str message: The action to send.
		'''
		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			self.broadcast_SendBattle(battle, 'SAIDBATTLEEX %s %s' % (client.username, msg), client)

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

	def in_FORCEJOINBATTLE(self, client, username, target_battle, password=None):
		'''
		Instruct a user in your battle to join another.
		[host]

		@required.str username: The target user.
		@required.int battle_id: The destination battle.
		@optional.str password: The battle's password, if required.
		'''

		if not username in self._root.usernames:
			client.Send("FORCEJOINBATTLEFAILED user %s not found!" %(username))
			return

		user = self.clientFromUsername(username)
		battle_id = user.current_battle

		battlehost = False
		if self._canForceBattle(client, username):
			battlehost = True
		elif not 'mod' in client.accesslevels:
			client.Send('FORCEJOINBATTLEFAILED You are not allowed to force this user into battle.')
			return

		user = self._root.usernames[username]
		if not user.compat['m']:
			client.Send('FORCEJOINBATTLEFAILED This user does not subscribe to matchmaking.')
			return

		if not target_battle in self._root.battles:
			client.Send('FORCEJOINBATTLEFAILED Target battle does not exist.')
			return

		target = self._root.battles[target_battle]
		if target.passworded:
			if password == target.password:
				user.Send('FORCEJOINBATTLE %s %s' % (target_battle, password))
			else:
				client.Send('FORCEJOINBATTLEFAILED Incorrect password for target battle.')
			return

		user.Send('FORCEJOINBATTLE %s' % (target_battle))

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
				user = self.clientFromUsername(username)
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
				user = self.clientFromUsername(username)
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
				host = self.clientFromUsername(battle.host)
				if battle.passworded == 1 and not battle.password == password:
					if not (host.compat['b'] and username in battle.authed_users): # supports battleAuth
						client.Send('JOINBATTLEFAILED Incorrect password.')
						return
				if battle.locked:
					client.Send('JOINBATTLEFAILED Battle is locked.')
					return
				if username in host.battle_bans: # TODO: make this depend on db_id instead
					client.Send('JOINBATTLEFAILED <%s> has banned you from their battles.' % battle.host)
					return
				if host.compat['b'] and not username in battle.authed_users: # supports battleAuth
					battle.pending_users.add(username)
					if client.ip_address in self._root.trusted_proxies:
						client_ip = client.local_ip
					else:
						client_ip = client.ip_address
					host.Send('JOINBATTLEREQUEST %s %s' % (username, client_ip))
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
				if host.compat['sp'] and scriptPassword: # supports scriptPassword
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

		if not self._canForceBattle(client):
			self.out_FAILED(client, "SETSCRIPTTAGS", "You are not allowed to change settings as client in a game!", True)
			return

		setscripttags = self._parseTags(scripttags)
		scripttags = []
		for tag in setscripttags:
			scripttags.append('%s=%s'%(tag.lower(), setscripttags[tag]))
		if not scripttags:
			return
		self._root.battles[client.current_battle].script_tags.update(setscripttags)
		self._root.broadcast_battle('SETSCRIPTTAGS %s'%'\t'.join(scripttags), client.current_battle)

	def in_REMOVESCRIPTTAGS(self, client, tags):
		'''
		Remove script tags and send an update to all clients in your battle.

		@required.str tags: A space-separated list of tags.
		'''
		if not self._canForceBattle(client):
			return

		battle = self._root.battles[client.current_battle]
		rem = set()
		for tag in set(tags.split(' ')):
			try:
				# this means we only broadcast removed tags if they existed
				del battle.script_tags[tag]
				rem.add(tag)
			except KeyError:
				pass
		if not rem:
			return
		self._root.broadcast_battle('REMOVESCRIPTTAGS %s'%' '.join(rem), client.current_battle)

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

	def in_MYBATTLESTATUS(self, client, _battlestatus, _myteamcolor):
		'''
		Set your status in a battle.

		@required.int status: The status to set, formatted as an awesome bitfield.
		@required.sint teamColor: Teamcolor to set. Format is hex 0xBBGGRR represented as decimal.
		'''
		try:
			battlestatus = int32(_battlestatus)
		except:
			self.out_SERVERMSG(client, 'MYBATTLESTATUS failed - invalid status: %s.' % (_battlestatus), True)
			return

		if battlestatus < 1:
			battlestatus = battlestatus + 2147483648
			self._root.console_write('MYBATTLESTATUS failed - invalid status is below 1: %s.'% (_battlestatus))

		try:
			myteamcolor = int32(_myteamcolor)
		except:
			self.out_SERVERMSG(client, 'MYBATTLESTATUS failed - invalid teamcolor: %s.'%myteamcolor, True)
			return

		battle_id = client.current_battle
		if battle_id in self._root.battles:
			battle = self._root.battles[battle_id]
			spectating = (client.battlestatus['mode'] == '0')

			clients = (self.clientFromUsername(name) for name in battle.users)
			spectators = len([user for user in clients if user and (user.battlestatus['mode'] == '0')])

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
				try:
					maphash = int32(maphash)
				except:
					self.out_SERVERMSG(client, "UPDATEBATTLEINFO failed - Invalid map hash send: %s %s " %(str(mapname),str(maphash)), True)
					maphash = 0
					return
				old = battle.copy()
				updated = {'id':battle_id, 'locked':int(locked), 'maphash':maphash, 'map':mapname}
				battle.update(**updated)

				oldstr = 'UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % old
				newstr = 'UPDATEBATTLEINFO %(id)s %(spectators)i %(locked)i %(maphash)s %(map)s' % battle.copy()
				if oldstr != newstr:
					self._root.broadcast(newstr)

	def in_MYSTATUS(self, client, _status):
		'''
		Set your client status, to be relayed to all other clients.

		@required.int status: A bitfield of your status. The server forces a few values itself, as well.
		'''
		try:
			status = int32(_status)
		except:
			self.out_SERVERMSG(client, 'MYSTATUS failed - invalid status %s'%(_status), True)
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
			self.out_SERVERMSG(client, 'No channels are currently visible (they must be registered and unlocked).')
			return

		for channel in channels:
			topic = channel.topic
			if topic:
				try:
					top = topic['text'].decode("utf-8")
				except:
					top = "Invalid utf-8"
			client.Send('CHANNEL %s %d %s'% (channel.name, len(channel.users), top))
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
		if not chan in self._root.channels:
			self.out_SERVERMSG(client, 'channel <%s> does not exist!' % (chan))
			return
		channel = self._root.channels[chan]
		if not (channel.isOp(client) or 'mod' in client.accesslevels):
			self.out_SERVERMSG(client, 'access denied')
		target = self.clientFromUsername(username)
		if target and username in channel.users:
			channel.kickUser(client, target, reason)
			self.out_SERVERMSG(client, '<%s> kicked from channel #%s' % (username, chan))
		else:
			self.out_SERVERMSG(client, '<%s> not in channel #%s' % (username, chan))

	def in_RING(self, client, username):
		'''
		Send target user a ringing notification, normally used for idle users in battle.
		[host]

		@required.str username: The target user.
		'''
		user = self.clientFromUsername(username)

		if not user: return
		if not 'mod' in client.accesslevels:
			battle_id = client.current_battle
			if battle_id and battle_id in self._root.battles:
				battle = self._root.battles[battle_id]
				if not battle.host in (client.username, username):
					return
				if not username in battle.users:
					return
			else:
				return

		user.Send('RING %s' % (client.username))


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
		if not self._canForceBattle(client):
			return
		allyno = int32(allyno)
		battle = self._root.battles[client.current_battle]
		rect = {'left':left, 'top':top, 'right':right, 'bottom':bottom}
		battle.startrects[allyno] = rect
		self._root.broadcast_battle('ADDSTARTRECT %s' % (allyno)+' %(left)s %(top)s %(right)s %(bottom)s' %(rect), client.current_battle, [client.username])

	def in_REMOVESTARTRECT(self, client, allyno):
		'''
		Remove a start rectangle for an ally team.
		[host]

		@required.int allyno: The ally number for the rectangle.
		'''
		if not self._canForceBattle(client):
			return
		allyno = int32(allyno)
		battle = self._root.battles[client.current_battle]
		try:
			del battle.startrects[allyno]
		except:
			self.out_SERVERMSG(client, 'invalid rect removed: %d' % (allyno), True)
			pass
		self._root.broadcast_battle('REMOVESTARTRECT %s' % allyno, client.current_battle, [client.username])

	def in_DISABLEUNITS(self, client, units):
		'''
		Add a list of units to disable.
		[host]

		@required.str units: A string-separated list of unit names to disable.
		'''
		if not self._canForceBattle(client):
			return
		units = units.split(' ')
		disabled_units = []
		battle = self._root.battles[client.current_battle]
		for unit in units:
			if not unit in battle.disabled_units:
				battle.disabled_units.append(unit)
				disabled_units.append(unit)
		if disabled_units:
			disabled_units = ' '.join(disabled_units)
			self._root.broadcast_battle('DISABLEUNITS %s'%disabled_units, client.current_battle, client.username)

	def in_ENABLEUNITS(self, client, units):
		'''
		Remove units from the disabled unit list.
		[host]

		@required.str units: A string-separated list of unit names to enable.
		'''
		if not self._canForceBattle(client, username):
			return
		units = units.split(' ')
		enabled_units = []
		battle = self._root.battles[client.current_battle]
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
		if not self._canForceBattle(client):
			return
		battle = self._root.battles[client.current_battle]
		battle.disabled_units = []
		self._root.broadcast_battle('ENABLEALLUNITS', client.current_battle, client.username)

	def in_HANDICAP(self, client, username, value):
		'''
		Change the handicap value for a player.
		[host]

		@required.str username: The player to handicap.
		@required.int handicap: The percentage of handicap to give (1-100).
		'''
		if not self._canForceBattle(client, username):
			return

		if not value.isdigit() or not int(value) in range(0, 101):
			return

		client = self._root.usernames[username]
		client.battlestatus['handicap'] = self._dec2bin(value, 7)
		self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), client.current_battle)

	def in_KICKFROMBATTLE(self, client, username):
		'''
		Kick a player from their battle.
		[host]

		@required.str username: The player to kick.
		'''
		if not self._canForceBattle(client, username):
			return
		kickuser = self._root.usernames[username]
		kickuser.Send('FORCEQUITBATTLE')
		battle = self._root.battles[client.current_battle]
		if username == battle.host:
			self.broadcast_RemoveBattle(battle)
			del self._root.battles[client.current_battle]
		else:
			self.in_LEAVEBATTLE(kickuser)


	def in_FORCETEAMNO(self, client, username, teamno):
		'''
		Force target player's team number.
		[host]

		@required.str username: The target player.
		@required.int teamno: The team to assign them.
		'''
		if not self._canForceBattle(client, username):
			return
		client = self._root.usernames[username]
		client.battlestatus['id'] = self._dec2bin(teamno, 4)
		self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), client.current_battle)

	def in_FORCEALLYNO(self, client, username, allyno):
		'''
		Force target player's ally team number.
		[host]

		@required.str username: The target player.
		@required.int teamno: The ally team to assign them.
		'''
		if not self._canForceBattle(client, username):
			return
		client = self._root.usernames[username]
		client.battlestatus['ally'] = self._dec2bin(allyno, 4)
		self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), client.current_battle)

	def in_FORCETEAMCOLOR(self, client, username, teamcolor):
		'''
		Force target player's team color.
		[host]

		@required.str username: The target player.
		@required.sint teamcolor: The color to assign, represented with hex 0xBBGGRR as a signed integer.
		'''
		if not self._canForceBattle(client, username):
			return
		client = self._root.usernames[username]
		client.teamcolor = teamcolor
		self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), client.current_battle)

	def in_FORCESPECTATORMODE(self, client, username):
		'''
		Force target player to become a spectator.
		[host]

		@required.str username: The target player.
		'''
		if not self._canForceBattle(client, username):
			return

		client = self._root.usernames[username]
		if client.battlestatus['mode'] == '1':
			battle = self._root.battles[client.current_battle]
			battle.spectators += 1
			client.battlestatus['mode'] = '0'
			self._root.broadcast_battle('CLIENTBATTLESTATUS %s %s %s'%(username, self._calc_battlestatus(client), client.teamcolor), client.current_battle)
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
				self.out_SERVERMSG(client, '<%s> has an ingame time of %d minutes (%d hours).'%(username, ingame_time, ingame_time / 60))
			else:
				good, data = self.userdb.get_ingame_time(username)
				if good:
					ingame_time = int(data)
					self.out_SERVERMSG(client, '<%s> has an ingame time of %d minutes (%d hours).'%(username, ingame_time, ingame_time / 60))
				else: self.out_SERVERMSG(client, 'Database returned error when retrieving ingame time for <%s> (%s)' % (username, data))
		elif not username:
			ingame_time = int(client.ingame_time)
			self.out_SERVERMSG(client, 'Your ingame time is %d minutes (%d hours).'%(ingame_time, ingame_time / 60))
		else:
			self.out_SERVERMSG(client, 'You can\'t get the ingame time of other users.')

	def in_GETLASTLOGINTIME(self, client, username):
		'''
		Get the last login time of target user.

		@required.str username: The target user.
		'''
		if username:
			good, data = self.userdb.get_lastlogin(username)
			if good: self.out_SERVERMSG(client, '<%s> last logged in on %s.' % (username, data.isoformat()))
			else: self.out_SERVERMSG(client, 'Database returned error when retrieving last login time for <%s> (%s)' % (username, data))

	
	def in_GETUSERID(self, client, username):
		user = self.clientFromUsername(username, True)
		if user:
			self.out_SERVERMSG(client, 'The ID for <%s> is %s' % (username, user.last_id))
		else:
			self.out_SERVERMSG(client, 'User not found.')

	def in_GETACCOUNTACCESS(self, client, username):
		'''
		Get the account access bitfield for target user.
		[mod]

		@required.str username: The target user.
		'''
		good, data = self.userdb.get_account_access(username)
		if good:
			self.out_SERVERMSG(client, 'Account access for <%s>: %s' % (username, data))
		else:
			self.out_SERVERMSG(client, 'Database returned error when retrieving account access for <%s> (%s)' % (username, data))

	def in_FINDIP(self, client, address):
		'''
		Get all usernames associated with target IP address.

		@required.str address: The target IP address.
		'''
		results = self.userdb.find_ip(address)
		for entry in results:
			if entry.username in self._root.usernames:
				self.out_SERVERMSG(client, '<%s> is currently bound to %s.' % (entry.username, address))
			else:
				if entry.last_login:
					lastlogin = entry.last_login.isoformat()
				else:
					lastlogin = "Unknown"
				self.out_SERVERMSG(client, '<%s> was recently bound to %s at %s' % (entry.username, address, lastlogin))

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
			self.out_SERVERMSG(client, '<%s> is currently bound to %s' % (username, self._root.usernames[username].ip_address))
			return

		ip = self.userdb.get_ip(username)
		if ip:
			self.out_SERVERMSG(client, '<%s> was recently bound to %s' % (username, ip))


	def in_CHANGEPASSWORD(self, client, oldpassword, newpassword):
		'''
		Change the password of current user.

		@required.str oldpassword: The previous password.
		@required.str newpassword: The new password.
		'''
		good, reason = self._validPasswordSyntax(newpassword)
		if not good:
			self.out_SERVERMSG(client, '%s' % (reason))
			return
		user = self.clientFromUsername(client.username, True)
		if user:
			if user.password == oldpassword:
				user.password = newpassword
				self.userdb.save_user(user)
				self.out_SERVERMSG(client, 'Password changed successfully! It will be used at the next login!')
			else:
				self.out_SERVERMSG(client, 'Incorrect old password.')

	def in_FORGEREVERSEMSG(self, client, user, msg):
		'''
		deprecated, TODO: will be removed on 7.12.2014:
			https://github.com/ZeroK-RTS/Zero-K-Infrastructure/issues/19
		'''
		if client.compat['cl']:
			self.out_SERVERMSG(client, 'Forging messages is deprecated and will be removed on 7.12.2014.')
			return

		if not (msg and msg.split(' ')[0] in ("LEAVEBATTLE", "JOINBATTLE")):
			self.out_SERVERMSG(client, "Invalid call to FORGEREVERSEMSG, this command is deprecated and will be removed on 7.12.2014, don't use it: %s" %(msg), True)
			return

		if not client.username == "Nightwatch":
			self.out_SERVERMSG(client, "Forging messages is deprecated, only exception for Nightwatch exists and will be removed on 7.12.2014", True)
			return

		if user in self._root.usernames:
			self._root.console_write('FORGEREVERSEMSG %s %s %s' %(client.username, user, msg))
			self._handle(self._root.usernames[user], msg)

	def in_GETLOBBYVERSION(self, client, username):
		'''
		Get the lobby version of target user.

		@required.str username: The target user.
		'''
		user = self.clientFromUsername(username, True)
		if user and 'lobby_id' in dir(user):
			self.out_SERVERMSG(client, '<%s> is using %s'%(user.username, user.lobby_id))

	def in_SETBOTMODE(self, client, username, mode):
		'''
		Set the bot flag of target user.

		@required.str username: The target user.
		@required.bool mode: The resulting bot mode.
		'''
		user = self.clientFromUsername(username, True)
		if user:
			bot = (mode.lower() in ('true', 'yes', '1'))
			user.bot = bot
			self.userdb.save_user(user)
			self.out_SERVERMSG(client, 'Botmode for <%s> successfully changed to %s' % (username, bot))

	def in_CHANGEACCOUNTPASS(self, client, username, newpass):
		'''
		Set the password for target user.
		[mod]

		@required.str username: The target user.
		@required.str password: The new password.
		'''
		user = self.clientFromUsername(username, True)
		if user:
			if user.access in ('mod', 'admin') and not client.access == 'admin':
				self.out_SERVERMSG(client, 'You have insufficient access to change moderator passwords.')
			else:
				res, reason = self._validPasswordSyntax(newpass)
				if not res:
					self.out_SERVERMSG(client, "invalid password specified: %s" %(reason))
					return
				self._root.console_write('Handler %s: <%s> changed password of <%s>.' % (client.handler.num, client.username, username))
				user.password = newpass
				self.userdb.save_user(user)
				self.out_SERVERMSG(client, 'Password for <%s> successfully changed to %s' % (username, newpass))

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
		self.out_SERVERMSG(client, 'Latest spring version is now set to: %s' % version)

	def in_KICKUSER(self, client, user, reason=''):
		'''
		Kick target user from the server.

		@required.str username: The target user.
		@optional.str reason: The reason to be shown.
		'''
		if user in self._root.usernames:
			kickeduser = self._root.usernames[user]
			if reason: reason = ' (reason: %s)' % reason
			for chan in list(kickeduser.channels):
				self._root.broadcast('CHANNELMESSAGE %s <%s> kicked <%s> from the server%s'%(chan, client.username, user, reason),chan)
			self.out_SERVERMSG(client, 'You\'ve kicked <%s> from the server.' % user)
			self.out_SERVERMSG(kickeduser, 'You\'ve been kicked from server by <%s>%s' % (client.username, reason))
			kickeduser.Remove('was kicked from server by <%s>: %s' % (client.username, reason))

	def in_TESTLOGIN(self, client, username, password):
		'''
		Test logging in as target user.

		@required.str username: The target user.
		@required.str password: The password to try.
		'''
		good, reason = self._validUsernameSyntax(username)
		if not good:
			client.Send('TESTLOGINDENY %s' %(reason))
			return

		good, reason = self._validPasswordSyntax(password)
		if not good:
			client.Send('TESTLOGINDENY %s' %(reason))
			return

		user = self.clientFromUsername(username, True)
		if user and user.password == password:
			client.Send('TESTLOGINACCEPT %s %s' % (user.username, user.db_id))
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

	def in_LISTCOMPFLAGS(self, client):
		flags = ""
		for flag in flag_map:
			if len(flags)>0:
				flags += " " + flag
			else:
				flags = flag
		client.Send("COMPFLAGS %s" %(flags))

	def in_BAN(self, client, username, duration, reason):
		'''
		Ban target user from the server.

		@required.str username: The target user.
		@required.float duration: The duration in days.
		@required.str reason: The reason to be shown.
		'''
		try: duration = float(duration)
		except:
			self.out_SERVERMSG(client, 'Duration must be a float (the ban duration in days)')
			return
		response = self.userdb.ban_user(client, username, duration, reason)
		if response: self.out_SERVERMSG(client, '%s' % response)

	def in_UNBAN(self, client, username):
		'''
		Remove all bans for target user from the server.

		@required.str username: The target user.
		'''
		response = self.userdb.unban_user(username)
		if response: self.out_SERVERMSG(client, '%s' % response)

	def in_BANIP(self, client, ip, duration, reason):
		'''
		Ban an IP address from the server.

		@required.str ip: The IP address to ban.
		@required.float duration: The duration in days.
		@required.str reason: The reason to show.
		'''
		try: duration = float(duration)
		except:
			self.out_SERVERMSG(client, 'Duration must be a float (the ban duration in days)')
			return
		response = self.userdb.ban_ip(client, ip, duration, reason)
		if response: self.out_SERVERMSG(client, '%s' % response)

	def in_UNBANIP(self, client, ip):
		'''
		Remove all bans for target IP from the server.

		@required.str ip: The target IP.
		'''
		response = self.userdb.unban_ip(ip)
		if response: self.out_SERVERMSG(client, '%s' % response)

	def in_BANLIST(self, client):
		'''
		Retrieve a list of all bans currently active on the server.
		'''
		for entry in self.userdb.banlist():
			self.out_SERVERMSG(client, '%s' % entry)

	def in_SETACCESS(self, client, username, access):
		'''
		Set the access level of target user.

		@required.str username: The target user.
		@required.str access: The new access to apply.
		Access levels: user, mod, admin
		'''
		user = self.clientFromUsername(username, True)
		if not user:
			self.out_SERVERMSG(client, "User not found.")
			return
		if not access in ('user', 'mod', 'admin'):
			self.out_SERVERMSG(client, "Invalid access mode, only user, mod, admin is valid.")
			return
		user.access = access
		if username in self._root.usernames:
			self._calc_access_status(user)
			self._root.broadcast('CLIENTSTATUS %s %s'%(username, user.status))
		self.userdb.save_user(user)

	def in_RELOAD(self, client):
		'''
		Reload core parts of the server code from source. This also reparses motd, update list, and trusted proxy file.
		Do not use this for changes unless you are very confident in your ability to recover from a mistake.

		Parts reloaded:
		ChanServ.py
		Protocol.py
		SayHooks.py

		User databases reloaded:
		SQLUsers.py
		LanUsers.py
		'''
		if not 'admin' in client.accesslevels:
		    return
		self._root.reload()
		self._root.console_write("Stats of command usage:")
		for k,v in self.stats.iteritems():
			self._root.console_write("%s %d" % (k, v))

	def in_CLEANUP(self, client):
		nchan = 0
		nbattle = 0
		nuser = 0
		#cleanup battles
		tmpbattle = self._root.battles.copy()
		for battle in tmpbattle:
			for user in self._root.battles[battle].users:
				if not user in self._root.usernames:
					self._root.console_write("deleting user in battle %s" % user)
					self._root.battles[battle].users.remove(user)
					nuser = nuser + 1
			if not self._root.battles[battle].host in self._root.usernames:
				self._root.console_write("deleting battle %s" % battle)
				del self._root.battles[battle]
				nbattle = nbattle + 1
				continue

		#cleanup channels
		tmpchannels = self._root.channels.copy()
		for channel in tmpchannels:
			for user in self._root.channels[channel].users:
				if not user in self._root.usernames:
					self._root.console_write("deleting user %s from channel %s" %( user, channel))
					self._root.channels[channel].users.remove(user)
			if len(self._root.channels[channel].users) == 0:
				del self._root.channels[channel]
				self._root.console_write("deleting empty channel %s" % channel)
				nchan = nchan + 1

		self.out_SERVERMSG(client, "deleted channels: %d battles: %d users: %d" %(nchan, nbattle, nuser))


	def in_SETBATTLE(self, client, tags):
		'''
		set a value in a battle, for example:

		@required tags: tags to be set, see SETSCRIPTTAGS, current supported tags:
				status=<battlestatus> <color>
			{ "username": {
				"user1" {
					"status": 1,
					"color" : 2
					}
				}
			}
			(without newline)
		'''
		try:
			data = json.loads(tags)
		except:
			self.out_FAILED(client, "SETBATTLE", "invalid json format", True)
			return
		try:
			for key, value in tags.iteritems():
				if key == "username":
					for subkey, subvalue in value.iteritems():
						if not self._canForceBattle(client, subvalue):
							return
						username = subvalue
						user = self.clientFromUsername(username)
						for subsubkey, subsubvalue in subvalue.iteritems():
							if subsubkey == "status":
								self.in_MYBATTLESTATUS(client, subsubvalue, client.color)
							if subsubkey == "color":
								self.in_MYBATTLESTATUS(client, client.status, subsubvalue)
						else:
							self.out_FAILED(client, "SETBATTLE", "unknown tag %s=%s" % (key, value), True)
		except:
			self.out_FAILED(client, "SETBATTLE", "couldn't handle values", True)

	# Begin outgoing protocol section #
	#
	# any function definition beginning with out_ and ending with capital letters
	# is a definition of an outgoing command.
	def out_DENIED(self, client, username, reason, inc = True):
		'''
			response to LOGIN
		'''
		if inc:
			client.failed_logins = client.failed_logins + 1
		client.Send("DENIED %s" %(reason))
		self._root.console_write('Handler %s:%s Failed to log in user <%s>: %s.'%(client.handler.num, client.session_id, username, reason))

	def out_OPENBATTLEFAILED(self, client, reason):
		'''
			response to OPENBATTLE
		'''
		client.Send('OPENBATTLEFAILED %s' % (reason))
		self._root.console_write('Handler %s: <%s> OPENBATTLEFAILED: %s' % (client.handler.num, client.username, reason))

	def out_SERVERMSG(self, client, message, log = False):
		'''
			send a message to the client
		'''
		client.Send('SERVERMSG %s' %(message))
		if log:
			self._root.console_write('Handler %s: <%s>: %s' % (client.handler.num, client.username, message))

	def out_FAILED(self, client, cmd, message, log = False):
		'''
			send to a client when a command failed
		'''
		client.Send('FAILED %s %s' %(cmd, message))
		if log:
			self._root.console_write('Handler %s <%s>: %s %s' % (client.handler.num, client.username, cmd, message))


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
