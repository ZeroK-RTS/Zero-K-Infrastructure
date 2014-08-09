--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Cutscenes",
    desc      = "v0.6 Letterboxes and camera control for cutscenes",
    author    = "KingRaptor (L.J. Lim)",
    date      = "2012.10.25",
    license   = "GNU GPL, v2 or later",
    layer     = -1*10^7,
    enabled   = true  --  loaded by default?
  }
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

include("keysym.h.lua")

local spWarpMouse = Spring.WarpMouse
local spSetMouseCursor = Spring.SetMouseCursor
local spGetMapDrawMode = Spring.GetMapDrawMode
local spGetCameraState = Spring.GetCameraState
local glColor = gl.Color
local glRect = gl.Rect

--Spring.Utilities = Spring.Utilities or {}
--VFS.Include("LuaRules/Utilities/math.lua")

local LETTERBOX_ENTER_SPEED = 0.1  -- % of screen height/s
local LETTERBOX_LEAVE_SPEED = 0.1
local LETTERBOX_BOUNDARY = 0.15
local FADE_SPEED = 0.5  -- 50% faded out after 1s
local UPDATE_PERIOD = 0.05
local RADIANS_TO_DEGREES = 180/math.pi

local drawListCamScreen

local vsx, vsy = gl.GetViewSizes()
local isInCutscene = false
local isEnteringCutscene = false  -- transition into cutscene
local isExitingCutscene = false   -- transition out of cutscene
local letterboxPos = 0
local isFadingIn = false
local isFadingOut = false
local screenFadeAlpha = 0
local showCameraScreen = false
local isSkippable = false
local displaySkipMessage = false
local camState = spGetCameraState()
local posCamDataX = vsx*0.85
local posCamDataY = vsy*0.3

local lastDrawMode = "normal"
local lastIconDist = Spring.GetConfigInt("UnitIconDist", 150)
local useEdgeScroll = Spring.GetConfigInt("WindowedEdgeMove", 1)
local DRAW_MODE_COMMANDS = {
  height = "showelevation",
  metal = "showmetalmap",
  pathTraversability = "showpathtraversability",
  los = "togglelos",
}

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
local function EnterCutscene(instant, skippable)
  local wasInCutscene = isInCutscene
  isInCutscene = true
  isSkippable = skippable
  spWarpMouse(vsx/2, vsy/2)
  WG.HideGUI()
  local paused = select(3, Spring.GetGameSpeed())
  if paused then
    Spring.SendCommands("pause")
  end
  Spring.SelectUnitArray({})
  
  -- set view settings to remove gameplay-oriented stuff
  if not wasInCutscene then
    lastIconDist = Spring.GetConfigInt("UnitIconDist", 150)
    Spring.SendCommands("disticon " .. 1000)
    useEdgeScroll = Spring.GetConfigInt("WindowedEdgeMove", 1)
    Spring.SetConfigInt( "WindowedEdgeMove", 0 )
    local drawMode = spGetMapDrawMode()
    if drawMode ~= "normal" then
      local cmd = DRAW_MODE_COMMANDS[drawMode]
      Spring.SendCommands(cmd)
      lastDrawMode = drawMode
    end
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
    local cmd = DRAW_MODE_COMMANDS[lastDrawMode]
    Spring.SendCommands(cmd)
  end
  Spring.SendCommands("disticon " .. lastIconDist)
  Spring.SetConfigInt( "WindowedEdgeMove", useEdgeScroll )
  WG.UnhideGUI()
end

local function LeaveCutscene(instant)
  isSkippable = false
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

local function DrawListCameraScreen(crosshairSize)
  gl.Vertex(vsx*0.05, vsy*0.55, 0)
  gl.Vertex(vsx*0.05, vsy*0.8, 0)
  gl.Vertex(vsx*0.05, vsy*0.8, 0)
  gl.Vertex(vsx*0.45, vsy*0.8, 0)
  
  gl.Vertex(vsx*0.95, vsy*0.55, 0)
  gl.Vertex(vsx*0.95, vsy*0.8, 0)
  gl.Vertex(vsx*0.95, vsy*0.8, 0)
  gl.Vertex(vsx*0.55, vsy*0.8, 0)
  
  gl.Vertex(vsx*0.95, vsy*0.45, 0)
  gl.Vertex(vsx*0.95, vsy*0.2, 0)
  gl.Vertex(vsx*0.95, vsy*0.2, 0)
  gl.Vertex(vsx*0.55, vsy*0.2, 0)
  
  gl.Vertex(vsx*0.05, vsy*0.45, 0)
  gl.Vertex(vsx*0.05, vsy*0.2, 0)
  gl.Vertex(vsx*0.05, vsy*0.2, 0)
  gl.Vertex(vsx*0.45, vsy*0.2, 0)
  
  gl.Vertex(vsx/2 - crosshairSize, vsy/2, 0)
  gl.Vertex(vsx/2 + crosshairSize, vsy/2, 0)
  gl.Vertex(vsx/2, vsy/2 + crosshairSize, 0)
  gl.Vertex(vsx/2, vsy/2 - crosshairSize, 0)
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
        if WG.IsGUIHidden() then
          --spWarpMouse(vsx/2, vsy/2)
        end
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
      
      if showCameraScreen then
        camState = spGetCameraState()
        camState.px, camState.py, camState.pz = Spring.GetCameraPosition()
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

