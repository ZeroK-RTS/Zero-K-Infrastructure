--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Monitors Unit Selections",
    desc      = "For the \"Unit Selected\" condition.",
    author    = "quantum",
    date      = "October 2010",
    license   = "GNU GPL, v2 or later",
    layer     = 1, 
    enabled   = true  --  loaded by default?
  }
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local mission = VFS.Include("mission.lua")
local triggers = mission.triggers -- array

local function FindFirstLogic(logicType)
  for _, trigger in ipairs(triggers) do
    for _, logicItem in ipairs(trigger.logic) do
      if logicItem.logicType == logicType then
        return logicItem, trigger
      end
    end
  end
end

if not FindFirstLogic("UnitSelectedCondition") then
  return
end



local oldSelection = {}

function UpdateSelection()
  local changed
  local newSelection = Spring.GetSelectedUnits()
  local newSelectionCount = #newSelection
  if (newSelectionCount == #oldSelection) then
    for i=1, newSelectionCount do
      if (newSelection[i] ~= oldSelection[i]) then -- it appears that the order does not change
        changed = true
        break
      end                                          
    end
  else
    changed = true
  end
  if changed then
    for i=1, newSelectionCount do
      local unitID = newSelection[i]
      if Spring.GetUnitRulesParam(unitID, "notifyselect") then
        Spring.SendLuaRulesMsg("notifyselect "..unitID)
      end
    end   
  end
  oldSelection = newSelection
end

function widget:CommandsChanged()
  UpdateSelection()
end










--------------------------------------------------------------------------------
--------------------------------------------------------------------------------