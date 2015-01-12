import inspect, sys, os, types, time, string

_permissionlist = ['admin', 'adminchan', 'mod', 'modchan', 'chanowner', 'chanadmin', 'chanpublic', 'public', 'battlehost', 'battlepublic']
_permissiondocs = {
					'admin':'Admin Commands', 
					'adminchan':'Admin Commands (channel)', 
					'mod':'Moderator Commands', 
					'modchan':'Moderator Commands (channel)', 
					'chanowner':'Channel Owner Commands (channel)', 
					'chanadmin':'Channel Admin Commands (channel)', 
					'chanpublic':'Public Commands (channel)', 
					'public':'Public Commands', 
					'battlepublic':'Public Commands (battle)', 
					'battlehost':'Battle Host Commands', 
					}

def _erase():
	l = dict(globals())
	for iter in l:
		if not iter == '_erase':
			del globals()[iter]

global bad_word_dict
global bad_site_list
bad_word_dict = {}
bad_site_list = []

def _update_lists():
	try:
		f = open('bad_words.txt', 'r')
		for line in f.readlines():
			if line.count(' ') < 1:
				bad_word_dict[line.strip()] = '***'
			else:
				sline = line.strip().split(' ', 1)
				bad_word_dict[sline[0]] = ' '.join(sline[1:])
		f.close()
	except Exception, e:
		print 'Error parsing profanity list: %s' %(e)
	try:
		f = open('bad_sites.txt', 'r')
		for line in f.readlines():
			line = line.strip()
			if line and not line in bad_site_list: bad_site_list.append(line)
		f.close()
	except Exception, e:
		print 'Error parsing shock site list: %s' %(e)
				
def _clear_lists():
	global bad_word_dict
	global bad_site_list
	bad_word_dict = {}
	bad_site_list = []

_update_lists()

chars = string.ascii_letters + string.digits

def _process_word(word):
	if word == word.upper(): uppercase = True
	else: uppercase = False
	lword = word.lower()
	if lword in bad_word_dict:
		word = bad_word_dict[lword]
	if uppercase: word = word.upper()
	return word

def _nasty_word_censor(msg):
	msg = msg.lower()
	for word in bad_word_dict.keys():
		if word.lower() in msg: return False
	return True

def _word_censor(msg):
	words = []
	word = ''
	letters = True
	for letter in msg:
		if bool(letter in chars) == bool(letters): word += letter
		else:
			letters = not bool(letters)
			words.append(word)
			word = letter
	words.append(word)
	newmsg = []
	for word in words:
		newmsg.append(_process_word(word))
	return ''.join(newmsg)

def _site_censor(msg):
	testmsg1 = ''
	testmsg2 = ''
	for letter in msg:
		if not letter: continue
		if letter.isalnum():
			testmsg1 += letter
			testmsg2 += letter
		elif letter in './%':
			testmsg2 += letter
	for site in bad_site_list:
		if site in msg or site in testmsg1 or site in testmsg2:
			return # 'I think I can post shock sites, but I am wrong.'
	return msg

def _spam_enum(client, chan):
	now = time.time()
	bonus = 0
	already = []
	times = [now]
	for when in dict(client.lastsaid[chan]):
		t = float(when)
		if t > now-5: # check the last five seconds # can check a longer period of time if old bonus decay is included, good for 2-3 second spam, which is still spam.
			for message in client.lastsaid[chan][when]:
				times.append(t)
				if message in already:
					bonus += 2 * already.count(message) # repeated message
				if len(message) > 50:
					bonus += min(len(message), 200) * 0.01 # long message: 0-2 bonus points based linearly on length 0-200
				bonus += 1 # something was said
				already.append(message)
		else: del client.lastsaid[chan][when]
	
	times.sort()
	last_time = None
	for t in times:
		if last_time:
			diff = t - last_time
			if diff < 1:
				bonus += (1 - diff) * 1.5
		last_time = t
	
	if bonus > 7: return True
	else: return False

def _spam_rec(client, chan, msg):
	now = str(time.time())
	if not chan in client.lastsaid: client.lastsaid[chan] = {}
	if not now in client.lastsaid[chan]:
		client.lastsaid[chan][now] = [msg]
	else:
		client.lastsaid[chan][now].append(msg)

def _chan_msg_filter(self, client, chan, msg):
	username = client.username
	channel = self._root.channels[chan]
	
	if channel.isMuted(client): return msg # client is muted, no use doing anything else
	if channel.antispam and not channel.isOp(client): # don't apply antispam to ops
		_spam_rec(client, chan, msg)
		if _spam_enum(client, chan):
			# this next line is necessary, because users aren't always muted i.e. you can't mute channel founders or moderators
			if channel.isMuted(client):
				channel.channelMessage('%s was muted for spamming.' % username)
				#if quiet: # maybe make quiet a channel-wide setting, so mute/kick/op/etc would be silent
				#	client.Send('CHANNELMESAGE %s You were quietly muted for spamming.'%chan)
				return ''
			
	if channel.censor:
		msg = _word_censor(msg)
	if channel.antishock:
		msg = _site_censor(msg)
	return msg

def hook_SAY(self, client, chan, msg):
	user = client.username
	channel = self._root.channels[chan]
	msg = _chan_msg_filter(self, client, chan, msg)
	return msg

def hook_SAYEX(self, client, chan, msg):
	msg = _chan_msg_filter(self, client, chan, msg)
	return msg

def hook_SAYPRIVATE(self, client, target, msg):
	return _site_censor(msg)

def hook_SAYBATTLE(self, client, battle_id, msg):
	return msg # no way to respond in battles atm

