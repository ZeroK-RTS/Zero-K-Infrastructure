function widget:GetInfo()
  return {
    name      = "FPS Log direct",
    desc      = "Logs FPS at regular intervals and writes to a textfile",
    author    = "knorke, modified by KingRaptor and Licho",
    date      = "2011",
    license   = "dfgh",
    layer     = 0,
    enabled   = true  --  loaded by default	
  }
end

local GetGameFrame = Spring.GetGameFrame

local PERIOD = 60	-- screenframes

local frames = {}
local loggedi = 0
local gameStart = Spring.GetGameSeconds()>0

local screenFrame = 0
local screenFrameLifetime = 0
local timeInterval = 0

local maxTimeInterval = 0

function widget:Update(dt)
	if not gameStart then return end
	screenFrame = screenFrame + 1
	timeInterval = timeInterval + dt
	
	if (dt > maxTimeInterval) then 
		maxTimeInterval = dt
	end
	
	if screenFrame == PERIOD then
		screenFrameLifetime = screenFrameLifetime + screenFrame
		local time_spend_per_frame = timeInterval / PERIOD
		local fps = 1 / time_spend_per_frame
		
		Spring.Echo("!transmitlobby FPS: "..fps)
		Spring.Echo("!transmitlobby Max lag: ".. maxTimeInterval)
		
		loggedi = loggedi + 1
		screenFrame = 0
		timeInterval = 0
		maxTimeInterval = 0
	end	
end

function widget:GameStart()
	gameStart = true
end

