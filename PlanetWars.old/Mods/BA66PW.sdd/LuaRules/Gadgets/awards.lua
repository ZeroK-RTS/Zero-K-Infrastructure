function gadget:GetInfo()
  return {
    name      = "AwardsBA",
    desc      = "Awards players at end of battle with shiny trophies.",
    author    = "CarRepairer",
    date      = "2008-10-15",
    license   = "GNU GPL, v2 or later",
    layer     = 1,
    enabled   = true -- loaded by default?
  }
end

local testMode = false

local function tobool(val)
  local t = type(val)
  if (t == 'nil') then
    return false
  elseif (t == 'boolean') then
    return val
  elseif (t == 'number') then
    return (val ~= 0)
  elseif (t == 'string') then
    return ((val ~= '0') and (val ~= 'false'))
  end
  return false
end


if tobool(Spring.GetModOptions().noawards)  then
  return
end 

local spGetAllyTeamList		= Spring.GetAllyTeamList
local spGetTeamList 		= Spring.GetTeamList
local spIsGameOver			= Spring.IsGameOver

local gaiaTeamID			= Spring.GetGaiaTeamID()

local totalTeams = 0
local totalTeamList = {}
function setUpTotalTeamList()
	local allyTeamList = spGetAllyTeamList()		
	for _, allyTeam in pairs(allyTeamList) do
		local teamList = spGetTeamList(allyTeam)
		
		if teamList then
			for _, team in pairs(teamList) do
				if team ~= gaiaTeamID then
				
					totalTeamList[team] = team
					totalTeams = totalTeams + 1
				
				end
			end
		end
	end
end

-------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------
if (gadgetHandler:IsSyncedCode()) then 
-------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------
local spAreTeamsAllied		= Spring.AreTeamsAllied
local spGetGameSeconds 		= Spring.GetGameSeconds
local spGetTeamStatsHistory	= Spring.GetTeamStatsHistory
local spGetUnitHealth		= Spring.GetUnitHealth
local spGetAllUnits			= Spring.GetAllUnits
local spGetUnitTeam			= Spring.GetUnitTeam
local spGetUnitDefID		= Spring.GetUnitDefID
local spGetUnitExperience	= Spring.GetUnitExperience

local floor     = math.floor

local airDamageList		= {}
local friendlyDamageList= {}
local damageList 		= {}
local empDamageList		= {}
local fireDamageList	= {}
local navyDamageList	= {}
local nuxDamageList		= {}
local defDamageList		= {}
local t3DamageList		= {}
local captureList		= {}
local ouchDamageList	= {}

local expUnitTeam, expUnitDefID, expUnitExp = 0,0,0


local awardList = {}
local sentAwards = false
local teamCount = 0

local boats, t3Units = {}, {}


local staticO_small = {
				armemp=1, cortron=1,
				armbrtha=1, corint=1,
				
				armguard=1, armamb=1,
				cortoast=1, corpun=1,

			}
			
local staticO_big = {
				armsilo=1,	corsilo=1,
				--mahlazer=1, 
				corbuzz=1,armvulc=1,
}
			
local flamerWeaponDefs = {}

function comma_value(amount)
  local formatted = amount
  while true do  
    formatted, k = string.gsub(formatted, "^(-?%d+)(%d%d%d)", '%1,%2')
    if (k==0) then
      break
    end
  end
  return formatted
end

function getMeanDamageExcept(excludeTeam)
	local mean = 0
	local count = 0
	for team,dmg in pairs(damageList) do
		if team ~= excludeTeam 
			and dmg > 100
		then
			mean = mean + dmg
			count = count + 1
		end
	end
	return (count>0) and (mean/count) or 0
end

function getMaxVal(valList)
	local winTeam, maxVal = false,0
	for team,val in pairs(valList) do
		if val and val > maxVal then
			winTeam = team
			maxVal = val
		end
	end
	return winTeam, floor(maxVal)
end

function awardAward(team, awardType, record)
	awardList[team][awardType] = record
	
	if testMode then
		for _,curTeam in pairs(totalTeamList) do
			if curTeam ~= team then	
				awardList[curTeam][awardType] = nil
			end
		end
	end
end

