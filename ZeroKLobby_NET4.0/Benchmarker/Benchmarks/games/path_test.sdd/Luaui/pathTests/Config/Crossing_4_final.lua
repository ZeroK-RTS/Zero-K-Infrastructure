local test = {
	{
		delayToNextTest = 300,
		unitList = {},
	},
	{
		delayToNextTest = 300,
		unitList = {},
	},
	{
		delayToNextTest = 300,
		unitList = {},
	},
	{
		delayToNextTest = 300,
		unitList = {},
	},	
}
local MAX_DELAY_FACTOR = 1.75
local stepSize = 128

local units = {
	"corak",
	"corraid",
	"corsh",
	"correap",
	"corak",
	"corraid",
	"corsh",
	"correap",	
}
local x1 = 128
local x2 = Game.mapSizeX - x1

for testNum = 1,#test do
	if units[testNum] and UnitDefNames[units[testNum]] then
		local unitDef = UnitDefNames[units[testNum]]
		local unitList = test[testNum].unitList
		local maxTravelTime = ((x1 - x2))/unitDef.speed * 30 * MAX_DELAY_FACTOR
		if maxTravelTime < 0 then
			maxTravelTime = -maxTravelTime
		end
		
		for z = stepSize, Game.mapSizeZ - stepSize, stepSize do
			local y1 = Spring.GetGroundHeight(x1, z)
			local y2 = Spring.GetGroundHeight(x2, z)
			unitList[#unitList+1] = {
				unitName = units[testNum],
				teamID = 0,
				startCoordinates = {x1, y1, z},
				destinationCoordinates = {{x2, y2, z}},
				maxTargetDist = 40,
				maxTravelTime = maxTravelTime,
			}
		end
		--Spring.Echo("Max travel time for " .. units[testNum] .. ": " .. maxTravelTime)
		--Spring.Log(widget:GetInfo().name, "info", "Max travel time for " .. units[testNum] .. ": " .. maxTravelTime)
	else
		Spring.Log(widget:GetInfo().name, "warning", "invalid unit name for path test")
	end
end

return test