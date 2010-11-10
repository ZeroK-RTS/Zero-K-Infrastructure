--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Mission Score",
    desc      = "Displays mission score.",
    author    = "quantum",
    date      = "2009-4-1",
    license   = "GNU GPL, v2 or later",
    layer     = 1, 
    enabled   = true  --  loaded by default?
  }
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local Spring, gl = Spring, gl

local fontSize = 15

function widget:DrawScreen()
  local viewSizeX, viewSizeY = gl.GetViewSizes()
  local y = 250
  for _, teamID in ipairs(Spring.GetTeamList()) do
    local score = Spring.GetTeamRulesParam(teamID, "score")
    if score then
      local _, playerID = Spring.GetTeamInfo(teamID)
      local playerName = Spring.GetPlayerInfo(playerID)
      local text = string.format("Score (%s): %d", playerName, score)
      local x = viewSizeX/2
      y = y + fontSize + fontSize*0.2
      gl.Color(1, 1, 1, 1)
      gl.Text(text, x, y, fontSize, "co")
    end
  end
end


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------