function gadget:Initialize()
	setUpTotalTeamList()
	for _,team in pairs(totalTeamList) do
		airDamageList[team] 	= 0
		friendlyDamageList[team]= 0
		damageList[team] 		= 0
		empDamageList[team] 	= 0
		fireDamageList[team] 	= 0
		navyDamageList[team] 	= 0
		nuxDamageList[team] 	= 0
		defDamageList[team] 	= 0
		t3DamageList[team] 		= 0
		captureList[team]		= 0
		ouchDamageList[team]	= 0
		
		awardList[team] = {}
		
		teamCount = teamCount + 1
	end

	local boatFacs = {'armsy', 'corsy', 'armasy', 'corasy'}
	for _, boatFac in pairs(boatFacs) do
		local udBoatFac = UnitDefNames[boatFac]
		for _, boatDefID in pairs(udBoatFac.buildOptions) do
			boats[boatDefID] = true
		end
	end
	
	local t3Facs = {'armshltx', 'corgant', }
	for _, t3Fac in pairs(t3Facs) do
		local udT3Fac = UnitDefNames[t3Fac]
		for _, t3DefID in pairs(udT3Fac.buildOptions) do
			t3Units[t3DefID] = true
		end
	end
	
	for i=1,#WeaponDefs do
		if (WeaponDefs[i].type=="Flame" or 
			WeaponDefs[i].fireStarter >=100 or 
			WeaponDefs[i].name:lower():find("napalm")) then --// == flamethrower or napalm
			--// 0.5 cus we want to differ trees an metal/tanks 
			--// (fireStarter-tag: 1.0->always flame trees, 2.0->always flame units/buildings too)
			flamerWeaponDefs[i]=WeaponDefs[i].fireStarter
		end
	end

end

function gadget:UnitTaken(unitID, unitDefID, oldTeam, newTeam)
	-- Units given to neutral?
	if not spAreTeamsAllied(oldTeam,newTeam) and captureList[newTeam] then
		local ud = UnitDefs[unitDefID]
		local mCost = ud and ud.metalCost
		captureList[newTeam] = captureList[newTeam] + mCost
	end
end

function gadget:UnitDestroyed(unitID, unitDefID, unitTeam)
	local experience = spGetUnitExperience(unitID)
	if experience > expUnitExp then
		expUnitExp = experience
		expUnitTeam = unitTeam
		expUnitDefID = unitDefID
	end
end

function gadget:UnitDamaged(unitID, unitDefID, unitTeam, fullDamage, paralyzer, weaponID,
		attackerID, attackerDefID, attackerTeam)
	
	if (not attackerTeam) 
		or (attackerTeam == unitTeam)
		or (attackerTeam == gaiaTeamID) 
		or (unitTeam == gaiaTeamID) 
		then return end
	
	local hp = spGetUnitHealth(unitID)
	local damage = (hp > 0) and fullDamage or fullDamage + hp
	
	if spAreTeamsAllied(attackerTeam, unitTeam) then
		if not paralyzer then
			friendlyDamageList[attackerTeam] = friendlyDamageList[attackerTeam] + damage
		end
	else
		if paralyzer then
			empDamageList[attackerTeam] = empDamageList[attackerTeam] + damage
		else
			damageList[attackerTeam] = damageList[attackerTeam] + damage
			ouchDamageList[unitTeam] = ouchDamageList[unitTeam] + damage
			local ad = UnitDefs[attackerDefID]
			
			if (flamerWeaponDefs[weaponID]) then				
				fireDamageList[attackerTeam] = fireDamageList[attackerTeam] + damage	
			end
			
			-- Static Weapons
			if (not ad.canMove) then
			
				-- bignukes, buzzsaw, mahlazer
				if staticO_big[ad.name] then
					nuxDamageList[attackerTeam] = nuxDamageList[attackerTeam] + damage
					
				-- not lrpc, tacnuke, emp missile
				elseif not staticO_small[ad.name] then
					defDamageList[attackerTeam] = defDamageList[attackerTeam] + damage
					
				end
				
			elseif ad.canFly then
				airDamageList[attackerTeam] = airDamageList[attackerTeam] + damage
				
			elseif boats[attackerDefID] then
				navyDamageList[attackerTeam] = navyDamageList[attackerTeam] + damage
			
			elseif t3Units[attackerDefID] then
				t3DamageList[attackerTeam] = t3DamageList[attackerTeam] + damage
			
			
			end	
		end
	end
	
