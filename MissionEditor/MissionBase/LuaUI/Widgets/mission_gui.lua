-- $Id: mission_gui.lua 3171 2008-11-06 09:06:29Z det $
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Mission GUI",
    desc      = "Turning this off might disrupt the mission.",
    author    = "quantum",
    date      = "Sept 11, 2008",
    license   = "GPL v2 or later",
    layer     = 0,
    enabled   = true --  loaded by default?
  }
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function PlaySound(fileName, ...)
  local path = "LuaUI/Sounds/"..fileName
  if VFS.FileExists(path) then
    Spring.PlaySoundFile(path, ...)
  else
    print("Error: file "..path.." doest not exist.")
  end
end

function MissionEvent(e)
  if e.logicType == "GuiMessageAction" then
    if e.image then 
      WG.Message:Show{
        texture = ":n:LuaUI/Images/"..e.image,
        text = e.message,
        width = e.imageWidth,
        height = e.imageHeight,
        pause = e.pause,
      }
    else
      WG.Message:Show{text = e.message, width = e.width, pause = e.pause}
    end
  elseif e.logicType == "PauseAction" then
    Spring.SendCommands"pause"
  elseif e.logicType == "MarkerPointAction" then
    local height = Spring.GetGroundHeight(e.x, e.y)
    Spring.MarkerAddPoint(e.x, height, e.y, e.text)
    if e.centerCamera then
      Spring.SetCameraTarget(e.x, height, e.y, 1)
    end
  elseif e.logicType == "SetCameraPointTargetAction" then
    local height = Spring.GetGroundHeight(e.x, e.y)
    Spring.SetCameraTarget(e.x, height, e.y, 1)
  elseif e.logicType == "SoundAction" then
    PlaySound(e.sound)
  elseif e.logicType == "SunriseAction" then
    WG.noonWanted = true
  elseif e.logicType == "SunsetAction" then
    WG.midnightWanted = true
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:Initialize()
  widgetHandler:RegisterGlobal("MissionEvent", MissionEvent)
end


function widget:Shutdown()
  widgetHandler:DeregisterGlobal("MissionEvent")
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------