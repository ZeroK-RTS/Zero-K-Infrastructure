function widget:GetInfo()
	return {
		name      = "Path tester",
		desc      = "tests pathfinder changes",
		author    = "BD",
		license   = "WTFPL",
		layer     = 0,
		enabled   = true
	}
end


local TEST_PATH = "pathTests"

local configFilePath = TEST_PATH .. "/config/" .. Game.mapName .. ".lua"
local resultFilePath = LUAUI_DIRNAME .. TEST_PATH .. "/results/" .. Game.mapName .. "-" .. Game.version .. "-" .. os.date("%Y%m%d-%H%M%S") .. ".lua"

local LOG_PREFIX = "path_tester: "

local SendCommands = Spring.SendCommands
local GiveOrderToUnit = Spring.GiveOrderToUnit
local GetGameFrame = Spring.GetGameFrame
local GetUnitPosition = Spring.GetUnitPosition
local GetUnitDirection = Spring.GetUnitDirection
local GetUnitVelocity = Spring.GetUnitVelocity
local Log = function(...)
	Spring.Log(widget:GetInfo().name, ...)
end
local CMD_MOVE = CMD.MOVE
local CMD_SELFD = CMD.SELFD
local CMD_FIRE_STATE = CMD.FIRE_STATE
include("savetable.lua")

local testScheduled = {}
local currentTest = 1
local testInitialized = false
local testResults = {}
local remapUnitIDToInternalIndex = {}
local delayNextTest

function widget:Initialize()
	Spring.Echo(LOG_PREFIX .. ": loading config " .. LUAUI_DIRNAME .. configFilePath)
	if VFS.FileExists(LUAUI_DIRNAME .. configFilePath) then
		testScheduled = include(configFilePath)
	else
		--nothing to be done, close spring
		Log("warning", LOG_PREFIX .. "missing config file")
		return
	end
	--enable cheats
	SendCommands({"cheat 1","godmode 1","globallos","spectator"})
end

function widget:GameStart()
	for _, unitID in ipairs(Spring.GetAllUnits()) do
		GiveOrderToUnit(unitID,CMD_SELFD,{},{}) --get rid of initial units
	end
end

function sqDist(posA,posB)
	return (posA[1]-posB[1])^2+(posA[2]-posB[2])^2+(posA[3]-posB[3])^2
end

