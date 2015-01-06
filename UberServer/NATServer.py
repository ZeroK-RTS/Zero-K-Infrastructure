try:
	from SocketServer import UDPServer,DatagramRequestHandler
except:
	# renamed in python 3
	from socketserver import UDPServer,DatagramRequestHandler
import sys

class CustomUDPServer(UDPServer):
	def Bind(self, root):
		self._root = root

	def finish_request(self, request, client_address):
		if '_root' in dir(self):
			self.RequestHandlerClass(request, client_address, self, self._root)
		else:
			pass # not bound to _root yet, no point in handling UDP

class handler(DatagramRequestHandler):
	def __init__(self, request, client_address, server, root):
		self._root = root
		self.request = request
		self.client_address = client_address
		self.server = server
		try:
			self.setup()
			self.handle()
			self.finish()
		finally:
			sys.exc_traceback = None    # Help garbage collection

	def handle(self):
		addr = self.client_address
		msg = self.rfile.readline().rstrip()
		#print "%s from %s(%d)" % (msg, addr[0], addr[1])
		self.wfile.write('PONG')
		if msg in self._root.usernames:
			self._root.usernames[msg]._protocol._udp_packet(msg, addr[0], addr[1])

class NATServer:
	def __init__(self, port):
		self.s = CustomUDPServer(('',port), handler)
		print("Awaiting UDP messages on port %d" % port)

	def bind(self, root):
		self.s.Bind(root)
	
	def start(self):
		self.s.serve_forever()

	def close(self):
		self.s.close()
