function widget:GetInfo()
  return {
    name      = "Widget_Fps_Log",
    desc      = "Some random logging",
    author    = "Licho",
    date      = "2013",
    layer     = 0,
    enabled   = true  --  loaded by default	
  }
end

local START_GRACE = 64

local GetTimer = Spring.GetTimer 
local DiffTimers = Spring.DiffTimers 
local frameTimer= GetTimer()

function widget:Update(dt)
	if not gameStart then return end
	if (Spring.GetGameFrame() > START_GRACE) then
		Spring.Echo("!transmitlobby w_update_dt: "..dt)
	end
end

function widget:DrawScreen()
	if not gameStart then return end
	local newTimer = GetTimer()
	if (Spring.GetGameFrame() > START_GRACE) then
		Spring.Echo("!transmitlobby w_drawscr_dt: "..DiffTimers(newTimer, frameTimer))
	end
	frameTimer = newTimer
end



function widget:GameStart()
	gameStart = true
end

