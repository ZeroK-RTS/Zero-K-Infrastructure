# if this is running from the scripts folder, move up a folder.
import os, sys
if not 'server.py' in os.listdir('.') and 'scripts' in os.listdir('..'):
	os.chdir('..')

sys.path.append('.')

import sys
sys.argv = [sys.argv[0], 'py2exe']

from distutils.core import setup
import py2exe

setup(
	console = ['server.py'],
	options = {'py2exe': {'compressed': 1,
			'optimize': 1,
			'bundle_files': 1,
			'excludes': [
				'_ssl',
			],
			'includes': [
				'Client',
				'DataHandler',
				'Dispatcher',
				'LANUsers',
				'Multiplexer',
				'NATServer',
				'Protocol',
				'SayHooks',
				'SQLUsers',
				'tasserver',
			],
		}},
	zipfile = None,
	ascii = True,
	)
