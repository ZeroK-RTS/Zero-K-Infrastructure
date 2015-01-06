import time
from select import * # eww hack but saves the other hack of selectively importing constants

class EpollMultiplexer:

	def __init__(self):
		self.filenoToSocket = {}
		self.socketToFileno = {}
		self.sockets = set([])
		self.output = set([])

		self.inMask = EPOLLIN | EPOLLPRI
		self.outMask = EPOLLOUT
		self.errMask = EPOLLERR | EPOLLHUP

		self.poller = epoll()

	def register(self, s):
		s.setblocking(0)
		fileno = s.fileno()
		self.filenoToSocket[fileno] = s
		self.socketToFileno[s] = fileno # gotta maintain this because fileno() lookups aren't possible on closed sockets
		self.sockets.add(s)
		self.poller.register(fileno, self.inMask | self.errMask)

	def unregister(self, s):
		if s in self.sockets:
			self.sockets.remove(s)
		if s in self.output:
			self.output.remove(s)
		if s in self.socketToFileno:
			fileno = self.socketToFileno[s]
			self.poller.unregister(fileno)
			del self.socketToFileno[s]
			del self.filenoToSocket[fileno]

	def setoutput(self, s, ready):
		# this if structure means it only scans output once.
		if not ready and s in self.output:
			self.output.remove(s)
		elif not ready:
			return
		elif ready and s in self.sockets:
			self.output.add(s)
		if not s in self.socketToFileno: return
		eventmask = self.inMask | self.errMask | (self.outMask if ready else 0)
		self.poller.modify(s, eventmask) # not valid for select.poll before python 2.6, might need to replace with register() in this context

	def pump(self, callback):
		while True:
			inputs, outputs, errors = self.poll()
			callback(inputs, outputs, errors)

	def poll(self):
		results = []
		try:
			results = self.poller.poll(10)
		except IOError as e:
			if e[0] == 4:
				# interrupted system call - this happens when any signal is triggered
				pass
			else:
				raise e

		inputs = []
		outputs = []
		errors = []

		for fd, mask in results:
			try:
				s = self.filenoToSocket[fd]
			except: # FIXME: socket was already deleted, shouldn't happen, but does!
				continue
			if mask & self.inMask: inputs.append(s)
			if mask & self.outMask: outputs.append(s)
			if mask & self.errMask: errors.append(s)
		return inputs, outputs, errors
