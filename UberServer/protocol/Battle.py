from AutoDict import AutoDict

class Battle(AutoDict):
	def __init__(self, root, id, type, natType, password, port, maxplayers,
						hashcode, rank, maphash, map, title, modname,
						passworded, host, users, spectators=0,
						startrects={}, disabled_units=[], pending_users=set(),
						authed_users=set(), bots={}, script_tags={},
						replay_script={}, replay=False,
						sending_replay_script=False, locked=False,
						engine=None, version=None):
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

		self.bots = bots
		self.script_tags = script_tags
		self.replay_script = replay_script
		self.replay = replay
		self.sending_replay_script = sending_replay_script
		self.locked = locked
		self.spectators = 0
		self.__AutoDictInit__()

