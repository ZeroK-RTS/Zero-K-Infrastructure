-- $Id: mission_runner.lua 3171 2008-11-06 09:06:29Z det $
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

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--
-- SYNCED
--
if (gadgetHandler:IsSyncedCode()) then 
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local mission = VFS.Include("mission.lua")
local triggers = mission.triggers -- array
local allTriggers = {unpack(triggers)} -- we'll never remove triggers from here, so the indices will stay correct
local unitGroups = {} -- key: unitID, value: group set (an array of strings)
local gaiaTeamID = Spring.GetGaiaTeamID()
local cheatingWasEnabled = false
local scoreSent = false
local scores = {}
local gameStarted = false
local isInCutscene = false
local events = {} -- key: frame, value: event array
local counters = {} -- key: name, value: count
local countdowns = {} -- key: name, value: frame
local displayedCountdowns = {} -- key: name
local lastFinishedUnits = {} -- key: teamID, value: unitID
local allowTransfer = false
local factoryExpectedUnits = {} -- key: factoryID, value: {unitDefID, groups: group set}
local repeatFactoryGroups = {} -- key: factoryID, value: group set
local objectives = {}	-- key: ID, value: {title, description, color, target}	-- important: widget must be able to access on demand

GG.mission = {
  scores = scores,
  counters = counters,
  countdowns = countdowns,
  unitGroups = unitGroups,
}

for _, counter in ipairs(mission.counters) do
  counters[counter] = 0
end

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


local function SetCount(set)
  local count = 0
  for _ in pairs(set) do
    count = count + 1
  end
  return count
end


local function GetUnitsInRegion(region, teamID)
  local regionUnits = {}
  if Spring.GetTeamInfo(teamID) then
    for _, area in ipairs(region.areas) do
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


local function SetUnitGroup(unitID, group)
  unitGroups[unitID] = unitGroups[unitID] or {}
  unitGroups[unitID][group] = true
end


local function FindUnitsInGroup(searchGroup)
  local results = {}
  if StartsWith(searchGroup, "Units in ") then
    for _, region in ipairs(mission.regions) do
      for playerIndex, playerName in ipairs(mission.players) do
        if searchGroup == string.format("Units in %s (%s)", region.name, playerName) then
          local teamID = playerIndex - 1
          return GetUnitsInRegion(region, teamID)          
        end
      end
    end
  elseif StartsWith(searchGroup, "Latest Factory Built Unit (") then
    for playerIndex, player in ipairs(mission.players) do
      if searchGroup == "Latest Factory Built Unit ("..player..")" then
        local teamID = playerIndex
        if lastFinishedUnits[teamID] then
          results[lastFinishedUnits[teamID]] = true
        end
      end
    end
    return results
  end
  -- static group
  for unitID, groups in pairs(unitGroups) do
    if groups[searchGroup] then
      results[unitID] = true
    end
  end
  return results
end

local function FindUnitInGroup(searchGroup)
  local results = FindUnitsInGroup(searchGroup)
  for unitID in pairs(results) do
    return unitID
  end
end

GG.mission.FindUnitsInGroup = FindUnitsInGroup
GG.mission.FindUnitInGroup = FindUnitInGroup

local function FindUnitsInGroups(searchGroups)
  local results = {}
  for searchGroup in pairs(searchGroups) do
    results = MergeSets(results, FindUnitsInGroup(searchGroup))
  end
  return results
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

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

local disabledUnitDefIDs = {}
for _, disabledUnitName in ipairs(mission.disabledUnits) do
  disabledUnitDefIDs[UnitDefNames[disabledUnitName].id] = true
end


local selectedUnitConditionGroups = {}
for condition in pairs(FindAllLogic("UnitSelectedCondition")) do
  selectedUnitConditionGroups = MergeSets(selectedUnitConditionGroups, condition.args.groups)
end

local unitIsVisibleConditionGroups = {}
for condition in pairs(FindAllLogic("UnitIsVisibleCondition")) do
  unitIsVisibleConditionGroups = MergeSets(unitIsVisibleConditionGroups, condition.args.groups)
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




