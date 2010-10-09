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

local viewSizeX, viewSizeY
local gl, GL, Spring = gl, GL, Spring
local fontHandler = loadstring(VFS.LoadFile(LUAUI_DIRNAME.."modfonts.lua", VFS.ZIP_FIRST))()
local font = "LuaUI/Fonts/FreeSansBold_16"


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------


function widget:DrawScreen()
  local viewSizeX, viewSizeY = gl.GetViewSizes()
  local score = Spring.GetGameRulesParam"score"
  if not score then return end
  local text = string.format("Score: %d", score)
  local width = fontHandler.GetTextWidth(text)
  fontHandler.UseFont(font)
  gl.Color(1, 1, 1, 1)
  fontHandler.Draw(text, (viewSizeX-width)/2, 100)
end


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------