
function gadget:GetInfo()
  return {
    name      = "Gadget_Fps_Log",
    desc      = "Some random logging",
    author    = "Licho",
    date      = "2013",
    license   = "GNU GPL, v2 or later",
    layer     = 1,
    enabled   = true -- loaded by default?
  }
end 

local GetTimer = Spring.GetTimer 
local DiffTimers = Spring.DiffTimers 

local START_GRACE = 64 -- do not report this first frames

if (gadgetHandler:IsSyncedCode()) then

function gadget:GameFrame(gf)
	SendToUnsynced("gameFrame", gf)
end


else 

local frameTimer= GetTimer()
local gfTimer = GetTimer()
local lastGameFrame = 0


function gadget:Update() 
	local newTimer = GetTimer()
	--local gf = Spring.GetGameFrame()
	if (Spring.GetGameFrame() > START_GRACE) then
		Spring.Echo("!transmitlobby g_update_dt: "..DiffTimers(newTimer, frameTimer))
	end
	frameTimer = newTimer
end

local function gameFrame(_, gf)
	if gf > lastGameFrame then
		local newTimer = GetTimer()
		if (Spring.GetGameFrame() > START_GRACE) then
			Spring.Echo("!transmitlobby g_gameframe_dt: "..DiffTimers(newTimer, gfTimer) / (gf - lastGameFrame))
		end
		lastGameFrame = gf
		gfTimer = newTimer
	end
end 

function gadget:Initialize()
    gadgetHandler:AddSyncAction("gameFrame", gameFrame)
end

end