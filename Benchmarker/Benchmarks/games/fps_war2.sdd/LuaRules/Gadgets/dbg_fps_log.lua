
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

local gf_dt_exceed_count = 100  -- how many can exceeded limit
local gf_dt_limit = 0.1 -- limit for the test


function gadget:Update() 
	local newTimer = GetTimer()
	--local gf = Spring.GetGameFrame()
	if (Spring.GetGameFrame() > START_GRACE) then
		Spring.Echo("!transmitlobby g_update_dt: "..DiffTimers(newTimer, frameTimer))
	end
	frameTimer = newTimer
end

function gadget:GameOver()
  local num = 0
  if gf_dt_exceed_count <= 0 then
		num = 1
  end
  Spring.Echo("!transmitlobby gf_dt_exceed:"..num)
end 


local function gameFrame(_, gf)
	if gf > lastGameFrame then
		local newTimer = GetTimer()
		if (Spring.GetGameFrame() > START_GRACE) then
			local gf_dt = DiffTimers(newTimer, gfTimer) / (gf - lastGameFrame)
			if (gf_dt > gf_dt_limit) then
				gf_dt_exceed_count = gf_dt_exceed_count - 1
				if gf_dt_exceed_count == 0 then
					Spring.Echo("!transmitlobby gf_dt_exceed:1")
					gf_dt_exceed_count = math.huge
				end
			end
			Spring.Echo("!transmitlobby g_gameframe_dt: "..gf_dt)
		end
		lastGameFrame = gf
		gfTimer = newTimer
	end
end 

function gadget:Initialize()
    gadgetHandler:AddSyncAction("gameFrame", gameFrame)
end

end