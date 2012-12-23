--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Mission Countdown",
    desc      = "Displays mission countdowns.",
    author    = "quantum",
    date      = "2009-4-9",
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
fontHandler.UseFont(font)
local fontSize  = fontHandler.GetFontSize()
local lineSpacing = 10


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------


function widget:DrawScreen()
  local viewSizeX, viewSizeY = gl.GetViewSizes()
  local y = 100
  for _, pair in ipairs(Spring.GetGameRulesParams()) do
    -- local text = string.format("Score: %d", score)
    local key, expiry = next(pair)
    local countdownName = string.match(key, "countdown:(.+)")
    if countdownName and expiry > 0 then
      local frames = expiry - Spring.GetGameFrame()
      if frames > 0 then
        local text = string.format("%s %02d:%02d:%02d", countdownName, frames / (30*60), (frames/30)%60, (frames*3)%100) 
        local width = fontHandler.GetTextWidth(text)
        fontHandler.UseFont(font)
        gl.Color(1, 1, 1, 1)
        y = y + fontSize + lineSpacing
        fontHandler.Draw(text, (viewSizeX-width)/2, y, 16)
      end
    end
  end
end


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------