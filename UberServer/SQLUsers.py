from datetime import datetime, timedelta
import json, requests


class User:
	def __init__(self, username, password, lobby_id, dbuser):
		self.username = username
		self.password = password
		self.lobby_id = lobby_id
		self.access = dbuser['Access']
		self.id = dbuser['AccountID']
		self.bot = dbuser['IsBot']

class UsersHandler:
	def __init__(self, root, engine):
		self._root = root
		return


	
	def login_user(self, username, password, ip, lobby_id, user_id, cpu, local_ip, country):
		payload = {'login':username, 'password':password, 'ip':ip, 'user_id': user_id, 'lobby_name':lobby_id, 'cpu':cpu, 'country':country}

		r = requests.get("{0}/Login".format(self._root.lobby_service_url), params=payload)
		data = r.json()

		if data['Ok']:
			dbuser = data['Account']
			reason = User(username, password, lobby_id, dbuser)
		else:
			reason = data['Reason']

		return data['Ok'], reason
	
	def end_session(self, db_id):
		#possibly log logout here
		return

	def register_user(self, user, password, ip, country): # need to add better ban checks so it can check if an ip address is banned when registering an account :)
		if len(user)>20: return False, 'Username too long'
		
		if self._root.censor:
			if not self._root.SayHooks._nasty_word_censor(user):
				return False, 'Name failed to pass profanity filter.'

		payload = {'login':user, 'password':password, 'ip':ip, 'country':country}

		r = requests.get("{0}/Register".format(self._root.lobby_service_url), params=payload)
		data = r.json()		
		
		return data['Ok'], data['Reason']

