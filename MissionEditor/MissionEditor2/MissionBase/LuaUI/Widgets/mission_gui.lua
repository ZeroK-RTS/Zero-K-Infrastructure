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
    enabled   = true, --  loaded by default?
    api       = true
  }
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
local camState = {}

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
		fontsize = e.fontSize,
        pause = e.pause,
      }
    else
      if WG.ShowMessageBox then
        WG.ShowMessageBox(e.message, e.width, e.height, e.fontSize, e.pause)
      else
        WG.Message:Show{text = e.message, width = e.width, height = e.height, fontsize = e.fontSize, pause = e.pause}
      end
    end
  elseif e.logicType == "GuiMessagePersistentAction" then
      if WG.ShowPersistentMessageBox then
        WG.ShowPersistentMessageBox(e.message, e.width, e.height, e.fontSize, (e.image and "LuaUI/Images/"..e.image) or nil)
      end
  elseif e.logicType == "HideGuiMessagePersistentAction" then
      if WG.HidePersistentMessageBox then
        WG.HidePersistentMessageBox()
      end
  elseif e.logicType == "ConvoMessageAction" then
      if WG.AddConvo then
        WG.AddConvo(e.message, e.fontSize, (e.image and "LuaUI/Images/"..e.image) or nil, e.sound and "LuaUI/sounds/convo/"..e.sound or nil, e.time)
      end
  elseif e.logicType == "ClearConvoMessageQueueAction" then
      if WG.ClearConvoQueue then
        WG.ClearConvoQueue()
      end  
  elseif e.logicType == "AddObjectiveAction" then
      if WG.AddObjective then
        WG.AddObjective(e.id, e.title, e.description, nil, e.status)
      end
  elseif e.logicType == "ModifyObjectiveAction" then
      if WG.ModifyObjective then
        WG.ModifyObjective(e.id, e.title, e.description, nil, e.status)
      end
  elseif e.logicType == "EnterCutsceneAction" then
      if WG.Cutscene and WG.Cutscene.EnterCutscene then
        WG.Cutscene.EnterCutscene(e.instant)
      end
  elseif e.logicType == "LeaveCutsceneAction" then
      if WG.Cutscene and WG.Cutscene.LeaveCutscene then
        WG.Cutscene.LeaveCutscene(e.instant)
      end
  elseif e.logicType == "FadeOutAction" then
      if WG.Cutscene and WG.Cutscene.FadeOut then
        WG.Cutscene.FadeOut(e.instant)
      end
  elseif e.logicType == "FadeInAction" then
      if WG.Cutscene and WG.Cutscene.FadeIn then
        WG.Cutscene.FadeIn(e.instant)
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
  elseif e.logicType == "SetCameraPosDirAction" then
    if e.rx then e.rx = math.rad(e.rx) end
    if e.ry then e.ry = math.rad(e.ry) end
    local cam = {
      px = e.px, py = e.py, pz = e.pz, rx = e.rx, ry = e.ry, mode = 4,
    }
    Spring.SetCameraState(cam, math.max(e.time, 0))
  elseif e.logicType == "SaveCameraStateAction" then
      camState = Spring.GetCameraState()
  elseif e.logicType == "RestoreCameraStateAction" then
      Spring.SetCameraState(camState, 1)
  elseif e.logicType == "ShakeCameraAction" then
      if WG.ShakeCamera then WG.ShakeCamera(e.strength) end
  elseif e.logicType == "SoundAction" then
    PlaySound(e.sound)
  elseif e.logicType == "MusicAction" then
    if WG.Music and WG.Music.StartTrack then
      if e.track then
	WG.Music.StartTrack("LuaUI/Sounds/music/"..e.track)
      else
	WG.Music.StartTrack()
      end
    elseif e.track ~= nil then
      Spring.StopSoundStream()
      Spring.PlaySoundStream("LuaUI/Sounds/music/"..e.track, 0.5)
    end
  elseif e.logicType == "StopMusicAction" then
    if WG.Music and WG.Music.StopTrack then
      WG.Music.StopTrack(e.noContinue)
    else
      Spring.StopSoundStream()
    end
  elseif e.logicType == "SunriseAction" then
    WG.noonWanted = true
  elseif e.logicType == "SunsetAction" then
    WG.midnightWanted = true
  elseif e.logicType == "CustomAction2" then
    local func, err = loadstring(e.codeStr)
    if err then
      error("Failed to load custom action: ".. e.codeStr)
      return
    end
    func()
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