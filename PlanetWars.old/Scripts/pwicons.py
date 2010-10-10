# Author: quantum
# Date: 27/12/2008
# License: GNU GPLv3 or later
# Description: Adds rank and PlanetWars participant icons next to player names.

import socket
import lobbyscript

api = lobbyscript.Callback()
gui = lobbyscript.GUI()

SERVER = 'planet-wars.eu'
PORT = 2666
RANK_ICONS_PATH = "lobby/python/scripts/pwranks/"
ICON_TYPE_RANK = "Planet-wars Rank"
ICON_TYPE_PARTICIPANT = "Planet-wars participant"

rankNames = ["Commander In Chief", "Field Marshall", "General", "Brigadier", 
              "Colonel", "Lt. Colonel", "Major", "Captain", "Commander"]
rankNames.reverse()

def toBGR(r, g, b): return (b * 65536) + (g * 256) + r

def _init():
    #api.ShowDebugWindow()
    print "initializing pwicons"
    iconsDict = {}
    for i in range(len(rankNames)):
      path = RANK_ICONS_PATH + "00" + str(i + 1) + ".png"
      iconsDict[i] = {'File': path, 'Name': rankNames[i], 'MaskColor': toBGR(255, 255, 255)}
    gui.AddPlayerIconType(ICON_TYPE_RANK, iconsDict)
    gui.AddPlayerIconType(ICON_TYPE_PARTICIPANT, { 0 : {'File': RANK_ICONS_PATH + 'planet.png', 'Name': "Planet Wars Participant", 'MaskColor': toBGR(255, 255, 255)}})
    
def _reinit(api):
  print "re-initializing pwicons"
  refresh_ranks()

def refresh_ranks():
    try: 
      client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
      client.connect((SERVER, PORT))
      global playerRanks
      playerNames = api.GetUsers().keys()
      client.send("/getranks "  + " ".join(playerNames) + "\n")
      response = ""
      while True:
          data = client.recv(1024)
          if not data: break
          response += data
      playerRanks = response.split()
      if len(playerNames) != len(playerRanks):
        print "error: playerNames and playerRanks do not match"
        return
      playerRanks = map(int, playerRanks)
      for i in range(len(playerNames)):
        name = playerNames[i]
        rank = int(playerRanks[i])
        if rank > 0: # -1 means player not in planetwars
          gui.SetPlayerIconId(name, ICON_TYPE_PARTICIPANT, 0)
          gui.SetPlayerIconId(name, ICON_TYPE_RANK, rank)
      client.close()
    except socket.error, (value,message):
      print "error: " + message
    
def cmd_pwicons():
    refresh_ranks()
    
def timer_60():
    refresh_ranks();
