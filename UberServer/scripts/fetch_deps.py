# if this is running from the scripts folder, move up a folder.
import os, sys
if not 'server.py' in os.listdir('.') and 'scripts' in os.listdir('..'):
	os.chdir('..')

sys.path.append('.')

import urllib, zipfile, sys, os, ip2country

# url = 'http://public.zjt3.com/uberserver/deps.zip' # site down
url = 'http://lunixbochs.googlepages.com/deps.zip'

print()
print('Downloading uberserver dependencies from \n[%s]\n'%url)
urlfile = urllib.urlopen(url)

length = int(urlfile.info()['content-length'])

inc_bytes = True
total_bytes = 0
linestatus = 50
print('Length: %s bytes'%length)
iteration = -1
bytes = ''

while inc_bytes:
	inc_bytes = urlfile.read(1024)
	if inc_bytes:
		bytes += inc_bytes
	total_bytes += len(inc_bytes)
	linestatus += 1
	if linestatus == 51:
		iteration += 1
		if iteration > 0:
			sys.stdout.write(' [%s]'%(('%i%%'%(total_bytes*100/length)).rjust(4)))
		print('\n'+('%iK -> '%(iteration*50)).rjust(10),)
		linestatus = 1
	if linestatus % 10 == 1 and linestatus > 1:
		print(' ',)
	if bytes:
		sys.stdout.write('.')
		sys.stdout.flush()

just = 50 - linestatus + 5 - (linestatus / 10)
sys.stdout.write((' [%s]'%(('%i%%'%(total_bytes*100/length)).rjust(4))).rjust(just+6))

print()
print()

print('Download complete: %s/%s'%(total_bytes, length))
print()

temp = open('deps.zip', 'wb')
temp.write(bytes)
temp.close()

print('Extracting...')
zipdb = zipfile.ZipFile('deps.zip', 'r')
for entry in zipdb.namelist():
	try:
		f = open(entry, 'w')
		f.write(zipdb.read(entry))
		f.close()
	except IOError:
		if not os.path.exists(entry): os.mkdir(entry)
zipdb.close()
os.remove('deps.zip')

print('\nDone.')

import ip2country