end

function gadget:GameFrame(n)
		
	if testMode then
		local frame32 = (n) % 32
		if (frame32 < 0.1) then
			sentAwards = false
		end
	
	else
		if not spIsGameOver() then return end
	end
	
	if not sentAwards then 
	
		for _, unitID in ipairs(spGetAllUnits()) do
			local teamID = spGetUnitTeam(unitID)
			 local unitDefID = spGetUnitDefID(unitID)
			 gadget:UnitDestroyed(unitID, unitDefID, teamID)
		end
	
		local pwnTeam, 	maxDamage 		= getMaxVal(damageList)		
		local navyTeam, maxNavyDamage 	= getMaxVal(navyDamageList)
		local airTeam, 	maxAirDamage 	= getMaxVal(airDamageList)
		local nuxTeam, 	maxNuxDamage 	= getMaxVal(nuxDamageList)
		local shellTeam,maxStaticDamage = getMaxVal(defDamageList)
		local fireTeam, maxFireDamage 	= getMaxVal(fireDamageList)
		local empTeam, 	maxEmpDamage 	= getMaxVal(empDamageList)
		local t3Team, 	maxT3Damage 	= getMaxVal(t3DamageList)
		
		local ouchTeam, maxOuchDamage 	= getMaxVal(ouchDamageList)
		
		local capTeam, 	maxCap	 		= getMaxVal(captureList)
		
		local friendTeam
		local maxFriendlyDamageRatio = 0
		for team,dmg in pairs(friendlyDamageList) do
			
			local totalDamage = dmg+damageList[team]
			local damageRatio = totalDamage>0 and dmg/totalDamage or 0
			
			if  damageRatio > maxFriendlyDamageRatio then
				friendTeam = team
				maxFriendlyDamageRatio = damageRatio
			end
		end
	
		local mTeam, eTeam
		local mMax, eMax, mTotal, eTotal = 0,0, 0,0
		for _,team in pairs(totalTeamList) do
			local statsTable = spGetTeamStatsHistory(team, 1,spGetTeamStatsHistory(team))
			local mProduced = statsTable[1].metalProduced
			local eProduced = statsTable[1].energyProduced
			
			mTotal = mTotal + mProduced
			eTotal = eTotal + eProduced
			
			if mProduced > mMax then
				mMax = mProduced
				mTeam = team
			end
			if eProduced > eMax then
				eMax = eProduced
				eTeam = team
			end
			
		end
		mMax = floor(mMax)
		eMax = floor(eMax)
		local mAve = mTotal / totalTeams
		local eAve = eTotal / totalTeams
		
		--test values
		if testMode then
			local testteam = 0
			--[[				
			pwnTeam, 	maxDamage 			= testteam+0	,1
			navyTeam, 	maxNavyDamage 		= testteam+1	,1
			t3Team, 	maxT3Damage 		= testteam+1	,1
			capTeam, 	maxCap	 			= testteam+0	,2

			expUnitTeam, expUnitExp			= testteam+0	,2.4444
				expUnitDefID = 35
	
			airTeam, 	maxAirDamage 		= testteam+2	,2
			nuxTeam, 	maxNuxDamage 		= testteam+0	,333333333

			shellTeam, 	maxStaticDamage 	= testteam+0	,1
			mTeam,  mMax 					= testteam+0	,1
			eTeam, eMax						= testteam+0	,11111111
			friendTeam, maxFriendlyDamageRatio = testteam+0	,0.5
			fireTeam, maxFireDamage			= testteam+0	,1
			empTeam, maxEmpDamage			= testteam+0	,1
		
			pwnTeam, 	maxDamage 			= testteam	,1
			navyTeam, 	maxNavyDamage 	= testteam	,1
			airTeam, 	maxAirDamage 		= testteam	,1
			nuxTeam, 	maxNuxDamage 		= testteam	,1
			shellTeam, 	maxStaticDamage = testteam	,1
			mTeam,  mMax 					= testteam	,1
			eTeam, eMax					= testteam	,1
			friendTeam, maxFriendlyDamageRatio = testteam,1
			fireTeam, maxFireDamage		= testteam	,1
			empTeam, maxEmpDamage			= testteam	,1
		--]]	
		end
	
		local easyFactor = 0.5
		local veryEasyFactor = 0.3
		local minFriendRatio = 0.25
		local chunkVal = 0.1

		local mChunk = mTotal * chunkVal
		local eChunk = eTotal * chunkVal
		
		if pwnTeam then
			awardAward(pwnTeam, 'pwn', 'Damage: '.. comma_value(maxDamage))
		end
		if mTeam and mMax > mAve + mChunk then
			awardAward(mTeam, 'metal', 'Income: '.. comma_value(mMax))
		end
		if eTeam and eMax > eAve + eChunk then
			awardAward(eTeam, 'energy', 'Income: '.. comma_value(eMax))
		end
		if navyTeam and maxNavyDamage > getMeanDamageExcept(navyTeam) then
			awardAward(navyTeam, 'navy', 'Damage: '.. comma_value(maxNavyDamage))
		end
		if airTeam and maxAirDamage > getMeanDamageExcept(airTeam) then
			awardAward(airTeam, 'air', 'Damage: '.. comma_value(maxAirDamage))
		end
		if t3Team and maxT3Damage > getMeanDamageExcept(t3Team) then
			awardAward(t3Team, 't3', 'Damage: '.. comma_value(maxT3Damage))
		end
		if nuxTeam and maxNuxDamage > getMeanDamageExcept(nuxTeam) * veryEasyFactor then
			awardAward(nuxTeam, 'nux', 'Damage: '.. comma_value(maxNuxDamage))
		end
		if shellTeam and maxStaticDamage > getMeanDamageExcept(shellTeam) * easyFactor then
			awardAward(shellTeam, 'shell', 'Damage: '.. comma_value(maxStaticDamage))
		end
		if fireTeam and maxFireDamage > getMeanDamageExcept(fireTeam) * easyFactor then
			awardAward(fireTeam, 'fire', 'Damage: '.. comma_value(maxFireDamage))
		end
		if empTeam and maxEmpDamage/10 > getMeanDamageExcept(empTeam) * easyFactor then
			awardAward(empTeam, 'emp', 'Damage: '.. comma_value(maxEmpDamage))
		end
		if capTeam and maxCap > 1000 then
			awardAward(capTeam, 'cap', 'Captured value: '.. comma_value(maxCap))
		end
		if friendTeam and maxFriendlyDamageRatio > minFriendRatio then
			awardAward(friendTeam, 'friend', 'Damage inflicted on allies: '.. floor(maxFriendlyDamageRatio * 100) ..'%')
		end
		if ouchTeam then
			awardAward(ouchTeam, 'ouch', 'Damage: '.. comma_value(maxOuchDamage))
		end
		if expUnitExp >= 3.0 then
			local vetName = UnitDefs[expUnitDefID] and UnitDefs[expUnitDefID].humanName
			local expUnitExpRounded = ''..floor(expUnitExp * 10)/10
			expUnitExpRounded = expUnitExpRounded:sub(1,3)
			awardAward(expUnitTeam, 'vet', vetName ..', '.. expUnitExpRounded ..' XP')
		end
			
		_G.awardList = awardList
		sentAwards = true
	end