--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function UpdateDisabledUnits(unitID)
  local unitDefID = Spring.GetUnitDefID(unitID)
  for _, buildID in ipairs(UnitDefs[unitDefID].buildOptions) do
    local cmdDescID = Spring.FindUnitCmdDesc(unitID, -buildID)
    if cmdDescID then
      Spring.EditUnitCmdDesc(unitID, cmdDescID, {disabled = disabledUnitDefIDs[buildID] or false})
    end
  end
end


local function UpdateAllDisabledUnits()
  for _, unitID in ipairs(Spring.GetAllUnits()) do
    UpdateDisabledUnits(unitID)
  end
end

local function AddEvent(frame, event)
  events[frame] = events[frame] or {}
  table.insert(events[frame], event)
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


local function ExecuteTrigger(trigger, frame)
  if not trigger.enabled then return end
  if math.random() < trigger.probability then
    if trigger.maxOccurrences == trigger.occurrences then
      RemoveTrigger(trigger) -- the trigger is no longer needed
      return
    end
    local createdUnits = {}
    local frame = frame or (Spring.GetGameFrame() + 1) -- events will take place at this frame
    for _, action in ipairs(trigger.logic) do
      local Event
      --Spring.Echo(action.logicType, action.name)
      if action.logicType == "CustomAction" then
        Event = function()
          if action.name == "my custom action name" then
            -- fill in your custom actions
          end
        end
	  elseif action.logicType == "CustomAction2" then
        Event = function()
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
        end
      elseif action.logicType == "DestroyUnitsAction" then
        Event = function()
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.DestroyUnit(unitID, true, not action.args.explode)
          end
        end
      elseif action.logicType == "ExecuteTriggersAction" then
        Event = function()
          for _, triggerIndex in ipairs(action.args.triggers) do
              ExecuteTrigger(allTriggers[triggerIndex])
          end
        end
      elseif action.logicType == "ExecuteRandomTriggerAction" then
        Event = function()
          local triggerIndex = TakeRandomI(action.args.triggers)
          ExecuteTrigger(allTriggers[triggerIndex])
        end
      elseif action.logicType == "AllowUnitTransfersAction" then
        Event = function()
          allowTransfer = true
        end
      elseif action.logicType == "TransferUnitsAction" then
        Event = function()
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            SpecialTransferUnit(unitID, action.args.player, false)
          end
        end
      elseif action.logicType == "ModifyResourcesAction" then
        Event = function()  
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
        end
      elseif action.logicType == "ModifyUnitHealthAction" then
        Event = function()
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.AddUnitDamage(unitID, action.args.damage)
          end
        end
      elseif action.logicType == "MakeUnitsAlwaysVisibleAction" then
        Event = function()
          for unitID in pairs(FindUnitsInGroup(action.args.group)) do
            Spring.SetUnitAlwaysVisible(unitID, true)
          end
        end
      elseif action.logicType == "ModifyCounterAction" then
        Event = function()
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
        end
      elseif action.logicType == "DisplayCountersAction" then
        Event = function()
          for counter, value in pairs(counters) do
            Spring.Echo(string.format("Counter %s: %f", counter, value))
          end
        end
      elseif action.logicType == "ModifyScoreAction" then
        Event = function()
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
        end
      elseif action.logicType == "EnableTriggersAction" then
        Event = function()
          for _, triggerIndex in ipairs(action.args.triggers) do
              allTriggers[triggerIndex].enabled = true
          end
        end
      elseif action.logicType == "DisableTriggersAction" then
        Event = function()
          for _, triggerIndex in ipairs(action.args.triggers) do
            allTriggers[triggerIndex].enabled = false
          end
        end
      elseif action.logicType == "WaitAction" then
        frame = frame + action.args.frames
      elseif action.logicType == "StartCountdownAction" then
        Event = function()
          local expiry = Spring.GetGameFrame() + action.args.frames
          countdowns[action.args.countdown] = expiry
          if action.args.display then
            displayedCountdowns[action.args.countdown] = true
            Spring.SetGameRulesParam("countdown:"..action.args.countdown, expiry)
          end
        end
      elseif action.logicType == "CancelCountdownAction" then
        Event = function()
          countdowns[action.args.countdown] = nil
          displayedCountdowns[action.args.countdown] = nil
          Spring.SetGameRulesParam("countdown:"..action.args.countdown, "-1")
        end
      elseif action.logicType == "ModifyCountdownAction" then
        Event = function()
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
        end
      elseif action.logicType == "CreateUnitsAction" then
        Event = function()
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
        end
      elseif action.logicType == "ConsoleMessageAction" then
        Event = function()
          Spring.SendMessage(action.args.message)
        end
      elseif action.logicType == "DefeatAction" then
        Event = function()
          Spring.Echo("defeating")
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
        end
      elseif action.logicType == "VictoryAction" then
        Event = function()
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
        end
      elseif action.logicType == "LockUnitsAction" then
        Event = function()
          for _, disabledUnitName in ipairs(action.args.units) do
            local disabledUnit = UnitDefNames[disabledUnitName]
            if disabledUnit then
              disabledUnitDefIDs[disabledUnit.id] = true
            end
          end
          UpdateAllDisabledUnits()
        end
      elseif action.logicType == "UnlockUnitsAction" then
        Event = function()
          for _, disabledUnitName in ipairs(action.args.units) do
            local disabledUnit = UnitDefNames[disabledUnitName]
            if disabledUnit then
              disabledUnitDefIDs[disabledUnit.id] = nil
            end
          end
          UpdateAllDisabledUnits()
        end
      elseif action.logicType == "PauseAction" or
             action.logicType == "MarkerPointAction" or 
             action.logicType == "SetCameraPointTargetAction" or
             action.logicType == "SetCameraPosDirAction" or
             action.logicType == "SaveCameraStateAction" or
             action.logicType == "RestoreCameraStateAction" or
             action.logicType == "ShakeCameraAction" or
             action.logicType == "GuiMessageAction" or
             action.logicType == "GuiMessagePersistentAction" or
             action.logicType == "HideGuiMessagePersistentAction" or
             action.logicType == "ConvoMessageAction" or
             action.logicType == "ClearConvoMessageQueueAction" or
             action.logicType == "AddObjectiveAction" or
             action.logicType == "ModifyObjectiveAction" or
             action.logicType == "SoundAction" or
             action.logicType == "MusicAction" or
             action.logicType == "StopMusicAction" or
             action.logicType == "SunriseAction" or 
             action.logicType == "SunsetAction" or
             action.logicType == "EnterCutsceneAction" or
             action.logicType == "LeaveCutsceneAction" or
	     action.logicType == "FadeOutAction" or
             action.logicType == "FadeInAction" then
        Event = function()
          action.args.logicType = action.logicType
          _G.missionEventArgs = action.args
          SendToUnsynced"MissionEvent"
          _G.missionEventArgs = nil
        end
	if action.logicType == "AddObjectiveAction" then
	  objectives[action.args.id] = {title = action.args.title, description = action.args.description}
	elseif action.logicType == "ModifyObjectiveAction" then
	  if objectives[action.args.id] then
            -- TBD
	  end
        elseif action.logicType == "EnterCutsceneAction" then
          isInCutscene = true
        elseif action.logicType == "LeaveCutsceneAction" then
          isInCutscene = false
	end
      elseif action.logicType == "GiveOrdersAction" then
        Event = function()
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
              local options
              if i == 1 then
                options = {}
              else
                options = {"shift"}
              end
              Spring.GiveOrderToUnit(unitID, CMD[order.orderType], {x, y, z}, options)
            end
          end
        end
      elseif action.logicType == "GiveFactoryOrdersAction" then
        Event = function()
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
        end      
      elseif action.logicType == "SendScoreAction" then
        Event = function()
          if not (cheatingWasEnabled or scoreSent) then
            SendToUnsynced("ScoreEvent")
            scoreSent = true
          end
        end
      elseif action.logicType == "SetCameraUnitTargetAction" then
        Event = function()
          for unitID, groups in pairs(unitGroups) do
            if groups[action.args.group] then
              local x, _, y = Spring.GetUnitPosition(unitID)
              local args = {
                x = x,
                y = y,
                logicType = "SetCameraPointTargetAction",
              }
              _G.missionEventArgs = args
              SendToUnsynced("MissionEvent")
              _G.missionEventArgs = nil
            end
          end
        end
      end
      if Event then
        AddEvent(frame, Event) -- schedule event
      end
    end
  end
  trigger.occurrences = trigger.occurrences + 1
  if trigger.maxOccurrences == trigger.occurrences then
    RemoveTrigger(trigger) -- the trigger is no longer needed
  end
