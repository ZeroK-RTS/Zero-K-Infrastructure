function widget:GetInfo()
	return {
		name      = "Path test creator",
		desc      = "allows to easily create path test configs",
		author    = "BD",
		license   = "WTFPL",
		layer     = 0,
		enabled   = true
	}
end

local TEST_PATH = "pathTests"

local mapModString =  Game.mapName .. "-".. Game.modShortName .. " " .. Game.modVersion
local configFilePath = TEST_PATH .. "/config/" .. mapModString .. ".lua"

local GetSelectedUnits = Spring.GetSelectedUnits
local GetUnitCommands = Spring.GetUnitCommands
local GetUnitPosition = Spring.GetUnitPosition
local GetUnitDefID = Spring.GetUnitDefID
local RequestPath = Spring.RequestPath
local GetUnitTeam = Spring.GetUnitTeam
local SendCommands = Spring.SendCommands
local GiveOrderToUnit = Spring.GiveOrderToUnit
local Echo = Spring.Echo

local CMD_MOVE = CMD.MOVE
local CMD_FIRE_STATE = CMD.FIRE_STATE

local max = math.max
local ceil = math.ceil

local testScheduled = {}

include("savetable.lua")


function sqDist(posA,posB)
	return (posA[1]-posB[1])^2+(posA[2]-posB[2])^2+(posA[3]-posB[3])^2
end

local function getPathDist(startPos,endPos, moveType, radius)
	local path = RequestPath(moveType or 1, startPos[1],startPos[2],startPos[3],endPos[1],endPos[2],endPos[3], radius)
	if not path then --startpoint is blocked
		startPos,endPos = endPos,startPos
		local path = RequestPath(moveType or 1, startPos[1],startPos[2],startPos[3],endPos[1],endPos[2],endPos[3], radius)
		if not path then
			return --if startpoint and endpoint is blocked path is not calculated
		end
	end
	local dist = 0
	local xyzs, iii = path.GetPathWayPoints(path)
	for i,pxyz in ipairs(xyzs) do
		local endPos = pxyz
		dist = dist + sqDist(endPos,startPos)^0.5
		startPos = endPos
	end
	return dist
end

function addTest(_,_,params)
	local distTollerance = params[1] or 25
	local nextTestDelay = params[2]
	local arrivalTimeout = params[3]
	local testEntry = {}
	local maxSelfdTime = 0
	testEntry.unitList = {}
	testEntry.delayToNextTest = 0
	for unitIndex, unitID in ipairs(GetSelectedUnits()) do
		local unitPos = {GetUnitPosition(unitID)}
		local unitDefID = GetUnitDefID(unitID)
		local unitQueue = GetUnitCommands(unitID)
		local unitDef = UnitDefs[unitDefID]
		local teamID = GetUnitTeam(unitID)
		local moveType, radius, speed,selfDTime = unitDef.moveData.id, unitDef.radius, unitDef.speed or 0, unitDef.selfDCountdown*30
		local previousPos = unitPos
		local donTProcess = false
		local distance = 0
		local destinations = {}
		if unitQueue then
			for pos, command in ipairs(unitQueue) do
				if command.id == CMD_MOVE then
					local wayPointPos = command.params
					for _, coord in ipairs(wayPointPos) do
						coord = ceil(coord)
					end
					distance = distance + (getPathDist(previousPos,wayPointPos, moveType, radius) or 0 )
					previousPos = wayPointPos
					table.insert(destinations,wayPointPos)
				end
			end
		end
		for _, coord in ipairs(unitPos) do
			coord = ceil(coord)
		end
		local unitEntry = {}
		unitEntry.unitName = UnitDefs[unitDefID].name
		unitEntry.startCoordinates = unitPos
		unitEntry.maxTargetDist = distTollerance
		unitEntry.maxTravelTime =  arrivalTimeout or ceil(distance / speed * 30 * 2)
		unitEntry.destinationCoordinates = destinations
		unitEntry.teamID = teamID

		testEntry.delayToNextTest = nextTestDelay or max(testEntry.delayToNextTest,selfDTime*2.2)

		table.insert(testEntry.unitList,unitEntry)
	end

	table.insert(testScheduled,testEntry)
	--save result table to a file
	table.save( testScheduled, LUAUI_DIRNAME .. "/" .. configFilePath )
	Echo("added test " .. #testScheduled)
end

function deleteTest(_,_,params)
	local index = tonumber(params[1])
	if not index then
		Echo("invalid test id")
		return
	end
	if testScheduled[index] then
		Echo("deleted test " .. index)
		testScheduled[index] = nil
		--save result table to a file
		table.save( testScheduled, LUAUI_DIRNAME .. "/" .. configFilePath )
	end
end

function file_exists(name)
	local f = io.open(name,"r")
	if f then
		io.close(f)
		return true
	else
		return false
	end
end

function widget:UnitCreated(unitID,unitDefID, unitTeam)
	--set hold fire
	GiveOrderToUnit(unitID,CMD_FIRE_STATE,{0},{})
end

function widget:Initialize()
	if file_exists(LUAUI_DIRNAME .. "/" .. configFilePath) then
		testScheduled = include(configFilePath)
	end

	widgetHandler:AddAction("addpathtest", addTest, nil, "t")
	widgetHandler:AddAction("delpathtest", deleteTest, nil, "t")

	SendCommands({"cheat 1","godmode 1","globallos","spectator"})
end
