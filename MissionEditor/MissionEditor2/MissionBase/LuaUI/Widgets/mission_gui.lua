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
local lastManualCam = {}

local function PlaySound(fileName, ...)
  local path = "LuaUI/Sounds/"..fileName
  if VFS.FileExists(path) then
    Spring.PlaySoundFile(path, ...)
  elseif VFS.FileExists(fileName) then
    Spring.PlaySoundFile(fileName, ...)
  else
    print("Error: file "..path.." doest not exist.")
  end
end

function MissionEvent(e)
  if e.logicType == "GuiMessageAction" then
    if e.image then 
      WG.Message:Show{
        texture = (e.imageFromArchive and "" or "LuaUI/Images/") .. e.image,
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
	local image
	if e.image then
	  image = (e.imageFromArchive and "" or "LuaUI/Images/") .. e.image
	end
        WG.ShowPersistentMessageBox(e.message, e.width, e.height, e.fontSize, image or nil)
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing message box widget for action " .. e.logicType)
      end
  elseif e.logicType == "HideGuiMessagePersistentAction" then
      if WG.HidePersistentMessageBox then
        WG.HidePersistentMessageBox()
      end
  elseif e.logicType == "ConvoMessageAction" then
      if WG.AddConvo then
	local image, sound
	if e.image then
	  image = (e.imageFromArchive and "" or "LuaUI/Images/") .. e.image
	end
	if e.sound then
	  sound = (e.soundFromArchive and "" or "LuaUI/Sounds/convo/") .. e.sound
	end
        WG.AddConvo(e.message, e.fontSize, image, sound, e.time)
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing message box widget for action " .. e.logicType)
      end
  elseif e.logicType == "ClearConvoMessageQueueAction" then
      if WG.ClearConvoQueue then
        WG.ClearConvoQueue()
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing message box widget for action " .. e.logicType)
      end 
  elseif e.logicType == "AddObjectiveAction" then
      if WG.AddObjective then
        WG.AddObjective(e.id, e.title, e.description, nil, "Incomplete")
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing Objectives widget for action " .. e.logicType)
      end
  elseif e.logicType == "ModifyObjectiveAction" then
      if WG.ModifyObjective then
        WG.ModifyObjective(e.id, e.title, e.description, nil, e.status)
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing Objectives widget for action " .. e.logicType)
      end
  elseif e.logicType == "AddUnitsToObjectiveAction" then
      if WG.AddUnitOrPosToObjective then
	for unitID in pairs(e.units) do
	  WG.AddUnitOrPosToObjective(e.id, unitID)
	end
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing or out-of-date Objectives widget for action " .. e.logicType)
      end
  elseif e.logicType == "AddPointToObjectiveAction" then
      if WG.AddUnitOrPosToObjective then
	WG.AddUnitOrPosToObjective(e.id, {e.x, e.y})
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing or out-of-date Objectives widget for action " .. e.logicType)
      end
  elseif e.logicType == "EnterCutsceneAction" then
      if WG.Cutscene and WG.Cutscene.EnterCutscene then
        WG.Cutscene.EnterCutscene(e.instant, e.skippable)
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
    local cam = lastManualCam
    cam.px = e.px or cam.px
    cam.py = e.py or cam.py
    cam.pz = e.pz or cam.pz
    cam.rx = e.rx or cam.rx
    cam.ry = e.ry or cam.ry
    cam.mode = 4
    Spring.SetCameraState(cam, math.max(e.time, 0))
  elseif e.logicType == "BeautyShotAction" then
    WG.BeautyShot(e.unitID, e)
  elseif e.logicType == "SaveCameraStateAction" then
    camState = Spring.GetCameraState()
  elseif e.logicType == "RestoreCameraStateAction" then
    Spring.SetCameraState(camState, 1)
  elseif e.logicType == "ShakeCameraAction" then
    if WG.ShakeCamera then
      WG.ShakeCamera(e.strength)
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing camera shake widget for action " .. e.logicType)
    end
  elseif e.logicType == "SoundAction" then
    PlaySound(e.sound)
  elseif e.logicType == "MusicAction" then
    local track = e.track
    if track and (not e.trackFromArchive) then
      track = "LuaUI/Sounds/music/" .. track
    end
    if WG.Music and WG.Music.StartTrack then
      if track then
	WG.Music.StartTrack(track)
      else
	WG.Music.StartTrack()
      end
    elseif track ~= nil then
      Spring.StopSoundStream()
      Spring.PlaySoundStream(track, 0.5)
    end
  elseif e.logicType == "MusicLoopAction" then
    if WG.Music and WG.Music.StartLoopingTrack then
      local intro, loop = e.trackIntro, e.trackLoop
      if intro and (not e.trackIntroFromArchive) then
	intro = "LuaUI/Sounds/music/" .. intro
      end
      if loop and (not e.trackLoopFromArchive) then
	loop = "LuaUI/Sounds/music/" .. loop
      end
      
      if e.trackIntro and e.trackLoop then
	WG.Music.StartLoopingTrack(intro, loop)
      else
	Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing Music Player widget for action " .. e.logicType)
      end
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
    -- workaround to make WG accessible to the customAction
    local func, err = loadstring("local WG = ({...})[1]; " .. e.codeStr)
    if err then
      error("Failed to load custom action: ".. e.codeStr)
      return
    end
    func(WG)
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