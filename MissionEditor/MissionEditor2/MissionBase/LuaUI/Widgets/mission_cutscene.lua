--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Cutscenes",
    desc      = "v0.1 Letterboxes and camera control for cutscenes",
    author    = "KingRaptor (L.J. Lim)",
    date      = "2012.10.25",
    license   = "GNU GPL, v2 or later",
    layer     = -12,
    enabled   = true  --  loaded by default?
  };
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
include("keysym.h.lua")

local LETTERBOX_ENTER_SPEED = 0.1  -- % of screen height/s
local LETTERBOX_LEAVE_SPEED = 0.1
local LETTERBOX_BOUNDARY = 0.15
local UPDATE_PERIOD = 0.05

local vsx, vsy = gl.GetViewSizes()
local isInCutscene = false
local isEnteringCutscene = false  -- transition into cutscene
local isExitingCutscene = false   -- transition out of cutscene
local letterboxPos = 0
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
local function EnterCutscene()
  isInCutscene = true
  isEnteringCutscene = true
  isExitingCutscene = false
  if not Spring.IsGUIHidden() then
    Spring.SendCommands("HideInterface")
  end
  local paused = select(3, Spring.GetGameSpeed())
  if paused then
    Spring.SendCommands("pause")
  end
end

local function LeaveCutscene()
  isExitingCutscene = true
  isEnteringCutscene = false
end

local function ProgressCutsceneEntrance()
  letterboxPos = letterboxPos + LETTERBOX_LEAVE_SPEED*UPDATE_PERIOD
  if letterboxPos > LETTERBOX_BOUNDARY then
    letterboxPos = LETTERBOX_BOUNDARY
    isEnteringCutscene = false
  end
end

local function ProgressCutsceneExit()
  letterboxPos = letterboxPos - LETTERBOX_LEAVE_SPEED*UPDATE_PERIOD
  
  if letterboxPos == 0 then
    isInCutscene = false
    isExitingCutscene = false
    if Spring.IsGUIHidden() then
      Spring.SendCommands("HideInterface")
    end
  end
end

local timer = 0
--local timer2 = 0
function widget:Update(dt)
    timer = timer + dt
    --timer2 = timer2 + dt
    if timer > UPDATE_PERIOD then
      if isInCutscene then
        if isExitingCutscene then
          ProgressCutsceneExit()
        elseif isEnteringCutscene then
          ProgressCutsceneEntrance()
        end
      end
      timer = 0
    end
    -- testing
    --if timer2 > 5 then
    --  LeaveCutscene()
    --  timer2 = 0
    --end
end

function widget:Initialize()
  WG.Cutscene = WG.Cutscene or {}
  WG.Cutscene.Enter = EnterCutscene
  WG.Cutscene.Leave = LeaveCutscene
  
  WG.Cutscene.IsInCutscene = function() return isInCutscene end
  
  --test
  EnterCutscene()
end

function widget:Shutdown()
  if Spring.IsGUIHidden() then
    Spring.SendCommands("HideInterface")
  end
  
  WG.Cutscene = nil
end

function widget:KeyPress(key, modifier, isRepeat)
  if isInCutscene then
    local guiHidden = Spring.IsGUIHidden()
    local paused = select(3, Spring.GetGameSpeed())
    if key == KEYSYMS.PAUSE or key == KEYSYMS.ESCAPE then
      Spring.SelectUnitMap({})
      if (guiHidden and (not paused)) or ((not guiHidden) and paused) then
        Spring.SendCommands({"HideInterface", "pause"})
      elseif guiHidden and paused then
        Spring.SendCommands("HideInterface")
      elseif (not guiHidden) and (not paused) then
        Spring.SendCommands("pause")
      end
      return false
    elseif key == KEYSYMS.F5 then
      return true
    end
    return guiHidden -- eat the keypress
  end
  return false
end

function widget:MousePress(x,y,button)
  if isInCutscene and Spring.IsGUIHidden() then
    return true -- eat the mousepress
  end
  return false
end

function widget:MouseWheel()
  if isInCutscene and Spring.IsGUIHidden() then
    return true
  end
  return false
end

function widget:DrawScreenEffects()
  if isInCutscene then
    gl.Color(0,0,0,1)
    gl.Rect(0, vsy*(1-letterboxPos), vsx, vsy)  --top letterbox
    gl.Rect(0, 0, vsx, vsy*letterboxPos)  --bottom letterbox
    gl.Color(1,1,1,1)
  end
end

function widget:ViewResize(viewSizeX, viewSizeY)
  vsx, vsy = viewSizeX, viewSizeY
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------