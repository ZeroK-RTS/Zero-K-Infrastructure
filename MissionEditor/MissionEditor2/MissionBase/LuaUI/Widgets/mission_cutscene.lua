--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Cutscenes",
    desc      = "v0.1 Letterboxes and camera control for cutscenes",
    author    = "KingRaptor (L.J. Lim)",
    date      = "2012.10.25",
    license   = "GNU GPL, v2 or later",
    layer     = -math.huge,
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
  WG.HideGUI()
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
  if letterboxPos >= LETTERBOX_BOUNDARY then
    letterboxPos = LETTERBOX_BOUNDARY
    isEnteringCutscene = false
  end
end

local function ProgressCutsceneExit()
  letterboxPos = letterboxPos - LETTERBOX_LEAVE_SPEED*UPDATE_PERIOD
  
  if letterboxPos <= 0 then
    letterboxPos = 0
    isInCutscene = false
    isExitingCutscene = false
    WG.UnhideGUI()
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
  if not WG.IsGUIHidden then
    Spring.Log(widget:GetInfo().name, LOG.ERROR, "Cutscenes cannot work without GUI-hiding API. Shutting down...")
    widgetHandler:RemoveWidget()
    return
  end

  WG.Cutscene = WG.Cutscene or {}
  WG.Cutscene.EnterCutscene = EnterCutscene
  WG.Cutscene.LeaveCutscene = LeaveCutscene
  
  WG.Cutscene.IsInCutscene = function() return isInCutscene end
  
  --WG.AddNoHideWidget(self)
end

function widget:Shutdown()
  if WG.UnhideGUI then
    --WG.RemoveNoHideWidget(self)
    WG.UnhideGUI()
  end
  WG.Cutscene = nil
end

function widget:KeyPress(key, modifier, isRepeat)
  if isInCutscene then
    local guiHidden = WG.IsGUIHidden()
    local paused = select(3, Spring.GetGameSpeed())
    if key == KEYSYMS.PAUSE or key == KEYSYMS.ESCAPE then
      Spring.SelectUnitMap({})
      if (guiHidden and (not paused)) then
        WG.UnhideGUI()
        Spring.SendCommands("pause")
      elseif ((not guiHidden) and paused) then
        WG.HideGUI()
        Spring.SendCommands("pause")
      elseif guiHidden and paused then
        WG.UnhideGUI()
      elseif (not guiHidden) and (not paused) then
        Spring.SendCommands("pause")
      end
      return false
    elseif key == KEYSYMS.F5 then
      return true
    end
    return guiHidden -- eat the keypress if appropriate (so nobody can use it)
  end
  return false
end

function widget:MousePress(x,y,button)
  if isInCutscene and WG.IsGUIHidden() then
    return true -- eat the mousepress
  end
  return false
end

function widget:MouseWheel()
  if isInCutscene and WG.IsGUIHidden() then
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