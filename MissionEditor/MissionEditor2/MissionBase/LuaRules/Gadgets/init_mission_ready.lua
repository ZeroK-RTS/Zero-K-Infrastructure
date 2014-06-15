local singleplayer = false
do
	local playerlist = Spring.GetPlayerList() or {}
	if (#playerlist <= 1) then
		singleplayer = true
	end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
function gadget:GetInfo()
  return {
    name      = "Mission Ready",
    desc      = "Ready screen for missons",
    author    = "Licho",
    date      = "15.4.2012",
    license   = "Nobody can do anything except me, Microsoft and Apple! Thieves hands off",
    layer     = 0,
    enabled   = singleplayer  --  loaded by default?
  }
end
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

if (not gadgetHandler:IsSyncedCode()) then

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
local glPopMatrix      = gl.PopMatrix
local glPushMatrix     = gl.PushMatrix
local glRotate         = gl.Rotate
local glScale          = gl.Scale
local glText           = gl.Text
local glTranslate      = gl.Translate

local yOffset = isMission and 50 or 150

local missionReady = false

function gadget:KeyPress()
	if missionReady == false then
		missionReady = true
		Spring.SendCommands("forcestart")
		return true
	end
	return false
end

function gadget:MousePress()
	if isMission and missionReady == false then
		missionReady = true
		Spring.SendCommands("forcestart")
		return true
	end
	return false
end

function gadget:GameSetup(label, ready, playerStates)
	lastLabel = label
	return true, missionReady
end

function gadget:DrawScreen() 
	local vsx, vsy = gl.GetViewSizes()
	local text = lastLabel
	if not missionReady then
		text = "Press any key to begin"
	end

	glPushMatrix()
	glTranslate((vsx * 0.5), (vsy * 0.5)+yOffset, 0)
	glScale(1.5, 1.5, 1)
	glText(text, 0, 0, 14, "oc")
	glPopMatrix()
end 

function gadget:Update() 
	if (Spring.GetGameFrame() > 1) then 
		gadgetHandler:RemoveGadget()
	end 
end 

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
end 