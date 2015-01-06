class AutoDict:
# method 1
#	def __getitem__(self, item):
#		return self.__getattribute__(item)

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
