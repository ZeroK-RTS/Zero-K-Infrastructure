function widget:GetInfo()
	return {
		name      = "GUI Hide Hax",
		desc      = "Allows hiding of non-registered GUI elements",
		author    = "KingRaptor (L.J. Lim)",
		date      = "2012.10.26",
		license   = "GNU GPL v2",
		layer     = 0,
		enabled   = true,
		handler   = true,
		api     = true,
	}
end

local noHide = {}
local layers = {}	-- [widget] == layer
local hidden = false

local function HideGUI()
	if hidden then return end
	
	hidden = true
	Spring.SendCommands("mapmarks 0")
	
	-- hook widgetHandler to allow us to override the DrawScreen callin
	local wh = widgetHandler
	wh.oldDrawScreenList = wh.DrawScreenList
	wh.DrawScreenList = noHide
end

local function UnhideGUI()
	if not hidden then return end
	hidden = false
	Spring.SendCommands("mapmarks 1")
	
	local wh = widgetHandler
	wh.DrawScreenList = wh.oldDrawScreenList
end

local function AddNoHide(wdgt)
	-- sort by layer
	local layer = wdgt:GetInfo().layer
	layers[wdgt] = layer
	local index = #noHide + 1
	for i=1,#noHide do
		if layer < layers[noHide[i]] then
			index = i
		end
	end
	table.insert(noHide, index, wdgt)
	--Spring.Echo("no hide", #noHide, index)
end

local function RemoveNoHide(wdgt)
	for i=1,#noHide do
		if noHide[i] == wdgt then
			table.remove(noHide, index)
			break
		end
	end
	local wh = widgetHandler
	layers[wdgt] = nil
end

function widget:Initialize()
	WG.HideGUI = HideGUI
	WG.UnhideGUI = UnhideGUI
	WG.AddNoHideWidget = AddNoHide
	WG.RemoveNoHideWidget = RemoveNoHide
	WG.IsGUIHidden = function() return hidden end
end


function widget:Shutdown()
	UnhideGUI()

	WG.HideGUI = nil
	WG.UnhideGUI = nil
	WG.AddNoHideWidget = nil
	WG.RemoveNoHideWidget = nil
	WG.IsGUIHidden = nil
end