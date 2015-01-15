import time
from select import * # eww hack but saves the other hack of selectively importing constants

class BaseMultiplexer:
	def __init__(self): self.__multiplex_init__()
	
	def __multiplex_init__(self):
		self.sockets = set([])
		self.output = set([])
	
	def register(self, fd):
		fd.setblocking(0)
		self.sockets.add(fd)
		self.pollRegister(fd)
	
	def unregister(self, fd):
		if fd in self.sockets:
			self.sockets.remove(fd)
			if fd in self.output:
				self.output.remove(fd)
			self.pollUnregister(fd)
	
	def setoutput(self, fd, ready):
		# this if structure means it only scans output once.
		if not ready and fd in self.output:
			self.output.remove(fd)
			self.pollSetoutput(fd, ready)
		elif ready and fd in self.sockets:
			self.output.add(fd)
			self.pollSetoutput(fd, ready)
	
	def poll(self):
		return self.sockets, self.outputs, []

	def pump(self, callback):
		while True:
			inputs, outputs, errors = self.poll()
			callback(inputs, outputs, errors)

	def empty(self):
		if not self.sockets: return True
	
	def pollRegister(self, fd): pass
	def pollUnregister(self, fd): pass
	def pollSetoutput(self, fd, ready): pass


class SelectMultiplexer(BaseMultiplexer):
	def poll(self):
		if not self.sockets: return ([], [] ,[])
		try: return select(self.sockets, self.output, [], 0.1)
		except:
			inputs = []
			outputs = []
			errors = []
			for s in self.sockets:
				try: select([s], [s], [], 0.01)
				except:
					errors.append(s)
					self.unregister(s)
			inputs, outputs, _ = select(self.sockets, self.output, [], 0.1)
			return inputs, outputs, errors

class KqueueMultiplexer(BaseMultiplexer):
	def __init__(self):
		raise NotImplementedError
		self.poller = kqueue()
		self.socketmap = {}
		self.__multiplex_init__()
		
	def pollRegister(self, fd): pass
	def pollUnregister(self, fd): pass
	def pollSetoutput(self, fd, ready): pass


class BasePollMultiplexer(BaseMultiplexer):
	
	def __init__(self):
		self.inMask = 0
		self.outMask = 0
		self.errMask = 0
		self.args = []
		
		self.__poll_init__()
	
	def __poll_init__(self):
		self.filenoToSocket = {}
		self.socketToFileno = {}
		self.__multiplex_init__()
	
	def pollRegister(self, fd):
		fileno = fd.fileno()
		self.filenoToSocket[fileno] = fd
		self.socketToFileno[fd] = fileno # gotta maintain this because fileno() lookups aren't possible on closed sockets
		self.poller.register(fileno, self.inMask | self.errMask)
		
	def pollUnregister(self, fd):
		fileno = self.socketToFileno[fd]
		del self.socketToFileno[fd]
		del self.filenoToSocket[fileno]
		self.poller.unregister(fileno)

		
	def pollSetoutput(self, fd, ready):
		if not fd in self.socketToFileno: return
		eventmask = self.inMask | self.errMask | (self.outMask if ready else 0)
		self.poller.modify(fd, eventmask) # not valid for select.poll before python 2.6, might need to replace with register() in this context
		
	def poll(self):
		for i in xrange(5):
			try:
				results = self.poller.poll(*self.args)
			except IOError, e:
				if e[0] == 4:
					# interrupted system call - this happens when any signal is triggered
					continue
				else:
					raise e
			
			break
			
		inputs = []
		outputs = []
		errors = []
		
		inMask = self.inMask
		outMask = self.outMask
		errMask = self.errMask
		for fd, mask in results:
			if mask & inMask: inputs.append(self.filenoToSocket[fd])
			if mask & outMask: outputs.append(self.filenoToSocket[fd])
			if mask & errMask: errors.append(self.filenoToSocket[fd])
		return inputs, outputs, errors
	
class EpollMultiplexer(BasePollMultiplexer):
	def __init__(self):
		self.args = []
		
		self.inMask = EPOLLIN | EPOLLPRI
		self.outMask = EPOLLOUT
		self.errMask = EPOLLERR | EPOLLHUP
		
		self.poller = epoll()
		self.__poll_init__()

class PollMultiplexer(BasePollMultiplexer):
	def __init__(self):
		self.inMask = POLLIN | POLLPRI
		self.outMask = POLLOUT
		self.errMask = POLLERR | POLLHUP | POLLNVAL
		
		self.args = [250]
		
		self.poller = poll()
		self.__poll_init__()

BestMultiplexer = SelectMultiplexer
if 'kqueue' in dir() and False: # not implemented
	BestMultiplexer = KqueueMultiplexer
elif 'epoll' in dir():
	BestMultiplexer = EpollMultiplexer
elif 'poll' in dir():
	BestMultiplexer = PollMultiplexer