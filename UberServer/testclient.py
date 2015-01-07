#!/usr/bin/env python
# coding=utf-8

import socket, inspect
from base64 import b64encode
import md5
import time
import threading


class LobbyClient:
	def __init__(self, username):
		self.socket = None
		try:
			self.socket = socket.create_connection(("localhost",8200), 5)
		except socket.error as msg:
			print(msg)
		self.lastping = time.time()
		self.pingsamples = 0
		self.maxping = 0
		self.minping = 100
		self.average = 0
		self.count = 0
		self.username = username
		self.loops = 0
	def Send(self, data):
		self.socket.send(str(data)+"\n")

	def handle(self, msg):
		numspaces = msg.count(' ')
		if numspaces:
			command,args = msg.split(' ',1)
		else:
			command = msg
		command = command.upper()

		funcname = 'in_%s' % command
		function = getattr(self, funcname)
		function_info = inspect.getargspec(function)
		total_args = len(function_info[0])-1
		optional_args = 0
		if function_info[3]:
			optional_args = len(function_info[3])
		required_args = total_args - optional_args

		if required_args == 0 and numspaces == 0:
			function()
			return True


		# bunch the last words together if there are too many of them
		if numspaces > total_args-1:
			arguments = args.split(' ',total_args-1)
		else:
			arguments = args.split(' ')
		try:
			function(*(arguments))
		except Exception, e:
			print("Error handling : %s %s" % (msg, e))

	def login(self):
		self.Send("LOGIN %s %s" %(self.username, b64encode(md5.new(self.username).digest())))
	def in_TASSERVER(self, protocolVersion, springVersion, udpPort, serverMode):
		#print("%s %s %s %s" % (protocolVersion, springVersion, udpPort, serverMode))
		self.Send("REGISTER %s %s" %(self.username, b64encode(md5.new(self.username).digest())))
	def in_SERVERMSG(self, msg):
		#print("SERVERMSG %" % msg)
		pass
	def in_REGISTRATIONDENIED(self, msg):
		self.login()
	def in_AGREEMENT(self, msg):
		pass
	def in_AGREEMENTEND(self):
		self.Send("CONFIRMAGREEMENT")
		self.login()
	def in_REGISTRATIONACCEPTED(self):
		self.login()
	def in_ACCEPTED(self, msg):
		print(msg)
	def in_MOTD(self, msg):
		pass
	def in_ADDUSER(self, msg):
		print(msg)
	def in_BATTLEOPENED(self, msg):
		print(msg)
	def in_UPDATEBATTLEINFO(self, msg):
		print(msg)
	def in_JOINEDBATTLE(self, msg):
		print(msg)
	def in_CLIENTSTATUS(self, msg):
		pass
		#print(msg)
	def in_LOGININFOEND(self):
		# do stuff
		#self.Send("PING")
		#self.Send("JOIN bla")
		pass
	def in_BATTLECLOSED(self, msg):
		print(msg)
	def in_REMOVEUSER(self, msg):
		print(msg)
	def in_LEFTBATTLE(self, msg):
		print(msg)
	def in_PONG(self):
		if self.count > 1000:
			print("max %0.3f min %0.3f average %0.3f" %(self.maxping, self.minping, (self.average / self.pingsamples)))
			self.Send("EXIT")
			return
		if self.lastping:
			diff = time.time() - self.lastping
			if diff>self.maxping:
				self.maxping = diff
			if diff<self.minping:
				self.minping = diff
			self.average = self.average + diff
			self.pingsamples = self.pingsamples +1
			print("%0.3f" %(diff))
		self.lastping = time.time()
		self.Send("PING")
		self.count = self.count + 1
	def in_JOIN(self, msg):
		print(msg)
	def in_CLIENTS(self, msg):
		print(msg)
	def in_JOINED(self, msg):
		print(msg)
	def in_LEFT(self, msg):
		print(msg)
	def run(self):
		if not self.socket:
			return
		sdata = ""
		while True:
			if (self.loops>5):
				self.socket.close()
				return
			self.loops += 1
			try:
				sdata += self.socket.recv(4096)
				if sdata.count('\n') > 0:
					data = sdata.split('\n')
					(datas, sdata) = (data[:len(data)-1], data[len(data)-1:][0])
					for data in datas:
						command = data.rstrip('\r').lstrip(' ') # strips leading spaces and trailing carriage return
						self.handle(command)
			except socket.timeout:
				pass
	def in_CHANNELTOPIC(self, msg):
		print(msg)
	def in_DENIED(self, msg):
		print(msg)

def runclient(i):
	print("Running client %d" %(i))
	name = "ubertest" + str(i)
	client = LobbyClient(name)
	client.run()
	print("finished: "+name)
threads = []
for x in range(0, 1):
	clientthread = threading.Thread(target=runclient, args=(x,))
	clientthread.start()
	threads.append(clientthread)
for t in threads:
	t.join()