end

-------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------
else  -- UNSYNCED
-------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------

local hostName = nil --planetwars hostname

local spGetGameFrame 	= Spring.GetGameFrame
local spGetMouseState 	= Spring.GetMouseState
local spSendCommands	= Spring.SendCommands


local glPushMatrix		= gl.PushMatrix
local glPopMatrix		= gl.PopMatrix
local glTexture			= gl.Texture
local glTexRect			= gl.TexRect
local glAlphaTest		= gl.AlphaTest
local glTranslate		= gl.Translate
local glColor			= gl.Color
local glBeginEnd		= gl.BeginEnd
local glVertex			= gl.Vertex
local glScale			= gl.Scale
local GL_QUADS     		= GL.QUADS
--local GL_GREATER		= GL.GREATER

LUAUI_DIRNAME = 'LuaUI/'
local fontHandler   = loadstring(VFS.LoadFile(LUAUI_DIRNAME.."modfonts.lua", VFS.ZIP_FIRST))()
local fancyFont		= LUAUI_DIRNAME.."Fonts/KOMTXT___16"
local smallFont		= LUAUI_DIRNAME.."Fonts/FreeSansBold_16"

local fhDraw    		= fontHandler.Draw
local fhDrawCentered	= fontHandler.DrawCentered

local caught, b1pressed, windowCaught, buttonHover
local showGameOverWin 	= true
local sentToPlanetWars	= false