end


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




--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function gadget:UnitDamaged(unitID, unitDefID, unitTeam, damage, paralyzer, 
                            weaponID, attackerID, attackerDefID, attackerTeam)
  for triggerIndex=1, #triggers do
    local trigger = triggers[triggerIndex]
    for conditionIndex=1, #trigger.logic do
      local condition = trigger.logic[conditionIndex]
      if condition.logicType == "UnitDamagedCondition" and
         --not paralyzer and
         (Spring.GetUnitHealth(unitID) < condition.args.value) and
         (condition.args.anyAttacker or FindUnitsInGroup(condition.args.attackerGroup)[attackerID]) and
         (condition.args.anyVictim or FindUnitsInGroup(condition.args.victimGroup)[unitID]) then
        ExecuteTrigger(trigger)
        break
      end
    end
  end
end


function gadget:AllowUnitTransfer(unitID, unitDefID, oldTeam, newTeam, capture)
  return capture or allowTransfer
end


function gadget:GameFrame(n)

  if not gameStarted then
    -- start with a clean slate
    for _, unitID in ipairs(Spring.GetAllUnits()) do
      if Spring.GetUnitTeam(unitID) ~= gaiaTeamID then
        Spring.DestroyUnit(unitID, false, true)
      end
    end
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
      Event(n) -- run event
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