function widget:GameFrame(frame)
	if delayNextTest and frame < delayNextTest then
		--wait for units of previous test to finish to selfD
		return
	end
	delayNextTest = nil
	local loadedTest = testScheduled[currentTest]
	if not loadedTest then
		--done executing tests
		--close spring
		Log("info",LOG_PREFIX .. "test session finished")
		SendCommands("quit")
		return
	end
	if not testInitialized then
		--initialize result test table
		Log("info", LOG_PREFIX .. "starting test session " .. currentTest)
		testResults[currentTest] = {}
		--spawn all units
		for testUnitID, unitInfoData in ipairs(loadedTest.unitList) do
			local spawnPoint = unitInfoData.startCoordinates
			local spawnString = string.format("give %s @%d,%d,%d",unitInfoData.unitName,spawnPoint[1],spawnPoint[2],spawnPoint[3] )
			if unitInfoData.teamID then
				spawnString = string.format("give %s %d @%d,%d,%d",unitInfoData.unitName,unitInfoData.teamID,spawnPoint[1],spawnPoint[2],spawnPoint[3] )
			end
			SendCommands(spawnString)
			--we don't know the unitID yet, we'll issue the move orders in UnitCreated
		end
		testInitialized = true
		return
	end
	local testEnded = true
	for testUnitID, unitInfoData in ipairs(loadedTest.unitList) do
		-- check for units @ destination or timeout
		local unitID = remapUnitIDToInternalIndex[testUnitID]
		if not unitID then -- unit not spawned yet
			testEnded = false
		else
			if not testResults[currentTest][testUnitID] then
				local unitPosition = {GetUnitPosition(unitID)}
				local unitFacing = {GetUnitDirection(unitID)}
				local unitVelocity = {GetUnitVelocity(unitID)}
				local targetDistance = 0
				local finalDestination = unitInfoData.destinationCoordinates[#unitInfoData.destinationCoordinates]
				if finalDestination then
					targetDistance = sqDist(unitPosition,finalDestination)^0.5
				end
				local atTarget = targetDistance < (unitInfoData.maxTargetDist or 1)
				local travelTime = frame - unitInfoData.startTime
				local timeout = not atTarget and travelTime >= (unitInfoData.maxTravelTime or 1)
				if not timeout and not atTarget  then
					testEnded = false
				else
					if timeout then
						Log("error",LOG_PREFIX .. "unit move timeout (test "  .. currentTest .. ", unit " .. testUnitID .. ")" )
					end
					if atTarget then
						Log("info",LOG_PREFIX .. "unit reached destination (test "  .. currentTest .. ", unit " .. testUnitID .. ")" )
					end
					local unitResult = {}
					unitResult.position = unitPosition
					unitResult.travelTime = travelTime
					unitResult.timeout = timeout
					unitResult.died = false
					unitResult.atTarget = atTarget
					unitResult.died = died
					unitResult.direction = unitFacing
					unitResult.velocity = unitVelocity
					unitResult.targetDistance = targetDistance
					testResults[currentTest][testUnitID] = unitResult
					
					testResults[currentTest].overall = testResults[currentTest].overall or {count = 0, travelTime = 0, atTarget = 0}
					local overallResults = testResults[currentTest].overall
					if atTarget then
						overallResults.travelTime = ((overallResults.travelTime * overallResults.atTarget) + travelTime)/(overallResults.atTarget + 1)
						overallResults.atTarget = overallResults.atTarget + 1
					end
					overallResults.count = overallResults.count + 1
				end
			end
		end
	end

	if testEnded then
		--kill all units
		Log("info",LOG_PREFIX .. "finished test session " .. currentTest)
		for testUnitID, unitInfoData in ipairs(loadedTest.unitList) do
			local unitID = remapUnitIDToInternalIndex[testUnitID]
			GiveOrderToUnit(unitID,CMD_SELFD,{},{})
		end
		Spring.Echo(LOG_PREFIX .. ": saving results to " .. resultFilePath)
		table.save( testResults, resultFilePath )
		--set delay for next test
		delayNextTest = frame + loadedTest.delayToNextTest
		--reset unitID table
		remapUnitIDToInternalIndex = {}
		--set pointer to next test
		currentTest = currentTest + 1
		testInitialized = false
	end
end

function widget:UnitDestroyed(unitID,unitDefID, unitTeam)
	local testUnitID
	for unitIndex, unitConvertID in ipairs(remapUnitIDToInternalIndex) do
		if unitConvertID == unitID then
			testUnitID = unitIndex
		end
	end
	if testUnitID then
		local unitInfoData = testScheduled[currentTest].unitList[testUnitID]
		local unitPosition = {GetUnitPosition(unitID)}
		local unitFacing = {GetUnitDirection(unitID)}
		local unitVelocity = {GetUnitVelocity(unitID)}
		local targetDistance = 0
		local finalDestination = unitInfoData.destinationCoordinates[#unitInfoData.destinationCoordinates]
		if finalDestination then
			targetDistance = sqDist(unitPosition,finalDestination)^0.5
		end
		local atTarget = targetDistance < (unitInfoData.maxTargetDist or 1)
		local travelTime = GetGameFrame() - unitInfoData.startTime
		local timeout = not atTarget and travelTime >= (unitInfoData.maxTravelTime or 1)
		local unitResult = {}
		unitResult.position = unitPosition
		unitResult.travelTime = travelTime
		unitResult.timeout = timeout
		unitResult.atTarget = atTarget
		unitResult.died = true
		unitResult.direction = unitFacing
		unitResult.velocity = unitVelocity
		unitResult.targetDistance = targetDistance
		testResults[currentTest][testUnitID] = unitResult
		Log("error",LOG_PREFIX .. "unit died (test "  .. currentTest .. ", unit " .. testUnitID .. ")" )
	end
end

function widget:UnitCreated(unitID,unitDefID, unitTeam)
	local unitIndex = #remapUnitIDToInternalIndex +1
	local unitInfoData = testScheduled[currentTest].unitList[unitIndex]
	if not unitInfoData then
		return
	end
	remapUnitIDToInternalIndex[unitIndex] = unitID
	--set the start timer
	testScheduled[currentTest].unitList[unitIndex].startTime = GetGameFrame()
	--set hold fire
	GiveOrderToUnit(unitID,CMD_FIRE_STATE,{0},{})
	--give move order
	for _, movePos in ipairs(unitInfoData.destinationCoordinates) do
		local wayPointPos = {movePos[1],movePos[2],movePos[3]}
		GiveOrderToUnit(unitID,CMD_MOVE,wayPointPos,{"shift"})
	end
end