local colSpacing		= 250
local bx, by 			= 100,100
local margin 			= 10
local tWidth,tHeight	= 30,40
local w, h 				= (colSpacing+margin)*3, 500
local exitX1,exitY1,exitX2,exitY2 = w-260, h-40, w-10, h-10

local teamNames		= {}
local teamColors	= {}
local teamColorsDim	= {}
local awardList

local maxRow 		= 8
local fontHeight 	= 16


local awardDescs = 
{
	pwn 	= 'Complete Annihilation Award', 
	navy 	= 'Navy Admiral', 
	air 	= 'Airforce General', 
	nux 	= 'Apocalyptic Achievement Award', 
	friend 	= 'Friendly Fire Award', 
	shell 	= 'Turtle Shell Award', 
	metal 	= 'Metal Baron',
	energy 	= 'Energy Tycoon',
	fire 	= 'Master Grill-Chef',
	emp 	= 'EMP Wizard',
	t3 		= 'Experimental Engineer',
	cap 	= 'Capture Award',
	vet 	= 'Decorated Veteran',
	ouch 	= 'Big Purple Heart',
}

function gadget:Initialize()
	setUpTotalTeamList()
	for _,team in pairs(totalTeamList) do		
		local _, leaderPlayerID = Spring.GetTeamInfo(team)
		teamNames[team] = Spring.GetPlayerInfo(leaderPlayerID)
		teamColors[team]  = {Spring.GetTeamColor(team)}
		teamColorsDim[team]  = {teamColors[team][1], teamColors[team][2], teamColors[team][3], 0.5}
	end
	spSendCommands({'endgraph 0'})

	-- planetwars init
	local modOptions = Spring.GetModOptions()
	if (modOptions) and modOptions.planetwars and modOptions.planetwars ~= '' then
		local optionsRaw = modOptions.planetwars
		optionsRaw = string.gsub(optionsRaw, '_', '=')
		optionsRaw = GG.base64dec(optionsRaw)
		options = assert(loadstring(optionsRaw))()
		hostName = options.hostname
	end  
end

local function DrawBox(x1,y1,x2,y2)	
	glVertex(x1, y1)
	glVertex(x2, y1)
	glVertex(x2, y2)
	glVertex(x1, y2)	
end

