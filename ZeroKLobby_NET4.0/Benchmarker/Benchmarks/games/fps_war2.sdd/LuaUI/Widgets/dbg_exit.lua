function widget:GetInfo()
  return {
    name      = "Exit",
    desc      = "Exits Game At Frame",
    author    = "Google Frog",
    date      = "21 June 2013",
    layer     = 0,
    enabled   = true  --  loaded by default	
  }
end


function widget:GameFrame(f)
	if f == 600 then
		Spring.SendCommands{"quit","quitforce"}
	end
end