local function HandlePause(guiHidden)
  local paused = select(3, Spring.GetGameSpeed())
  
  Spring.SelectUnitMap({})
  if (guiHidden and (not paused)) then
    WG.UnhideGUI()
    Spring.SendCommands("pause")
  elseif ((not guiHidden) and paused) then
    WG.HideGUI()
    if Spring.IsGUIHidden() then
      Spring.SendCommands("hideinterface")
    end
    Spring.SendCommands("pause")
  elseif guiHidden and paused then
    WG.UnhideGUI()
  elseif (not guiHidden) and (not paused) then
    Spring.SendCommands("pause")
  end
end

local function SetDrawCameraScreen(bool)
  showCameraScreen = bool
end

local function DrawCameraScreen()
  gl.LineWidth(3)
  gl.Color(1,1,1,1)
  gl.CallList(drawListCamScreen)
  gl.Text("X\t\t"..math.ceil(camState.px + 0.5), posCamDataX,posCamDataY, 12, "s")
  gl.Text("Y\t\t"..math.ceil(camState.py + 0.5), posCamDataX,posCamDataY-14, 12, "s")
  gl.Text("Z\t\t"..math.ceil(camState.pz + 0.5), posCamDataX,posCamDataY-28, 12, "s")
  gl.Text("RX\t"..math.ceil(camState.rx*RADIANS_TO_DEGREES + 0.5), posCamDataX,posCamDataY-42, 12, "s")
  gl.Text("RY\t"..math.ceil(camState.ry*RADIANS_TO_DEGREES + 0.5), posCamDataX,posCamDataY-56, 12, "s")
  gl.LineWidth(1)
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
  WG.Cutscene.FadeIn = FadeIn
  WG.Cutscene.FadeOut = FadeOut
  WG.Cutscene.SetDrawCameraScreen = SetDrawCameraScreen
  
  WG.Cutscene.IsInCutscene = function() return isInCutscene end
  
  --WG.AddNoHideWidget(self)
  
  drawListCamScreen = gl.CreateList(gl.BeginEnd, GL.LINES, DrawListCameraScreen, vsy*0.05)
end

function widget:Shutdown()
  if WG.UnhideGUI then
    --WG.RemoveNoHideWidget(self)
    WG.UnhideGUI()
  end
  
  Spring.SendCommands("disticon " .. lastIconDist)
  if lastDrawMode ~= "normal" then
    local cmd = DRAW_MODE_COMMANDS[lastDrawMode]
    Spring.SendCommands(cmd)
  end
  
  gl.DeleteList(drawListCamScreen)
  WG.Cutscene = nil
end

-- block all keypresses that aren't pause or Esc while in cutscene
function widget:KeyPress(key, modifier, isRepeat)
  local guiHidden = WG.IsGUIHidden()
  if isInCutscene and (Spring.GetGameFrame() > 0) then
    if key == KEYSYMS.PAUSE or key == KEYSYMS.ESCAPE then
      HandlePause(guiHidden)
      return false
    elseif key == KEYSYMS.SPACE and isSkippable then
      return true -- pass to KeyRelease
    end
    
    -- allow screenshots
    local keystr = Spring.GetKeySymbol(key)
    local keybinds = Spring.GetKeyBindings(keystr) or {}
    for i=1,#keybinds do
      for key in pairs(keybinds[i]) do
        local lowerkey = key:lower()
        if lowerkey == "screenshot" then --or key:lower() == "hideinterface"
          return false
        elseif lowerkey == "crudemenu" or lowerkey == "pause" then
          HandlePause(guiHidden)
          return false
        end
      end
    end
    
    return guiHidden -- eat the keypress if appropriate (so nobody can use it)
  end
  return false
end

function widget:KeyRelease(key, modifier, isRepeat)
  if key == KEYSYMS.SPACE and isSkippable then
    if displaySkipMessage or (not WG.IsGUIHidden()) then
      Spring.SendLuaRulesMsg("skipCutscene")
      displaySkipMessage = false
      return true
    else
      displaySkipMessage = true
    end
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
    if showCameraScreen then
      DrawCameraScreen()
    end
  end
  if screenFadeAlpha > 0 then
    glColor(0,0,0,screenFadeAlpha)
    glRect(0, vsy, vsx, 0)
    glColor(1,1,1,1)
  end
  if isInCutscene and isSkippable and (displaySkipMessage or (not WG.IsGUIHidden()) ) then
    gl.Translate(vsx * 0.75, vsy * LETTERBOX_BOUNDARY/2, 0)
    gl.Text("SPACE: Skip", 0, 0, 20, "O")
  end  
end

function widget:ViewResize(viewSizeX, viewSizeY)
  vsx, vsy = viewSizeX, viewSizeY
  gl.DeleteList(drawListCamScreen)
  drawListCamScreen = gl.CreateList(gl.BeginEnd, GL.LINES, DrawListCameraScreen, vsy*0.05)
  posCamDataX = vsx*0.85
  posCamDataY = vsy*0.3
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