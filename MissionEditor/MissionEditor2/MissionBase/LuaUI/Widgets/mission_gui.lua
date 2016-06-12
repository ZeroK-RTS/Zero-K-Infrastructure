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

local function Translate(id, defaultString)
  if (not id) or (not WG.Translate) or (id == '') then
    return defaultString
  end
  return WG.Translate("missions", id) or defaultString
end

local actionsTable = {
  ["GuiMessageAction"] = function(e)
    local message = Translate(e.stringID, e.message)
    if e.image then 
      WG.Message:Show{
        texture = (e.imageFromArchive and "" or "LuaUI/Images/") .. e.image,
        text = message,
        width = e.imageWidth,
        height = e.imageHeight,
        fontsize = e.fontSize,
        pause = e.pause,
      }
    else
      if WG.ShowMessageBox then
        WG.ShowMessageBox(message, e.width, e.height, e.fontSize, e.pause)
      else
        WG.Message:Show{text = message, width = e.width, height = e.height, fontsize = e.fontSize, pause = e.pause}
      end
    end
  end,
  ["GuiMessagePersistentAction"] = function(e)
    if WG.ShowPersistentMessageBox then
      local image
      if e.image then
        image = (e.imageFromArchive and "" or "LuaUI/Images/") .. e.image
      end
      local message = Translate(e.stringID, e.message)
      WG.ShowPersistentMessageBox(message, e.width, e.height, e.fontSize, image or nil)
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing message box widget for action " .. e.logicType)
    end
  end,
  ["HideGuiMessagePersistentAction"] = function(e)
    if WG.HidePersistentMessageBox then
      WG.HidePersistentMessageBox()
    end
  end,
  ["ConvoMessageAction"] = function(e)
    if WG.AddConvo then
      local message = Translate(e.stringID, e.message)
      local image, sound
      if e.image then
        image = (e.imageFromArchive and "" or "LuaUI/Images/") .. e.image
      end
      if e.sound then
        sound = (e.soundFromArchive and "" or "LuaUI/Sounds/convo/") .. e.sound
      end
      WG.AddConvo(message, e.fontSize, image, sound, e.time)
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing message box widget for action " .. e.logicType)
    end
  end,
  ["ClearConvoMessageQueueAction"] = function(e)
    if WG.ClearConvoQueue then
      WG.ClearConvoQueue()
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing message box widget for action " .. e.logicType)
    end 
  end,
  ["AddObjectiveAction"] = function(e)
    if WG.AddObjective then
      local title = Translate(e.titleStringID, e.title)
      local desc = Translate(e.stringID, e.description)
      WG.AddObjective(e.id, title, desc, nil, "Incomplete")
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing Objectives widget for action " .. e.logicType)
    end
  end,
  ["ModifyObjectiveAction"] = function(e)
    if WG.ModifyObjective then
      local title = Translate(e.titleStringID, e.title)
      local desc = Translate(e.stringID, e.description)
      WG.ModifyObjective(e.id, title, desc, nil, e.status)
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing Objectives widget for action " .. e.logicType)
    end
  end,
  ["AddUnitsToObjectiveAction"] = function(e)
    if WG.AddUnitOrPosToObjective then
      for unitID in pairs(e.units) do
        WG.AddUnitOrPosToObjective(e.id, unitID)
      end
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing or out-of-date Objectives widget for action " .. e.logicType)
    end
  end,
  ["AddPointToObjectiveAction"] = function(e)
    if WG.AddUnitOrPosToObjective then
      WG.AddUnitOrPosToObjective(e.id, {e.x, e.y})
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing or out-of-date Objectives widget for action " .. e.logicType)
    end
  end,
  ["EnterCutsceneAction"] = function(e)
    if WG.Cutscene and WG.Cutscene.EnterCutscene then
      WG.Cutscene.EnterCutscene(e.instant, e.skippable)
    end
  end,
  ["LeaveCutsceneAction"] = function(e)
    if WG.Cutscene and WG.Cutscene.LeaveCutscene then
      WG.Cutscene.LeaveCutscene(e.instant)
    end
  end,
  ["FadeOutAction"] = function(e)
    if WG.Cutscene and WG.Cutscene.FadeOut then
      WG.Cutscene.FadeOut(e.instant)
    end
  end,
  ["FadeInAction"] = function(e)
    if WG.Cutscene and WG.Cutscene.FadeIn then
      WG.Cutscene.FadeIn(e.instant)
    end
  end,
  ["PauseAction"] = function(e)
    Spring.SendCommands"pause"
  end,
  ["MarkerPointAction"] = function(e)
    local height = Spring.GetGroundHeight(e.x, e.y)
    local text = Translate(e.stringID, e.text)
    Spring.MarkerAddPoint(e.x, height, e.y, text)
    if e.centerCamera then
      Spring.SetCameraTarget(e.x, height, e.y, 1)
    end
  end,
  ["SetCameraPointTargetAction"] = function(e)
    local height = Spring.GetGroundHeight(e.x, e.y)
    Spring.SetCameraTarget(e.x, height, e.y, 1)
  end,
  ["SetCameraPosDirAction"] = function(e)
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
  end,
  ["BeautyShotAction"] = function(e)
    WG.BeautyShot(e.unitID, e)
  end,
  ["SaveCameraStateAction"] = function(e)
    camState = Spring.GetCameraState()
  end,
  ["RestoreCameraStateAction"] = function(e)
    Spring.SetCameraState(camState, 1)
  end,
  ["ShakeCameraAction"] = function(e)
    if WG.ShakeCamera then
      WG.ShakeCamera(e.strength)
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "Missing camera shake widget for action " .. e.logicType)
    end
  end,
  ["SoundAction"] = function(e)
    PlaySound(e.sound)
  end,
  ["MusicAction"] = function(e)
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
  end,
  ["MusicLoopAction"] = function(e)
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
  end,
  ["StopMusicAction"] = function(e)
    if WG.Music and WG.Music.StopTrack then
      WG.Music.StopTrack(e.noContinue)
    else
      Spring.StopSoundStream()
    end
  end,
  ["SunriseAction"] = function(e)
    WG.noonWanted = true
  end,
  ["SunsetAction"] = function(e)
    WG.midnightWanted = true
  end,
  ["CustomAction2"] = function(e)
    -- workaround to make WG accessible to the customAction
    local func, err = loadstring("local WG = ({...})[1]; " .. e.codeStr)
    if err then
      error("Failed to load custom action: ".. e.codeStr)
      return
    end
    func(WG)
  end
}

function MissionEvent(e)
  if actionsTable[e.logicType] then
    actionsTable[e.logicType](e)
  else
    Spring.Log(widget:GetInfo().name, LOG.ERROR, "Unable to find action type " .. e.logicType)
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