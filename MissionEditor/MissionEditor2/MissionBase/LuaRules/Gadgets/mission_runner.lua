--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function gadget:GetInfo()
  return {
    name      = "Mission Runner",
    desc      = "Runs missions built with the mission editor",
    author    = "quantum",
    date      = "Sept 03, 2008",
    license   = "GPL v2 or later",
    layer     = 0,
    enabled   = true --  loaded by default?
  }
end


local callInList = { -- events forwarded to unsynced
  "UnitFinished",
}

VFS.Include("savetable.lua")
local magic = "--mt\r\n"
local SAVE_FILE = "mission.lua"
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--
-- SYNCED
--
if (gadgetHandler:IsSyncedCode()) then 
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
VFS.Include("LuaRules/Configs/mission_config.lua")

local mission = VFS.Include("mission.lua")
local triggers = mission.triggers -- array
local allTriggers = {unpack(triggers)} -- we'll never remove triggers from here, so the indices will stay correct
local unitGroups = {} -- key: unitID, value: group set (an array of strings)
local cheatingWasEnabled = false
local scores = {}
local gameStarted = false
local currCutsceneID
local currCutsceneIsSkippable
local events = {} -- key: frame, value: event array
local counters = {} -- key: name, value: count
local countdowns = {} -- key: name, value: frame
local displayedCountdowns = {} -- key: name
local lastFinishedUnits = {} -- key: teamID, value: unitID
local allowTransfer = false
local factoryExpectedUnits = {} -- key: factoryID, value: {unitDefID, groups: group set}
local repeatFactoryGroups = {} -- key: factoryID, value: group set
local objectives = {}	-- [index] = {id, title, description, color, unitsOrPositions = {}}	-- important: widget must be able to access on demand
local unitsWithObjectives = {}
local wantUpdateDisabledUnits = false

_G.displayedCountdowns = displayedCountdowns
_G.factoryExpectedUnits = factoryExpectedUnits
_G.repeatFactoryGroups = repeatFactoryGroups

GG.mission = {
  scores = scores,
  counters = counters,
  countdowns = countdowns,
  unitGroups = unitGroups,
  triggers = triggers,
  allTriggers = allTriggers,
  objectives = objectives,
  cheatingWasEnabled = cheatingWasEnabled,
  allowTransfer = allowTransfer,
}
_G.mission = GG.mission

for _, counter in ipairs(mission.counters) do
  counters[counter] = 0
end

