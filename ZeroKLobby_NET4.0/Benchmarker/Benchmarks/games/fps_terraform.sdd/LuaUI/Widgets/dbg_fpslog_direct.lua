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

function widget:Update(dt)
	if not gameStart then return end
	if (Spring.GetGameFrame() > START_GRACE) then
		Spring.Echo("!transmitlobby w_update_dt: "..dt)
	end
end



function widget:GameStart()
	gameStart = true
	Spring.SendCommands('specfullview')
end

--function widget:GameFrame()
--	if f == 1500 then
--		Spring.SendCommands('specfullview')
--	end
--end