function gadget:DrawScreen()

	if not testMode then
		if not spIsGameOver() then return end
	end
	
	if (not awardList) and SYNCED.awardList then
		awardList = SYNCED.awardList
	end
	
	local mx, my, b1 = spGetMouseState()
	buttonHover = false
	if mx > bx+exitX1 and mx < bx+exitX2 and my > by+exitY1 and my < by+exitY2 then
		buttonHover = true
		if b1 and not b1pressed then
			
			if showGameOverWin then
				spSendCommands({'endgraph 1'})
			else
				spSendCommands({'endgraph 0'})
			end
			showGameOverWin = not showGameOverWin
			
		end
		
	elseif mx > bx and mx < bx+w and my > by and my < by+h then
		if b1 and not b1pressed then
			windowCaught = true
			
			cx = mx-bx
			cy = my-by
			caught = true
		end
		
	end
	
	if windowCaught then
		bx = mx-cx
		by = my-cy
	end
	
	if b1 then
		b1pressed = true
	else
		b1pressed = false
		windowCaught = false		
	end
		
	fontHandler.UseFont(smallFont)
	
	-- Main Box
	glTranslate(bx,by, 0)
	glColor(0.2, 0.2, 0.2, 0.4)
	glBeginEnd(GL_QUADS, DrawBox, 0,0,w,h)
	
	-- Title
	glColor(1, 1, 0, 0.8)
	glPushMatrix()
	glTranslate(colSpacing,h-fontHeight*2,0)
	glScale(1.5, 1.5, 1.5)
	fhDraw('Awards', 0,0)
	glPopMatrix()
	
	-- Button
	if buttonHover then
		glColor(0.4, 0.4, 0.9, 0.85)
	else
		glColor(0.9, 0.9, 0.9, 0.85)
	end
	glBeginEnd(GL_QUADS, DrawBox, exitX1,exitY1,exitX2,exitY2)
	fhDrawCentered('Show/Hide Stats Window', (exitX1 + exitX2)/2,(exitY1 + exitY2)/2 - fontHeight/2)
	
	glTranslate(margin, h - (tHeight + margin)*2, 0)
	local row, col = 0,0
	if awardList then
		
		local teamCount = 0
		
		for team,awards in spairs(awardList) do
		
			local awardCount = 0
			for awardType, record in spairs(awards) do
				awardCount = awardCount + 1
				if not sentToPlanetWars and hostName ~= nil then
					local planetWarsData = teamNames[team] ..' '.. awardType ..' '.. awardDescs[awardType] ..', '.. record
					Spring.SendCommands("w "..hostName.." pwaward:".. planetWarsData)
					Spring.Echo(planetWarsData)
				end
			end
		
			if awardCount > 0 then
				teamCount = teamCount + 1
				
				if row == maxRow-1 then
					row = 0
					col = col + 1
					glTranslate(margin+colSpacing, (tHeight+margin)*(maxRow-1) , 0)
				end
				
				glColor( teamColorsDim[team] )
				glBeginEnd(GL_QUADS, DrawBox, 0-margin/2, 0-margin/2, colSpacing-margin/2, tHeight+margin/2)
				
				glColor(1,1,1,1)	
				fhDraw(teamNames[team] , 0, fontHeight )
				
				row = row + 1
				glTranslate( 0, 0 - (tHeight+margin), 0)
				if row == maxRow then
					row = 0
					col = col + 1
					glTranslate(margin+colSpacing, (tHeight+margin)*maxRow , 0)
				end
				
				for awardType, record in spairs(awards) do
				
					glColor(teamColorsDim[team] )
					glBeginEnd(GL_QUADS, DrawBox, 0-margin/2, 0-margin/2, colSpacing-margin/2, tHeight+margin/2)
					glColor(1,1,1,1)	
					
					glPushMatrix()
						
						local border = 2
						glColor(0,0,0,1)
						glBeginEnd(GL_QUADS, DrawBox, 0-border, 0-border, tWidth+border, tHeight+border)
						glColor(1,1,1,1)	
						glTexture('LuaRules/Images/awards/trophy_'.. awardType ..'.png')
						glTexRect(0, 0, tWidth, tHeight )
						
						glTranslate(tWidth+margin,(fontHeight+margin),0)
						glColor(1,1,0,1)
						glPushMatrix()
							if awardDescs[awardType]:len() > 35 then
								glScale(0.6,1,1)
							elseif awardDescs[awardType]:len() > 20 then
								glScale(0.8,1,1)
							end
							--fhDraw(awardCount ..') '.. awardDescs[awardType], 0,0) 
							fhDraw(awardDescs[awardType], 0,0) 
						glPopMatrix()
						
						glTranslate(0,0-(fontHeight/2+margin),0)
						glColor(1,1,1,1)
						glPushMatrix()
							if record:len() > 35 then
								glScale(0.6,1,1)
							elseif record:len() > 20 then
								glScale(0.8,1,1)
							end
							
							fhDraw('  '..record, 0,0)
						glPopMatrix()
						
					glPopMatrix()
					
					row = row + 1
					glTranslate( 0, 0 - (tHeight+margin), 0)
					if row == maxRow then
						row = 0
						col = col + 1
						glTranslate(margin+colSpacing, (tHeight+margin)*maxRow , 0)
					end
				end
			end --if at least 1 award
		end
		sentToPlanetWars = true
	end
	glColor(0,0,0,0)
end

function gadget:ViewResize(vsx, vsy)
	bx = vsx/2 - w/2
	by = vsy/2 - h/2
end
-------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------
end