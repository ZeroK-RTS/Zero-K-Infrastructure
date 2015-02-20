--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--
--  file:    
--  brief:   
--  author: jK
--
--  Copyright (C) 2007.
--  Licensed under the terms of the GNU GPL, v2 or later.
--
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--
--  SYNCED
--

local doStart   = true;
local counter   = 0
local testID    = 0
local curBenchmark
local benchmarks = VFS.Include("LuaRules/Benchmarks/benchmarks.lua",nil, VFS.ZIP_ONLY)

function RecvLuaMsg(msg,playerID)
  if msg == "StartBenchmark" then 
	testID=testID+1
	StartBenchmark(testID)
  end
end

function Shutdown()
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local mapconst  = 64*Game.squareSize;
local mapX,mapY = Game.mapX*mapconst,Game.mapY*mapconst;
local groundMin, groundMax = Spring.GetGroundExtremes(); groundMin, groundMax = math.max(groundMin,0), math.max(groundMax,1);

function StartBenchmark(id)
  curBenchmark = benchmarks[id]
  if (not curBenchmark) then
	Spring.GameOver({})
    return
  end

  counter = curBenchmark.duration*30
  SendToUnsynced("StartBenchmark",id)
end

function StopBenchmark(id)
  SendToUnsynced("StopBenchmark",id)
  if (curBenchmark ~= nil and curBenchmark.finish) then
    curBenchmark.finish()
  end
  doStart = true;
  curBenchmark = nil
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function GameFrame()
  if (curBenchmark) then
    if (doStart)and(curBenchmark.start) then
      curBenchmark.start()
      doStart = false
    end

    if (curBenchmark.gameframe) then
      curBenchmark.gameframe()
    end
  end

  if (counter>0) then
    counter = counter-1
    if (counter==0) then
      StopBenchmark(testID)
    end
  end

  SendToUnsynced("GameFrame")
end