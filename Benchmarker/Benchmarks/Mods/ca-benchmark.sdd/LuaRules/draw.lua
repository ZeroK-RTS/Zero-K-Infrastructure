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
--  UNSYNCED
--

local testID = 0;
local benchmarks = VFS.Include("LuaRules/Benchmarks/benchmarks.lua",nil, VFS.ZIP_ONLY)

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

VFS.Include("LuaRules/resultFile.lua",nil, VFS.ZIP_ONLY)
if (output:Open()==false) then
  Script.Kill("")
end

output:WriteHeader()

Spring.SendCommands("spectator","cheats","godmode")

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function RecvFromSynced(cmd,...)
  if (cmd=="StartBenchmark") then
    StartBenchmark(...);
  elseif (cmd=="StopBenchmark") then
    StopBenchmark(...);
  elseif (type(_G[cmd])=="function") then
    _G[cmd](...);
  end
  return true;
end

-------------------------------------------------------------------------------
-------------------------------------------------------------------------------

local function round(num, idp)
  return string.format("%." .. (idp or 0) .. "f", num)
end

--// used vars
local startBenchSend  = false
local FPSs            = {}
local timeTable       = {}  --//used to calc the fps per frame
local testRunning     = false
local thisGameFrame   = 0
local thisVideoFrame  = 0
local testPause       = 0
local nextTestFrame   = testPause
local durationCounter = -1
local curTest

-------------------------------------------------------------------------------
-------------------------------------------------------------------------------
--
--
--

function Shutdown()
  output:Close()
end

function DrawScreen(vsx,vsy)
  table.insert( timeTable, Spring.GetTimer() )
  thisVideoFrame = thisVideoFrame+1

  if (thisGameFrame>0)and(not testRunning) then
    if (thisGameFrame<nextTestFrame) then
      if (benchmarks[testID+1]) then
        gl.Text("\255\255\255\001".."Starting '"..benchmarks[testID+1].name.."' test in", vsx*0.5, vsy*0.5+45, 25, "oc")
        gl.Text("\255\255\255\001"..round((nextTestFrame-thisGameFrame)/30,1), vsx*0.5, vsy*0.5, 36, "oc")
      end
    elseif (thisGameFrame<nextTestFrame+20) then
      gl.Text("\255\255\001\001GO!", vsx*0.5, vsy*0.5, 50, "oc")
    end
  end

  if (durationCounter>=0) then
    gl.Text("\255\255\001\001"..curTest.name, vsx*0.5, vsy*0.5+45, 25, "oc")
    gl.Text("\255\255\001\001"..round(durationCounter/30,1), vsx*0.5, vsy*0.5, 36, "oc")
  end
end

function GameFrame()
  thisGameFrame = Spring.GetGameFrame()

  if (not testRunning)and(not startBenchSend)and(thisGameFrame>nextTestFrame+20) then
    Spring.SendLuaRulesMsg("StartBenchmark")
    startBenchSend = true
  end

  --if (testRunning)and((n+1)%30<1) then
  --  table.insert(FPSs,Spring.GetFPS())
  --end

  if (durationCounter>=0) then
    durationCounter=durationCounter-1
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function StartBenchmark(id)
  curTest = benchmarks[id]
  durationCounter = curTest.duration*30;
  testPause = curTest.waitAfter or 4;
  testRunning = id;
  timeTable = {};
  testID = id;
  FPSs = {};
end

function StopBenchmark(id)
  if (curTest.type == "runtime") then
    output:WriteTestRuntime(curTest, math.floor(Spring.DiffTimers(timeTable[#timeTable],timeTable[1])), curTest.duration)
  else
    --// create our own FPS table (much more accurate)
    local fpsTable = {}
    for i=21,#timeTable,20 do
      table.insert( fpsTable , 20/Spring.DiffTimers(timeTable[i],timeTable[i-20]) )
    end

    output:WriteTest(curTest,fpsTable)
  end
  nextTestFrame = thisGameFrame + testPause
  testRunning = false
  timeTable = {}
  FPSs = {}
  startBenchSend = false
end


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------