local shiftOptsTable = {"shift"}

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function TakeRandomI(t)
  return t[math.random(#t)]
end


local function CopyTable(original)   -- Warning: circular table references lead to an infinite loop.
  local copy = {}
  for k, v in pairs(original) do
    if (type(v) == "table") then
      copy[k] = CopyTable(v)
    else
      copy[k] = v
    end
  end
  return copy
end


local function ArrayToSet(array)
  local set = {}
  for i=1, #array do
    set[array[i]] = true
  end
  return set
end


local function DoSetsIntersect(set1, set2)
  for key in pairs(set1) do
    if set2[key] then
      return true
    end
  end
  return false
end


local function MergeSets(set1, set2)
  local result = {}
  for key in pairs(set1) do
    result[key] = true
  end
  for key in pairs(set2) do
    result[key] = true
  end
  return result
end


-- overrules the transfer prohibition
local function SpecialTransferUnit(...)
  local oldAllowTransferState = allowTransfer
  allowTransfer = true
  Spring.TransferUnit(...)
  allowTransfer = oldAllowTransferState
end

local function FindFirstLogic(logicType)
  for _, trigger in ipairs(triggers) do
    for _, logicItem in ipairs(trigger.logic) do
      if logicItem.logicType == logicType then
        return logicItem, trigger
      end
    end
  end
end

local function FindAllLogic(logicType)
  local logicItems = {}
  for _, trigger in ipairs(triggers) do
    for _, logicItem in ipairs(trigger.logic) do
      if logicItem.logicType == logicType then
        logicItems[logicItem] = trigger
      end
    end
  end
  return logicItems
end


local function ArraysHaveIntersection(array1, array2)
  for _, item1 in ipairs(array1) do
    for _, item2 in ipairs(array2) do
      if item1 == item2 then return true end
    end
  end
  return false
end


local function RemoveTrigger(trigger)
  for i=1, #triggers do
    if triggers[i] == trigger then
      table.remove(triggers, i)
      break
    end
  end
end


local function ArrayContains(array, item)
  for i=1, #array do
    if item == array[i] then return true end
  end
  return false
end

local function StartsWith(s, startString)
  return string.sub(s, 1, #startString) == startString
end


local function CountTableElements(t)
  local count = 0
  for _ in pairs(t) do
    count = count + 1
  end
  return count
end


local function GetUnitsInRegion(region, teamID)
  local regionUnits = {}
  if Spring.GetTeamInfo(teamID) then
    for i=1, #region.areas do
      local area = region.areas[i]
      local areaUnits
      if area.category == "cylinder" then
        areaUnits = Spring.GetUnitsInCylinder(area.x, area.y, area.r, teamID)
      elseif area.category == "rectangle" then
        areaUnits = Spring.GetUnitsInRectangle(area.x, area.y, area.x + area.width, area.y + area.height, teamID)
      else
        error "area category not supported"
      end
      for _, unitID in ipairs(areaUnits) do
        regionUnits[unitID] = true
      end
    end
  end
  return regionUnits
end

local function IsUnitInRegion(unitID, region)
  local x, y, z = Spring.GetUnitPosition(unitID)
  for i=1,#region.areas do
    local area = region.areas[i]
    if area.category == "cylinder" then
      local dist = ( (x - area.x)^2 + (z - area.y)^2 )^0.5
      if dist <= area.r then
        return true
      end
    elseif area.category == "rectangle" then
      local rx1, rx2, rz1, rz2 = area.x, area.x + area.width, area.y, area.y + area.height
      if x >= rx1 and x <= rx2 and z >= rz1 and z <= rz2 then
        return true
      end
    else
      error "area category not supported"
    end    
  end
  return false
end

local function GetRegionsUnitIsIn(unitID)
  local regions = {}
  local ret = false
  for i=1,#mission.regions do
    local region = mission.regions[i]
    if IsUnitInRegion(unitID, region) then
      regions[#region + 1] = region
      ret = true
    end
  end
  if ret then
    return regions
  else
    return nil
  end
end

GG.mission.IsUnitInRegion = IsUnitInRegion

local function SetUnitGroup(unitID, group)
  unitGroups[unitID] = unitGroups[unitID] or {}
  unitGroups[unitID][group] = true
end


local function FindUnitsInGroup(searchGroup)
  local results = {}
  if StartsWith(searchGroup, "Units in ") then
    for i=1,#mission.regions do
      local region = mission.regions[i]
      for playerIndex=1, #mission.players do
        local player = mission.players[playerIndex]
        if searchGroup == string.format("Units in %s (%s)", region.name, player) then
          local teamID = playerIndex - 1
          return GetUnitsInRegion(region, teamID)          
        end
      end
    end
  elseif StartsWith(searchGroup, "Latest Factory Built Unit (") then
    for playerIndex=1, #mission.players do
      local player = mission.players[playerIndex]
      if searchGroup == "Latest Factory Built Unit ("..player..")" then
        local teamID = playerIndex - 1
        if lastFinishedUnits[teamID] then
          results[lastFinishedUnits[teamID]] = true
        end
      end
    end
    return results
  elseif StartsWith(searchGroup, "Any Unit (") then
    for playerIndex=1, #mission.players do
      local player = mission.players[playerIndex]
      if searchGroup == "Any Unit ("..player..")" then
        local teamID = playerIndex - 1
        local units = Spring.GetTeamUnits(teamID)
        local ret = {}
        for i=1,#units do
          ret[units[i]] = true
        end
        return ret
      end
    end
  end
  -- static group
  for unitID, groups in pairs(unitGroups) do
    if groups[searchGroup] then
      results[unitID] = true
    end
  end
  return results
end

-- returns first unitID it finds in a group
local function FindUnitInGroup(searchGroup)
  local results = FindUnitsInGroup(searchGroup)
  for unitID in pairs(results) do
    return unitID
  end
end

local function FindUnitsInGroups(searchGroups)
  local results = {}
  for searchGroup in pairs(searchGroups) do
    results = MergeSets(results, FindUnitsInGroup(searchGroup))
  end
  return results
end

GG.mission.FindUnitsInGroup = FindUnitsInGroup
GG.mission.FindUnitInGroup = FindUnitInGroup
GG.mission.FindUnitsInGroups = FindUnitsInGroups

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
local disabledUnitDefIDs = {} -- [team] = {[unitDefID1] = true, [unitDefID2] = true, ...}
local selectedUnitConditionGroups = {}
local unitIsVisibleConditionGroups = {}

do
  local teams = Spring.GetTeamList()
  for _, trigger in pairs(triggers) do
    trigger.occurrences = trigger.occurrences or 0
    if trigger.maxOccurrences < 0 then
      trigger.maxOccurrences = math.huge
    elseif trigger.maxOccurrences == 0 then
      RemoveTrigger(trigger)
    end
  end
  
  for condition in pairs(FindAllLogic"TimeCondition") do
    condition.args.period = condition.args.frames
  end
  
  for condition in pairs(FindAllLogic"UnitCreatedCondition") do
    condition.args.unitDefIDs = {}
    for _, unitName in ipairs(condition.args.units) do
      condition.args.unitDefIDs[UnitDefNames[unitName].id] = true
    end
  end
  
  for condition in pairs(FindAllLogic"UnitFinishedCondition") do
    condition.args.unitDefIDs = {}
    for _, unitName in ipairs(condition.args.units) do
      condition.args.unitDefIDs[UnitDefNames[unitName].id] = true
    end
  end
  
  for condition in pairs(FindAllLogic"UnitFinishedInFactoryCondition") do
    condition.args.unitDefIDs = {}
    for _, unitName in ipairs(condition.args.units) do
      condition.args.unitDefIDs[UnitDefNames[unitName].id] = true
    end
  end
  
  for i=1,#teams do
    local teamID = teams[i]
    disabledUnitDefIDs[teamID] = {}
    for _, disabledUnitName in ipairs(mission.disabledUnits) do
      disabledUnitDefIDs[teamID][UnitDefNames[disabledUnitName].id] = true
    end
  end
  
  for condition in pairs(FindAllLogic("UnitSelectedCondition")) do
    selectedUnitConditionGroups = MergeSets(selectedUnitConditionGroups, condition.args.groups)
  end
  
  for condition in pairs(FindAllLogic("UnitIsVisibleCondition")) do
    unitIsVisibleConditionGroups = MergeSets(unitIsVisibleConditionGroups, condition.args.groups)
  end
end


local function AddUnitGroup(unitID, group)
  unitGroups[unitID][group] = true
  if selectedUnitConditionGroups[group] then
    Spring.SetUnitRulesParam(unitID, "notifyselect", 1)
  end
  if unitIsVisibleConditionGroups[group] then
    Spring.SetUnitRulesParam(unitID, "notifyvisible", 1)
  end
end

GG.mission.AddUnitGroup = AddUnitGroup

local function AddUnitGroups(unitID, groups)
  for group in pairs(groups) do
    AddUnitGroup(unitID, group)
  end
end

local function RemoveUnitGroup(unitID, group)
  unitGroups[unitID][group] = nil
  if selectedUnitConditionGroups[group] then
    Spring.SetUnitRulesParam(unitID, "notifyselect", 0)
  end
  if unitIsVisibleConditionGroups[group] then
    Spring.SetUnitRulesParam(unitID, "notifyvisible", 0)
  end 
end

GG.mission.RemoveUnitGroup = RemoveUnitGroup
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function UpdateDisabledUnits(unitID, teamID)
  local unitDefID = Spring.GetUnitDefID(unitID)
  local buildOpts = UnitDefs[unitDefID].buildOptions
  for i=1,#buildOpts do
    local buildID = buildOpts[i]
    local cmdDescID = Spring.FindUnitCmdDesc(unitID, -buildID)
    if cmdDescID then
      Spring.EditUnitCmdDesc(unitID, cmdDescID, {disabled = disabledUnitDefIDs[teamID][buildID] or false})
    end
  end
end


local function UpdateAllDisabledUnits()
  local units = Spring.GetAllUnits()
  for i=1,#units do
    local unitID = units[i]
    local teamID = Spring.GetUnitTeam(unitID)
    UpdateDisabledUnits(unitID, teamID)
  end
end

local function AddEvent(frame, event, args, cutsceneID)
  events[frame] = events[frame] or {}
  table.insert(events[frame], {event = event, args = args, cutsceneID = cutsceneID})
end


local function CustomConditionMet(name)
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "CustomCondition" and trigger.name == name then
        ExecuteTrigger(trigger)
        break
      end
    end
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- actually defined in a bit
local function ExecuteTrigger() end

local function UnsyncedEventFunc(action)
  action.args.logicType = action.logicType
  _G.missionEventArgs = action.args
  SendToUnsynced("MissionEvent")
  _G.missionEventArgs = nil
end

local actionsTable = {
  CustomAction = function(action)
          if action.name == "my custom action name" then
            -- fill in your custom actions
          end
        end,
  CustomAction2 = function(action)
          if action.args.synced then
            local func, err = loadstring(action.args.codeStr)
            if err then
              error("Failed to load custom action: ".. action.args.codeStr)
              return
            end
	    func()
          else
            action.args.logicType = action.logicType
            _G.missionEventArgs = action.args
            SendToUnsynced"MissionEvent"
            _G.missionEventArgs = nil
          end
        end,
  DestroyUnitsAction = function(action)
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.DestroyUnit(unitID, true, not action.args.explode)
          end
        end,
  ExecuteTriggersAction = function(action)
          for _, triggerIndex in ipairs(action.args.triggers) do
              ExecuteTrigger(allTriggers[triggerIndex])
          end
        end,
  ExecuteRandomTriggerAction = function(action)
          local triggerIndex = TakeRandomI(action.args.triggers)
          ExecuteTrigger(allTriggers[triggerIndex])
        end,
  AllowUnitTransfersAction = function(action)
          allowTransfer = true
          GG.mission.allowTransfer = true
        end,
  TransferUnitsAction = function(action)
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            SpecialTransferUnit(unitID, action.args.player, false)
          end
        end,
  ModifyResourcesAction = function(action)  
          local teamID = action.args.player
          if Spring.GetTeamInfo(teamID) then
            if action.args.category == "metal" then
              if action.args.amount > 0 then
                Spring.AddTeamResource(teamID, "metal", action.args.amount)
              else
                Spring.UseTeamResource(teamID, "metal", -action.args.amount)
              end
            elseif action.args.category == "energy" then
              if action.args.amount > 0 then
                Spring.AddTeamResource(teamID, "energy", action.args.amount)
              else
                Spring.UseTeamResource(teamID, "energy", -action.args.amount)
              end
            elseif action.args.category == "energy storage" then
              local _, currentStorage = Spring.GetTeamResources(teamID, "energy")
              Spring.SetTeamResource(teamID, "es", currentStorage + action.args.amount)
            elseif action.args.category == "metal storage" then
              local _, currentStorage = Spring.GetTeamResources(teamID, "metal")
              Spring.SetTeamResource(teamID, "ms", currentStorage + action.args.amount)
            end
          end
        end,
  ModifyUnitHealthAction = function(action)
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.AddUnitDamage(unitID, action.args.damage)
          end
        end,
  MakeUnitsAlwaysVisibleAction = function(action)
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.SetUnitAlwaysVisible(unitID, action.args.value)
          end
        end,
  MakeUnitsNeutralAction = function(action)
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.SetUnitNeutral(unitID, action.args.value)
          end
        end,
  ModifyCounterAction = function(action)
          local counter = action.args.counter
          local value = action.args.value
          local n = counters[counter]
          if action.args.action == "Increase" then
             counters[counter] = n + value
          elseif action.args.action == "Reduce" then
             counters[counter] = n - value
          elseif action.args.action == "Set" then
             counters[counter] = value
          elseif action.args.action == "Multiply" then
             counters[counter] = n * value
          end
          for _, trigger in ipairs(triggers) do
            for _, condition in ipairs(trigger.logic) do
              if condition.logicType == "CounterModifiedCondition" then
                local c = condition.args.condition
                local v = condition.args.value
                local n = counters[condition.args.counter]
                if (c == "=" and n == v) or
                   (c == "<" and n < v) or
                   (c == ">" and n > v) or
                   (c == "<=" and n <= v) or 
                   (c == ">=" and n >= v) or
                   (c == "!=" and n ~= v) then
                  ExecuteTrigger(trigger)
                  break
                end
              end
            end
          end
        end,
  DisplayCountersAction = function(action)
          for counter, value in pairs(counters) do
            Spring.Echo(string.format("Counter %s: %f", counter, value))
          end
        end,
  ModifyScoreAction = function(action)
          for _, teamID in ipairs(action.args.players) do
            if Spring.GetTeamInfo(teamID) then
              local score = scores[teamID] or 0
              if action.args.action == "Increase Score" then
                score = score + action.args.value
              elseif action.args.action == "Reduce Score" then
                score = score - action.args.value
              elseif action.args.action == "Set Score" then
                score = action.args.value
              elseif action.args.action == "Multiply Score" then
                score = score * action.args.value
              end
              scores[teamID] = score
              Spring.SetTeamRulesParam(teamID, "score", score)
            end
          end
        end,
  EnableTriggersAction = function(action)
          for _, triggerIndex in ipairs(action.args.triggers) do
            allTriggers[triggerIndex].enabled = true
          end
        end,
  DisableTriggersAction = function(action)
          for _, triggerIndex in ipairs(action.args.triggers) do
            allTriggers[triggerIndex].enabled = false
          end
        end,
  StartCountdownAction = function(action)
          local expiry = Spring.GetGameFrame() + action.args.frames
          countdowns[action.args.countdown] = expiry
          if action.args.display then
            displayedCountdowns[action.args.countdown] = true
            Spring.SetGameRulesParam("countdown:"..action.args.countdown, expiry)
          end
        end,
  CancelCountdownAction = function(action)
          countdowns[action.args.countdown] = nil
          displayedCountdowns[action.args.countdown] = nil
          Spring.SetGameRulesParam("countdown:"..action.args.countdown, "-1")
        end,
  ModifyCountdownAction = function(action)
          if countdowns[action.args.countdown] then
            local newExpiry
            if action.args.action == "Extend" then
              newExpiry = countdowns[action.args.countdown] + action.args.frames
            elseif action.args.action == "Anticipate" then
              newExpiry = countdowns[action.args.countdown] - action.args.frames
            else
              error"countdown modify mode not supported"
            end
            if newExpiry < Spring.GetGameFrame() then -- execute immediatly
              countdowns[action.args.countdown] = nil
              displayedCountdowns[action.args.countdown] = nil
              Spring.SetGameRulesParam("countdown:"..action.args.countdown, "-1")
              for _, trigger in ipairs(triggers) do
                for _, condition in ipairs(trigger.logic) do
                  if condition.logicType == "CountdownEndedCondition" and
                     condition.args.countdown == action.args.countdown then
                    ExecuteTrigger(trigger)
                    break
                  end
                end
              end
            else -- change expiry time
              countdowns[action.args.countdown] = newExpiry
              if displayedCountdowns[action.args.countdown] then
                Spring.SetGameRulesParam("countdown:"..action.args.countdown, newExpiry)
              end
            end
          end
          -- todo: execute trigger if countdown has expired! print Spring.Echo
        end,
  CreateUnitsAction = function(action, createdUnits)
          local gameframe = Spring.GetGameFrame()
          for _, unit in ipairs(action.args.units) do
            if Spring.GetTeamInfo(unit.player) then
              if unit.isGhost then
                for group in pairs(unit.groups) do
                  unit[#unit + 1] = group
                end
                _G.ghostEventArgs = unit
                SendToUnsynced("GhostEvent")
                _G.ghostEventArgs = nil
              else
                local ud = UnitDefNames[unit.unitDefName]
                local isBuilding = ud.isBuilding or ud.isFactory or not ud.canMove
                local cardinalHeading = "n"
                --if isBuilding then
                  if unit.heading > 45 and unit.heading <= 135 then
                    cardinalHeading = "e"
                  elseif unit.heading > 135 and unit.heading <= 225 then
                    cardinalHeading = "s"
                  elseif unit.heading > 225 and unit.heading <= 315 then
                    cardinalHeading = "w"
                  end
                --end
                
                -- ZK mex placement
                if ud.customParams.ismex and GG.metalSpots then
                  local function GetClosestMetalSpot(x, z)
                    local bestSpot
                    local bestDist = math.huge
                    local bestIndex 
                    for i = 1, #GG.metalSpots do
                            local spot = GG.metalSpots[i]
                            local dx, dz = x - spot.x, z - spot.z
                            local dist = dx*dx + dz*dz
                            if dist < bestDist then
                                    bestSpot = spot
                                    bestDist = dist
                                    bestIndex = i
                            end
                    end
                    return bestSpot
                  end
                  
                  local bestSpot = GetClosestMetalSpot(unit.x, unit.y )
                  unit.x, unit.y = bestSpot.x, bestSpot.z
                end
                
                local unitID, drop
                local height = Spring.GetGroundHeight(unit.x, unit.y)
                if GG.DropUnit and (action.args.useOrbitalDrop) then
                  drop = true
                  unitID = GG.DropUnit(unit.unitDefName, unit.x, height, unit.y, cardinalHeading, unit.player)
                else
                  unitID = Spring.CreateUnit(unit.unitDefName, unit.x, height, unit.y, cardinalHeading, unit.player)
                end
                if action.args.ceg and action.args.ceg ~= '' then
                  Spring.SpawnCEG(action.args.ceg, unit.x, height, unit.y, 0, 1, 0)
                end
                if unitID then
                  if not isBuilding then
                    if unit.heading ~= 0 then
                      local heading = (unit.heading - 180)/360 * 2 * math.pi
                      if drop and gameframe > 1 then
                        --Spring.MoveCtrl.SetRotation(unitID, 0, heading, 0)
                        Spring.SetUnitRotation(unitID, 0, heading, 0)
                      else
                        Spring.SetUnitRotation(unitID, 0, heading, 0)
                      end
                    end
                  end
                  createdUnits[unitID] = true
                  if unit.groups and next(unit.groups) then
                    AddUnitGroups(unitID, unit.groups)
                  end
                end
              end
            end
          end
        end,
  ConsoleMessageAction = function(action)
          Spring.SendMessage(action.args.message)
        end,
  DefeatAction = function(action)
	  local aiAllyTeams = {}
          local teams = Spring.GetTeamList()
          for i=1,#teams do
            local unitTeam = teams[i]
            local _, _, _, isAI, _, allyTeam = Spring.GetTeamInfo(unitTeam)
            if isAI then
              aiAllyTeams[#aiAllyTeams+1] = allyTeam
            else
              Spring.KillTeam(unitTeam)
            end
          end
	  Spring.GameOver(aiAllyTeams)
        end,
  VictoryAction = function(action)
	  local humanAllyTeams = {}
          local teams = Spring.GetTeamList()
          for i=1,#teams do
            local unitTeam = teams[i]
            local _, _, _, isAI, _, allyTeam = Spring.GetTeamInfo(unitTeam)
            if not isAI then
              humanAllyTeams[#humanAllyTeams+1] = allyTeam
            end
          end
	  Spring.GameOver(humanAllyTeams)
        end,
  LockUnitsAction = function(action)
          local teams = action.args.players or {}
          if CountTableElements(teams) == 0 then
            teams = Spring.GetTeamList()
          end
          for _, disabledUnitName in ipairs(action.args.units) do
            local disabledUnit = UnitDefNames[disabledUnitName]
            if disabledUnit then
              for _,teamID in pairs(teams) do
                disabledUnitDefIDs[teamID][disabledUnit.id] = true
              end
            end
          end
          wantUpdateDisabledUnits = true
        end,
  UnlockUnitsAction = function(action)
          local teams = action.args.players or {}
          if CountTableElements(teams) == 0 then
            teams = Spring.GetTeamList()
          end
          for _, disabledUnitName in ipairs(action.args.units) do
            local disabledUnit = UnitDefNames[disabledUnitName]
            if disabledUnit then
              for _,teamID in pairs(teams) do
                disabledUnitDefIDs[teamID][disabledUnit.id] = nil
              end
            end
          end
          wantUpdateDisabledUnits = true
        end,
  GiveOrdersAction = function(action, createdUnits)
          local orderedUnits
          if not next(action.args.groups) then
            orderedUnits = createdUnits
          else
            orderedUnits = FindUnitsInGroups(action.args.groups)
          end
          for unitID in pairs(orderedUnits) do
            for i, order in ipairs(action.args.orders) do
              -- bug workaround: the table needs to be copied before it's used in GiveOrderToUnit
              local x, y, z = order.args[1], order.args[2], order.args[3]
              if y == 0 then y = Spring.GetGroundHeight(x, z) end
              local options
              if i == 1 and (not action.args.queue) then
                options = 0
              else
                options = shiftOptsTable
              end
              Spring.GiveOrderToUnit(unitID, CMD[order.orderType], {x, y, z}, options)
            end
          end
        end,
  GiveTargetedOrdersAction = function(action, createdUnits)
          local orderedUnits
          if not next(action.args.groups) then
            orderedUnits = createdUnits
          else
            orderedUnits = FindUnitsInGroups(action.args.groups)
          end
          for unitID in pairs(orderedUnits) do
            for i, order in ipairs(action.args.orders) do
              local targets = FindUnitsInGroups(action.args.targetGroups)
              local target
              for unitID in pairs(targets) do
                target = unitID
                break
              end
              local options
              if i == 1 and (not action.args.queue) then
                options = 0
              else
                options = shiftOptsTable
              end
              Spring.GiveOrderToUnit(unitID, CMD[order.orderType], {target}, options)
            end
          end
        end,
  GiveFactoryOrdersAction = function(action, createdUnits)
          local orderedUnits
          if not next(action.args.factoryGroups) then
            orderedUnits = createdUnits
          else
            orderedUnits = FindUnitsInGroups(action.args.factoryGroups)
          end
          for factoryID in pairs(orderedUnits) do
            local factoryDef = UnitDefs[Spring.GetUnitDefID(factoryID)]
            local hasBeenGivenOrders = false
            for _, unitDefName in ipairs(action.args.buildOrders) do
              local unitDef = UnitDefNames[unitDefName]
              if ArrayContains(factoryDef.buildOptions, unitDef.id) then
                Spring.GiveOrderToUnit(factoryID, -unitDef.id, {}, {})
                hasBeenGivenOrders = true
                if next(action.args.builtUnitsGroups) then
                  factoryExpectedUnits[factoryID] = factoryExpectedUnits[factoryID] or {}
                  table.insert(factoryExpectedUnits[factoryID], {unitDefID = unitDef.id, groups = CopyTable(action.args.builtUnitsGroups)})
                end
              end
            end
            if hasBeenGivenOrders and action.args.repeatOrders then
              Spring.GiveOrderToUnit(factoryID, CMD.REPEAT, {1}, {})
              repeatFactoryGroups[factoryID] = CopyTable(action.args.builtUnitsGroups)
            end
          end
        end,
  SendScoreAction = function(action)
          if not (cheatingWasEnabled) then
            SendToUnsynced("ScoreEvent")
          end
        end,
  SetCameraUnitTargetAction = function(action)
          for unitID, groups in pairs(unitGroups) do
            if groups[action.args.group] then
              local _,_,_,x,_,y = Spring.GetUnitPosition(unitID, true)
              local args = {
                x = x,
                y = y,
                logicType = "SetCameraPointTargetAction",
              }
              _G.missionEventArgs = args
              SendToUnsynced("MissionEvent")
              _G.missionEventArgs = nil
              break
            end
          end
        end,
  BeautyShotAction = function(action)
          for unitID, groups in pairs(unitGroups) do
            if groups[action.args.group] then
              local _,_,_,x,y,z = Spring.GetUnitPosition(unitID, true)
              action.args.x = x
              action.args.y = y
              action.args.z = z
              action.args.unitID = unitID
              UnsyncedEventFunc(action)
              break
            end
          end
        end,
  AddObjectiveAction = function(action)
          objectives[#objectives+1] = {id = action.args.id, title = action.args.title, description = action.args.description, status = "Incomplete", unitsOrPositions = {}}
          UnsyncedEventFunc(action)
        end,
  ModifyObjectiveAction = function(action)
          for i=1,#objectives do
            local obj = objectives[i]
            if obj.id == action.args.id then
              local title = action.args.title
              if title and title ~= '' then
                obj.title = title
              end
              
              local description = action.args.description
              if description and description ~= '' then
                obj.description = description
              end
              
              obj.status = action.args.status or obj.status
              break
            end
          end
          UnsyncedEventFunc(action)
        end,
  AddUnitsToObjectiveAction = function(action)
          for i=1,#objectives do
            local obj = objectives[i]
            if obj.id == action.args.id then
              local groupName = action.args.group
              local units = FindUnitsInGroup(groupName)
              for unitID in pairs(units) do
                obj.unitsOrPositions[#obj.unitsOrPositions + 1] = unitID
                unitsWithObjectives[unitID] = true
              end
              action.args.units = units
              break
            end
          end
          UnsyncedEventFunc(action)
        end,
  AddPointToObjectiveAction = function(action)
          for i=1,#objectives do
            local obj = objectives[i]
            if obj.id == action.args.id then
              obj.unitsOrPositions[#obj.unitsOrPositions + 1] = {action.args.x, action.args.y}
              break
            end
          end
          UnsyncedEventFunc(action)
        end,
  EnterCutsceneAction = function(action)
          currCutsceneID = action.args.id
          currCutsceneIsSkippable = action.args.skippable
          UnsyncedEventFunc(action)
        end,
  LeaveCutsceneAction = function(action)
          currCutsceneID = nil
          currCutsceneIsSkippable = false
          UnsyncedEventFunc(action)
        end,
  StopCutsceneActionsAction = function(action)
          local toKillID = action.args.cutsceneID
          local anyCutscene = (toKillID == "Any Cutscene")
          local currentCutscene = (toKillID == "Current Cutscene")
          for frame, eventsAtFrame in pairs(events) do
            for i=#eventsAtFrame,1,-1 do  -- work backwards so we don't mess up the indexes ahead of us when removing table entries
              local event = eventsAtFrame[i]
              if anyCutscene or (event.cutsceneID == toKillID) or (currentCutscene and (event.cutsceneID == currCutsceneID)) then
                table.remove(eventsAtFrame, i)
              end
            end
          end
        end,
}

local unsyncedActions = {
  PauseAction = true,
  MarkerPointAction = true, 
  SetCameraPointTargetAction = true,
  SetCameraPosDirAction = true,
  SaveCameraStateAction = true,
  RestoreCameraStateAction = true,
  ShakeCameraAction = true,
  GuiMessageAction = true,
  GuiMessagePersistentAction = true,
  HideGuiMessagePersistentAction = true,
  ConvoMessageAction = true,
  ClearConvoMessageQueueAction = true,
  AddObjectiveAction = true,
  ModifyObjectiveAction = true,
  AddUnitsToObjectiveAction = true,
  AddPointToObjectiveAction = true,
  SoundAction = true,
  MusicAction = true,
  MusicLoopAction = true,
  StopMusicAction = true,
  SunriseAction = true, 
  SunsetAction = true,
  EnterCutsceneAction = true,
  LeaveCutsceneAction = true,
  FadeOutAction = true,
  FadeInAction = true,
}
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

ExecuteTrigger = function(trigger, frame)
  if not trigger.enabled then return end
  if math.random() < trigger.probability then
    if trigger.maxOccurrences == trigger.occurrences then
      RemoveTrigger(trigger) -- the trigger is no longer needed
      return
    end
    local createdUnits = {}
    local cutsceneID
    local frame = frame or (Spring.GetGameFrame() + 1) -- events will take place at this frame
    for i=1,#trigger.logic do
      local action = trigger.logic[i]
      local args = {action, createdUnits, action.logicType}
      local Event = actionsTable[action.logicType]
      
      if action.logicType == "WaitAction" then
        frame = frame + action.args.frames
      elseif action.logicType == "EnterCutsceneAction" then
        cutsceneID = action.args.id
      elseif action.logicType == "LeaveCutsceneAction" then
        cutsceneID = nil
      end
      
      if unsyncedActions[action.logicType] and (not Event) then
        Event = UnsyncedEventFunc
      end

      if Event then
        AddEvent(frame, Event, args, cutsceneID) -- schedule event
      end
    end
  end
  trigger.occurrences = trigger.occurrences + 1
  if trigger.maxOccurrences == trigger.occurrences then
    RemoveTrigger(trigger) -- the trigger is no longer needed
  end
end
GG.mission.ExecuteTrigger = ExecuteTrigger

local function ExecuteTriggerByName(name)
  local triggers = GG.mission.triggers
  for i=1,#triggers do
    local trigger = triggers[i]
    if trigger and trigger.name == name then
      ExecuteTrigger(trigger)
    end
  end
end
GG.mission.ExecuteTriggerByName = ExecuteTriggerByName

local function SetTriggerEnabledByName(name, enabled)
  local triggers = GG.mission.triggers
  for i=1,#triggers do
    local trigger = triggers[i]
    if trigger and trigger.name == name then
      trigger.enabled = enabled
    end
  end
end
GG.mission.SetTriggerEnabledByName = SetTriggerEnabledByName

local function CheckUnitsEnteredGroups(unitID, condition)
  if not next(condition.args.groups) then return true end -- no group selected: any unit is ok
  if not unitGroups[unitID] then return false end -- group is required but unit has no group
  if DoSetsIntersect(condition.args.groups, unitGroups[unitID]) then return true end -- unit has one of the required groups
  return false
end


local function CheckUnitsEnteredPlayer(unitID, condition)
  if not condition.args.players[1] then return true end -- no player is required: any is ok
  return ArrayContains(condition.args.players, Spring.GetUnitTeam(unitID)) -- unit is is owned by one of the selected players
end


local function CheckUnitsEntered(units, condition)
  local count = 0
  for _, unitID in ipairs(units) do
    if CheckUnitsEnteredGroups(unitID, condition) and 
       CheckUnitsEnteredPlayer(unitID, condition) then
      count = count + 1
    end
  end
  return count >= condition.args.number
end

local function SendMissionVariables(tbl)
  if not (tbl and type(tbl) == "table") then
    Spring.Log(gadget:GetInfo().name, "error", "Invalid argument for SendMissionVariables")
    return
  end
  local str = ""
  for k,v in pairs(tbl) do
    str = str .. k .. "=" .. v .. ";"
  end
  print("MISSIONVARS: "..GG.Base64Encode(str))
end

GG.mission.SendMissionVariables = SendMissionVariables

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function gadget:UnitDamaged(unitID, unitDefID, unitTeam, damage, paralyzer, 
                            weaponID, attackerID, attackerDefID, attackerTeam)
  local toExecute = {}
  for triggerIndex=1, #triggers do
    local trigger = triggers[triggerIndex]
    for conditionIndex=1, #trigger.logic do
      local condition = trigger.logic[conditionIndex]
      if condition.logicType == "UnitDamagedCondition" and
         --not paralyzer and
         (Spring.GetUnitHealth(unitID) < condition.args.value) and
         (condition.args.anyAttacker or FindUnitsInGroup(condition.args.attackerGroup)[attackerID]) and
         (condition.args.anyVictim or FindUnitsInGroup(condition.args.victimGroup)[unitID]) then
        toExecute[#toExecute + 1] = trigger
        break
      end
    end
  end
  -- do it this way to avoid the problem when you remove elements from a table while still iterating over it
  for i=1,#toExecute do
    ExecuteTrigger(toExecute[i])
  end
end


function gadget:AllowUnitTransfer(unitID, unitDefID, oldTeam, newTeam, capture)
  return capture or allowTransfer
end

function gadget:GamePreload()
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "GamePreloadCondition" then
        ExecuteTrigger(trigger, -1)
      end
    end
  end
  if events[-1] then
    for _, Event in ipairs(events[-1]) do
        Event.event(unpack(Event.args)) -- run event
    end
  end
end

function gadget:GameFrame(n)

  if not gameStarted then
    for _, trigger in ipairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "GameStartedCondition" then
          ExecuteTrigger(trigger, n)
        end
      end
    end
    if mission.debug then
      Spring.SendCommands("setmaxspeed 100")
    end
    
    
    for _, trigger in ipairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "PlayerJoinedCondition" then
          local players = Spring.GetPlayerList(condition.args.playerNumber, true)
          if players[1] then
            ExecuteTrigger(trigger)
            break
          end          
        end
      end
    end
    
    gameStarted = true
  end
  
 
  if events[n] then -- list of events to run at this frame
    for _, Event in ipairs(events[n]) do
      Event.event(unpack(Event.args)) -- run event
    end
    events[n] = nil
  end
  
  for countdown, expiry in pairs(countdowns) do
    if n == expiry then
      countdowns[countdown] = nil
      displayedCountdowns[countdown] = nil
      for _, trigger in ipairs(triggers) do
        for _, condition in ipairs(trigger.logic) do
          if condition.logicType == "CountdownEndedCondition" and
             condition.args.countdown == countdown then
            ExecuteTrigger(trigger)
            break
          end
        end
      end
    end
    for _, trigger in ipairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "CountdownTickCondition" and
           condition.args.countdown == countdown and 
           (expiry - n) % condition.args.frames == 0 then
          ExecuteTrigger(trigger)
          break
        end
        if condition.logicType == "TimeLeftInCountdownCondition" and
           condition.args.countdown == countdown and 
           (expiry - n) < condition.args.frames then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
  end
  
  
  for _, trigger in pairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      local args = condition.args
      if condition.logicType == "TimeCondition" and args.frames == n then 
        args.frames = n + args.period
        ExecuteTrigger(trigger)
        break
      end
    end
  end
  
  for _, trigger in pairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      local args = condition.args
      if condition.logicType == "TimerCondition" and args.frames == n then 
        ExecuteTrigger(trigger)
        break
      end
    end
  end
  
  if Spring.IsCheatingEnabled() then
    if not cheatingWasEnabled then
      cheatingWasEnabled = true
      GG.mission.cheatingWasEnabled = true
      Spring.Echo "The score will not be saved."
    end
  end
  
  if (n+3)%30 == 0 then
    for _, trigger in pairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "UnitsAreInAreaCondition" then
          local areas = condition.args.areas
          for _, area in ipairs(areas) do
            local units
            if area.category == "cylinder" then
              units = Spring.GetUnitsInCylinder(area.x, area.y, area.r)
            elseif area.category == "rectangle" then
              units = Spring.GetUnitsInRectangle(area.x, area.y, area.x + area.width, area.y + area.height)
            else
              error "area category not supported"
            end
            if CheckUnitsEntered(units, condition) then
              ExecuteTrigger(trigger)
              break
            end
          end
        end
      end
    end
  end
  
  if wantUpdateDisabledUnits then
    UpdateAllDisabledUnits()
    wantUpdateDisabledUnits = false
  end
end


function gadget:GameOver()
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "GameEndedCondition" then
        ExecuteTrigger(trigger)
        break
      end
    end
  end
end


function gadget:TeamDied(teamID)
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "PlayerDiedCondition" and condition.args.playerNumber == teamID then
        ExecuteTrigger(trigger)
        break
      end
    end
  end
end


function gadget:UnitDestroyed(unitID, unitDefID, unitTeam)
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "UnitDestroyedCondition" then
        -- check if the unit is in a region the trigger is watching
        for groupName in pairs(condition.args.groups) do
          if FindUnitsInGroup(groupName)[unitID] then
            ExecuteTrigger(trigger)
            break
          end
        end
      end
    end
  end
  unitGroups[unitID] = nil
  factoryExpectedUnits[unitID] = nil
  repeatFactoryGroups[unitID] = nil
  
  if unitsWithObjectives[unitID] then
    for i=1, #objectives do
      local obj = objectives[i]
      for j=#obj.unitsOrPositions, 1, -1 do
        unitOrPos = obj.unitsOrPositions[j]
        if unitOrPos == unitID then
          table.remove(obj.unitsOrPositions, j)
        end
      end
    end
    unitsWithObjectives[unitID] = nil
  end
end


function gadget:UnitFromFactory(unitID, unitDefID, unitTeam, factID, factDefID, userOrders)

  lastFinishedUnits[unitTeam] = unitID
  
  -- assign groups
  if repeatFactoryGroups[factID] then
    for group in pairs(repeatFactoryGroups[factID]) do
      AddUnitGroup(unitID, group)
    end
  elseif factoryExpectedUnits[factID] then
    if factoryExpectedUnits[factID][1].unitDefID == unitDefID then
      for group in pairs(factoryExpectedUnits[factID][1].groups) do
        AddUnitGroup(unitID, group)
      end
      table.remove(factoryExpectedUnits[factID], 1)
    end
  end
  
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "UnitFinishedInFactoryCondition" and 
        (condition.args.unitDefIDs[unitDefID] or not condition.args.units[1]) then
        if not next(condition.args.players) or ArrayContains(condition.args.players, unitTeam) then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
  end
end


function gadget:UnitFinished(unitID, unitDefID, unitTeam)
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "UnitFinishedCondition" and 
        (condition.args.unitDefIDs[unitDefID] or not condition.args.units[1]) then
        if not next(condition.args.players) or ArrayContains(condition.args.players, unitTeam) then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
  end
end


function gadget:UnitCreated(unitID, unitDefID, unitTeam)
  unitGroups[unitID] = unitGroups[unitID] or {}
  for _, trigger in ipairs(triggers) do
    for _, condition in ipairs(trigger.logic) do
      if condition.logicType == "UnitCreatedCondition" and
        (condition.args.unitDefIDs[unitDefID] or not condition.args.units[1]) then
        if not next(condition.args.players) or ArrayContains(condition.args.players, unitTeam) then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
  end
  UpdateDisabledUnits(unitID, unitTeam)
end

-- ZK custom functions
function gadget:AllowCommand_GetWantedCommand()	
	return true
end

function gadget:AllowCommand_GetWantedUnitDefID()	
	return true
end

function gadget:AllowCommand(unitID, unitDefID, teamID, cmdID, cmdParams, cmdOptions, cmdTag, synced)
  --if isInCutscene and (not synced) then
  --  return false
  --end
  -- prevent widgets from building disabled units
  if disabledUnitDefIDs[teamID][-cmdID] then
    local luaAI = Spring.GetTeamLuaAI(teamID) or ''
    return (luaAI ~= '') and EXEMPT_AI_FROM_UNIT_LOCK
  end
  return true
end

local function Deserialize(text)
  local f, err = loadstring(text)
  if not f then
    Spring.Echo("error while deserializing (compiling): "..tostring(err))
    return
  end
  setfenv(f, {}) -- sandbox for security and cheat prevention
  local success, arg = pcall(f)
  if not success then
    Spring.Echo("error while deserializing (calling): "..tostring(arg))
    return
  end
  return arg
end


function gadget:RecvLuaMsg(msg, player)
  local _, _, _, teamID = Spring.GetPlayerInfo(player)
  if StartsWith(msg, magic) then
    local args = Deserialize(msg)
    if args.type == "notifyGhostBuilt" then
      for _, trigger in ipairs(triggers) do
        for _, condition in ipairs(trigger.logic) do
          if condition.logicType == "UnitBuiltOnGhostCondition" then
            if teamID == args.teamID and not next(condition.args.groups) or DoSetsIntersect(condition.args.groups, args.groups) then
              for group in pairs(args.groups) do  
                AddUnitGroup(args.unitID, group)
              end
              ExecuteTrigger(trigger)
              break
            end
          end
        end
      end
    end
  elseif StartsWith(msg, "notifyselect ") then
    local unitID = tonumber(string.match(msg, "%d+"))
    for _, trigger in ipairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "UnitSelectedCondition" then
          if not next(condition.args.players) or ArrayContains(condition.args.players, teamID) then
            ExecuteTrigger(trigger)
            break
          end
        end
      end
    end
  elseif StartsWith(msg, "notifyvisible ") then
    local unitID = tonumber(string.match(msg, "%d+"))
    for _, trigger in ipairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "UnitIsVisibleCondition" then
          if not next(condition.args.players) or ArrayContains(condition.args.players, teamID) then
            ExecuteTrigger(trigger)
            break
          end
        end
      end
    end
  elseif StartsWith(msg, "sendMissionObjectives") then
    SendToUnsynced("SendMissionObjectives")
  elseif StartsWith(msg, "skipCutscene") then
    if currCutsceneID and currCutsceneIsSkippable then
      for _, trigger in ipairs(triggers) do
        for _, condition in ipairs(trigger.logic) do
          local conditionCutsceneID = condition.args.cutsceneID
          if condition.logicType == "CutsceneSkippedCondition"
            and ((conditionCutsceneID == currCutsceneID) or (conditionCutsceneID == "Any Cutscene") or (conditionCutsceneID == "Current Cutscene"))
            then
            ExecuteTrigger(trigger)
          end
        end
      end
    end
  end
end


-- function gadget:AllowResourceTransfer()
  -- return false
-- end


function gadget:Initialize()
    -- Set up the forwarding calls to the unsynced part of the gadget.
    -- This does not overwrite the calls in the synced part.
    local SendToUnsynced = SendToUnsynced
    for _, callIn in pairs(callInList) do
        local fun = gadget[callIn]
        if (fun ~= nil) then
            gadget[callIn] = function(self, ...)
                SendToUnsynced(callIn, ...)
                return fun(self, ...)
            end
        else
            gadget[callIn] = function(self, ...)
                SendToUnsynced(callIn, ...)
            end
        end
        gadgetHandler:UpdateCallIn(callIn)
    end
end

function gadget:UnitEnteredLos(unitID, unitTeam, allyTeam, unitDefID)
  for i=1,#triggers do
    for j=1,#triggers[i].logic do
      local condition = triggers[i].logic[j]
      if condition.logicType == "UnitEnteredLOSCondition" then
        if not next(condition.args.alliances) or ArrayContains(condition.args.alliances, allyTeam) then
          for groupName in pairs(condition.args.groups) do
            local unitDefID = Spring.GetUnitDefID(unitID)
            if FindUnitsInGroup(groupName)[unitID] then
              ExecuteTrigger(triggers[i])
              break
            end
          end
        end
      end
    end
  end
end

function gadget:Load(zip)
  if not GG.SaveLoad then
    Spring.Log(gadget:GetInfo().name, LOG.WARNING, "Save/Load API not found")
    return
  end
  
  local data = GG.SaveLoad.ReadFile(zip, "Mission Runner", SAVE_FILE)
  if data then
    scores = data.scores
    counters = data.counters
    countdowns = data.countdowns
    displayedCountdowns = data.displayedCountdowns
    unitGroups = data.unitGroups
    repeatFactoryGroups = data.repeatFactoryGroups
    factoryExpectedUnits = data.factoryExpectedUnits
    triggers = data.triggers
    allTriggers = data.triggers
    objectives = data.objectives
    cheatingWasEnabled = data.cheatingWasEnabled
    allowTransfer = data.allowTransfer
    GG.mission.cheatingWasEnabled = cheatingWasEnabled
    GG.mission.allowTransfer = allowTransfer
    gameStarted = true 
  end
  
  SendToUnsynced("SendMissionObjectives")
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- 
-- UNSYNCED
--
else
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- need this because SYNCED.tables are merely proxies, not real tables
local function MakeRealTable(proxy)
  local proxyLocal = proxy
  local ret = {}
  for i,v in spairs(proxyLocal) do
    if type(v) == "table" then
      ret[i] = MakeRealTable(v)
    else
      ret[i] = v
    end
  end
  return ret
end

function WrapToLuaUI()
  if Script.LuaUI("MissionEvent") then
    local missionEventArgs = {}
    local missionEventArgsSynced = SYNCED.missionEventArgs
    for k, v in spairs(missionEventArgsSynced) do
      missionEventArgs[k] = v
    end
    Script.LuaUI.MissionEvent(missionEventArgs)
  end
end


local ghosts = {}

function GhostEvent()
  local ghost= {}
  local ghostSynced = SYNCED.ghostEventArgs
  for k, v in spairs(ghostSynced) do
    ghost[k] = v
  end
  ghosts[ghost] = true
  local _, _, _, teamID = Spring.GetPlayerInfo(ghost.player)
  ghost.unitDefID = UnitDefNames[ghost.unitDefName].id
  ghost.teamID = teamID
end


local startTime = Spring.GetTimer()

function gadget:DrawWorld()
  for ghost in pairs(ghosts) do
    gl.PushMatrix()
    local t = Spring.DiffTimers(Spring.GetTimer(), startTime)
    local alpha = (math.sin(t*6)+1)/6+1/3 -- pulse
    gl.Color(0.7, 0.7, 1, alpha)
    gl.Translate(ghost.x, Spring.GetGroundHeight(ghost.x, ghost.y), ghost.y)
    gl.DepthTest(true)
    gl.UnitShape(ghost.unitDefID, ghost.teamID)
    gl.PopMatrix()
  end
end


function getDistance(x1, z1, x2, z2)
  local x, z = x2 - x1, z2- z1
  return (x*x + z*z)^0.5
end


function gadget:UnitFinished(unitID, unitDefID, unitTeam)
  local x, y, z = Spring.GetUnitPosition(unitID)
  for ghost in pairs(ghosts) do
    if unitDefID == ghost.unitDefID and getDistance(x, z, ghost.x, ghost.y) < 100 then -- ghost.y is no typo
      local groups = {}
      for i=1, #ghost do
        groups[ghost[i]] = true
      end
      local args = {
        type = "notifyGhostBuilt",
        unitID = unitID,
        groups = groups,
        teamID = ghost.teamID,
      }
      Spring.SendLuaRulesMsg(magic..table.show(args))
      ghosts[ghost] = nil
      break
    end
  end
end


function ScoreEvent()
  local teamID = Spring.GetLocalTeamID()
  local score = Spring.GetTeamRulesParam(teamID, "score") or 0
  if score then
    print("SCORE: "..GG.Base64Encode(tostring(Spring.GetGameFrame()).."/"..tostring(math.floor(score))))
  end
end

function SendMissionObjectives()
  if Script.LuaUI("MissionObjectivesFromSynced") then
    local objectives = MakeRealTable(SYNCED.mission.objectives)
    Script.LuaUI.MissionObjectivesFromSynced(objectives)
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function gadget:Initialize()
  gadgetHandler:AddSyncAction('MissionEvent', WrapToLuaUI)
  gadgetHandler:AddSyncAction('GhostEvent', GhostEvent)
  gadgetHandler:AddSyncAction('ScoreEvent', ScoreEvent)
  gadgetHandler:AddSyncAction('SendMissionObjectives', SendMissionObjectives)
  for _,callIn in pairs(callInList) do
    local fun = gadget[callIn]
    gadgetHandler:AddSyncAction(callIn, fun)
  end
end

function gadget:Shutdown()
  gadgetHandler:RemoveSyncAction('MissionEvent')
  gadgetHandler:RemoveSyncAction('GhostEvent')
  gadgetHandler:RemoveSyncAction('ScoreEvent')
  gadgetHandler:RemoveSyncAction('SendMissionObjectives')
end

function gadget:Save(zip)
  if not GG.SaveLoad then
    Spring.Log(gadget:GetInfo().name, LOG.WARNING, "Save/Load API not found")
    return
  end
  
  local toSave = MakeRealTable(SYNCED.mission)
  toSave.displayedCountdowns = MakeRealTable(SYNCED.displayedCountdowns)
  toSave.factoryExpectedUnits = MakeRealTable(SYNCED.factoryExpectedUnits)
  toSave.repeatFactoryGroups = MakeRealTable(SYNCED.repeatFactoryGroups)  
  
  GG.SaveLoad.WriteSaveData(zip, SAVE_FILE, toSave)
end
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
end