--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Monitors Unit Camera",
    desc      = "For the \"Unit Is Visible\" condition.",
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

if not FindFirstLogic("UnitIsVisibleCondition") then
  return
end


local timer = 0
function widget:Update(dt)
  timer = timer + dt
  if timer > 3 then
    timer = 0
    for _, unitID in ipairs(Spring.GetVisibleUnits()) do
      if Spring.GetUnitRulesParam(unitID, "notifyvisible") == 1 then
        Spring.SendLuaRulesMsg("notifyvisible "..unitID)
      end
    end
  end
end



--------------------------------------------------------------------------------
--------------------------------------------------------------------------------