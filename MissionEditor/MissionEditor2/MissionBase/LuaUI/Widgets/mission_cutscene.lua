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

local spWarpMouse = Spring.WarpMouse
local spSetMouseCursor = Spring.SetMouseCursor
local spGetMapDrawMode = Spring.GetMapDrawMode
local glColor = gl.Color
local glRect = gl.Rect

local LETTERBOX_ENTER_SPEED = 0.1  -- % of screen height/s
local LETTERBOX_LEAVE_SPEED = 0.1
local LETTERBOX_BOUNDARY = 0.15
local FADE_SPEED = 0.5  -- 50% faded out after 1s
local UPDATE_PERIOD = 0.05

local vsx, vsy = gl.GetViewSizes()
local isInCutscene = false
local isEnteringCutscene = false  -- transition into cutscene
local isExitingCutscene = false   -- transition out of cutscene
local letterboxPos = 0
local isFadingIn = false
local isFadingOut = false
local screenFadeAlpha = 0

local lastDrawMode = "normal"
local lastIconDist = 150
local DRAW_MODE_COMMANDS = {
  height = "showelevation",
  metal = "showmetalmap",
  pathTraversability = "showpathtraversability",
  los = "togglelos",
}

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
local function EnterCutscene(instant)
  isInCutscene = true
  WG.HideGUI()
  local paused = select(3, Spring.GetGameSpeed())
  if paused then
    Spring.SendCommands("pause")
  end
  Spring.SelectUnitArray({})
  
  -- set view settings to remove gameplay-oriented stuff
  lastIconDist = Spring.GetConfigInt("UnitIconDist", 150)
  Spring.SendCommands("disticon " .. 1000)
  local drawMode = spGetMapDrawMode()
  if drawMode ~= "normal" then
    local cmd = DRAW_MODE_COMMANDS[drawMode]
    lastDrawMode = drawMode
  end
  
  if instant then
    letterboxPos = LETTERBOX_BOUNDARY
    isEnteringCutscene = false
    isExitingCutscene = false
  else
    isEnteringCutscene = true
    isExitingCutscene = false
  end
end

-- restore user's view settings as needed; unhide GUI
local function RestoreFromCutscene()
  letterboxPos = 0
  isInCutscene = false
  isExitingCutscene = false
  if lastDrawMode ~= "normal" then
    Spring.SendCommands(DRAW_MODE_COMMANDS[lastDrawMode])
  end
  Spring.SendCommands("disticon " .. lastIconDist)
  WG.UnhideGUI()
end

local function LeaveCutscene(instant)
  if instant then
    RestoreFromCutscene()
  else
    isExitingCutscene = true
    isEnteringCutscene = false
  end
end

local function ProgressCutsceneEntrance(dt)
  letterboxPos = letterboxPos + LETTERBOX_LEAVE_SPEED*dt
  if letterboxPos >= LETTERBOX_BOUNDARY then
    letterboxPos = LETTERBOX_BOUNDARY
    isEnteringCutscene = false
  end
end

local function ProgressCutsceneExit(dt)
  letterboxPos = letterboxPos - LETTERBOX_LEAVE_SPEED*dt
  
  if letterboxPos <= 0 then
    RestoreFromCutscene()
  end
end

-- controls fade-out/fade-in/letterboxing progression; hides and locks mouse
local timer = 0
function widget:Update(dt)
    if isInCutscene and WG.IsGUIHidden() then
      spSetMouseCursor('none')
    end
    timer = timer + dt
    if timer > UPDATE_PERIOD then
      if isInCutscene then
        --if WG.IsGUIHidden() then
        --  spWarpMouse(vsx/2, vsy/2)
        --end
        if isExitingCutscene then
          ProgressCutsceneExit(timer)
        elseif isEnteringCutscene then
          ProgressCutsceneEntrance(timer)
        end
      end
      if isFadingIn then
        screenFadeAlpha = screenFadeAlpha - FADE_SPEED*timer
        if screenFadeAlpha <= 0 then
          screenFadeAlpha = 0
          isFadingIn = false
        end
      elseif isFadingOut then
        screenFadeAlpha = screenFadeAlpha + FADE_SPEED*timer
        if screenFadeAlpha >= 1 then
          screenFadeAlpha = 1
          isFadingOut = false
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

local function FadeOut(instant)
  if instant then
    screenFadeAlpha = 1
  else
    isFadingOut = true
    isFadingIn = false
  end
end

local function FadeIn(instant)
  if instant then
    screenFadeAlpha = 0
  else
    isFadingIn = true
    isFadingOut = false
  end
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
  
  WG.Cutscene.FadeIn = FadeIn
  WG.Cutscene.FadeOut = FadeOut
  --WG.AddNoHideWidget(self)
end

function widget:Shutdown()
  if WG.UnhideGUI then
    --WG.RemoveNoHideWidget(self)
    WG.UnhideGUI()
  end
  WG.Cutscene = nil
end

-- block all keypresses that aren't pause or Esc while in cutscene
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

-- draw letterbox/blackout effects
function widget:DrawScreenEffects()
  if isInCutscene then
    glColor(0,0,0,1)
    glRect(0, vsy*(1-letterboxPos), vsx, vsy)  --top letterbox
    glRect(0, 0, vsx, vsy*letterboxPos)  --bottom letterbox
    glColor(1,1,1,1)
  end
  if screenFadeAlpha > 0 then
    glColor(0,0,0,screenFadeAlpha)
    glRect(0, vsy, vsx, 0)
    glColor(1,1,1,1)
  end
end

function widget:ViewResize(viewSizeX, viewSizeY)
  vsx, vsy = viewSizeX, viewSizeY
end

-- block commands while in cutscene
function widget:CommandNotify()
  if isInCutscene and WG.IsGUIHidden() then
    return true
  end
  return false
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------