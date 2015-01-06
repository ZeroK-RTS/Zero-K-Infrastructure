from pygeoip import pygeoip
import traceback

dbfile = 'GeoIP.dat'

def update():
	gzipfile = dbfile + ".gz"
	f = open(gzipfile, 'w')
	dburl = 'http://geolite.maxmind.com/download/geoip/database/GeoLiteCountry/GeoIP.dat.gz'
	import urllib2
	import gzip
	print("Downloading %s ..." %(dburl))
	response = urllib2.urlopen(dburl)
	f.write(response.read())
	f.close()
	print("done!")
	f = gzip.open(gzipfile)
	db = open(dbfile, 'w')
	db.write(f.read())
	f.close()
	db.close()

try:
	f=open(dbfile,'r')
	f.close()
except:
	print("%s doesn't exist, downloading..." % (dbfile))
	update()

def loaddb():
	global geoip
	try:
		geoip = pygeoip.Database(dbfile)
		return True
	except Exception as e:
		print("Couldn't load %s: %s" % (dbfile, str(e)))
		print(traceback.format_exc())
		return False

working = loaddb()


def lookup(ip):
	if not working: return '??'
	addrinfo = geoip.lookup(ip)
	if not addrinfo.country: return '??'
	return addrinfo.country

def reloaddb():
	if not working: return
	loaddb()
"""
print lookup("37.187.59.77")
print lookup("77.64.139.108")
print lookup("8.8.8.8")
print lookup("0.0.0.0")
import csv
with open('/tmp/test.csv', 'rb') as csvfile:
	reader = csv.reader(csvfile, delimiter=' ', quotechar='"')
	for row in reader:
		ip = row[0]
		print("%s %s" %(ip, lookup(row[0])))
"""
