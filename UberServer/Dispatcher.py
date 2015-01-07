import Multiplexer, Client
import socket, thread, traceback
from protocol import Protocol, Channel

class Dispatcher:
	def __init__(self, root, server):
		self._root = root
		self.server = server
		self.poller = Multiplexer.BestMultiplexer()
		self.socketmap = {}
		self.workers = []
		self.protocol = Protocol.Protocol(root)
		# legacy vars
		self.thread = thread.get_ident()
		self.num = 0
	
	def pump(self):
		self.poller.register(self.server)
		self.poller.pump(self.callback)

	def callback(self, inputs, outputs, errors):
		try:
			for s in inputs:
				if s == self.server:
					try:
						conn, addr = self.server.accept()
					except socket.error, e:
						if e[0] == 24: # ulimit maxfiles, need to raise ulimit
							self._root.console_write('Maximum files reached, refused new connection.')
						else:
							raise socket.error, e
					client = Client.Client(self._root, conn, addr, self._root.session_id)
					self.addClient(client)
				else:
					try:
						data = s.recv(4096)
						if data:
							if s in self.socketmap: # for threading, just need to pass this to a worker thread... remember to fix the problem for any calls to handler, and fix msg ids (handler.thread)
									self.socketmap[s].Handle(data)
							else:
								self._root.console_write('Problem, sockets are not being cleaned up properly.')
						else:
							raise socket.error, 'Connection closed.'
					except socket.error:
						self.removeSocket(s)
			
			for s in outputs:
				try:
					self.socketmap[s].FlushBuffer()
				except KeyError:
					self.removeSocket(s)
				except socket.error:
					self.removeSocket(s)
		except: self._root.error(traceback.format_exc())

	def rebind(self):
		self.protocol = Protocol.Protocol(self._root)
		for client in self._root.clients.values():
			client.Bind(protocol=self.protocol)

	def addClient(self, client):
		self._root.clients[self._root.session_id] = client
		self._root.session_id += 1
		client.Bind(self, self.protocol)
		if not client.static:
			self.socketmap[client.conn] = client
			self.poller.register(client.conn)
	
	def removeSocket(self, s):
		if s in self.socketmap:
			self.socketmap[s].Remove()
	
	def finishRemove(self, client, reason='Quit'):
		if client.static or not client._protocol: return # static clients don't disconnect
		client._protocol._remove(client, reason)
		
		s = client.conn
		if s in self.socketmap: del self.socketmap[s]
		self.poller.unregister(s)
		
		try:
			s.shutdown(socket.SHUT_RDWR)
			s.close()
		except socket.error: #socket shut down by itself ;) probably got a bad file descriptor
			try:
				s.close()
			except socket.error:
				pass # in case shutdown was called but not close.
		except AttributeError:
			pass
		
		self._root.console_write('Client disconnected from %s, session ID was %s: %s'%(client.ip_address, client.session_id, reason))