function gadget:UnitDestroyed(unitID)
  if unitGroups[unitID] then
    for _, trigger in ipairs(triggers) do
      for _, condition in ipairs(trigger.logic) do
        if condition.logicType == "UnitDestroyedCondition" and 
          DoSetsIntersect(condition.args.groups, unitGroups[unitID]) then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
    unitGroups[unitID] = nil
  end
  factoryExpectedUnits[unitID] = nil
  repeatFactoryGroups[unitID] = nil
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
        (condition.args.unitDefIDs[unitDefID] or not condition.args.units[0]) then
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
        (condition.args.unitDefIDs[unitDefID] or not condition.args.units[0]) then
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
        (condition.args.unitDefIDs[unitDefID] or not condition.args.units[0]) then
        if not next(condition.args.players) or ArrayContains(condition.args.players, unitTeam) then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
  end
  UpdateDisabledUnits(unitID)
end



function gadget:AllowCommand(unitID, unitDefID, teamID, cmdID, cmdParams, cmdOptions, cmdTag, synced)
  --if isInCutscene and (not synced) then
  --  return false
  --end
  -- prevent widgets from building disabled units
  if disabledUnitDefIDs[-cmdID] then
    return false
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

--[[
function gadget:UnitEnteredLos(unitID, unitTeam, allyTeam, unitDefID)
  for i=1,#triggers do
    for j=1,#triggers[i].logic do
      local condition = triggers[i].logic[j]
      if condition.logicType == "UnitEnteredLosCondition" then
        if not next(condition.args.players) or ArrayContains(condition.args.players, teamID) then
          ExecuteTrigger(trigger)
          break
        end
      end
    end
  end
end
]]--

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- 
-- UNSYNCED
--
else
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function WrapToLuaUI()
  if Script.LuaUI"MissionEvent" then
    local missionEventArgs = {}
    for k, v in spairs(SYNCED.missionEventArgs) do
      missionEventArgs[k] = v
    end
    Script.LuaUI.MissionEvent(missionEventArgs)
  end
end


local ghosts = {}


function GhostEvent()
  local ghost= {}
  for k, v in spairs(SYNCED.ghostEventArgs) do
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
  local score = Spring.GetTeamRulesParam(teamID, "score")
  if score then
    print("SCORE: "..GG.Base64Encode(tostring(Spring.GetGameFrame()).."/"..tostring(math.floor(score))))
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function gadget:Initialize()
  gadgetHandler:AddSyncAction('MissionEvent', WrapToLuaUI)
  gadgetHandler:AddSyncAction('GhostEvent', GhostEvent)
  gadgetHandler:AddSyncAction('ScoreEvent', ScoreEvent)
  for _,callIn in pairs(callInList) do
      local fun = gadget[callIn]
      gadgetHandler:AddSyncAction(callIn, fun)
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
end
