--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
--
--  file:    resultFile.lua
--  brief:   
--  author:  jK
--
--  Copyright (C) 2007.
--  Licensed under the terms of the GNU GPL, v2 or later.
--
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function round(num, idp)
  return tonumber(string.format("%." .. (idp or 0) .. "f", num))
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

output = {}
output.__index = output

function output:WriteLine(text)
  self.file:write("--  " .. text .. string.rep(" ",64-text:len()) .. "--\n")
end

local colline,col="--",1
function output:ColLine(text)
  colline = colline .. "  " .. text .. string.rep(" ",28-text:len()) .. "  --"
  if (col==2) then
    self.file:write(colline.."\n")
    colline="--"
    col=1
  else
    col=2
  end
end


function output:WriteHeader()
  self.file:write( string.rep("-",70) .. "\n" )
  self.file:write( string.rep("-",70) .. "\n" )
  self:WriteLine("")
  self.file:write( "--                 CA-Spring Benchmark Results                      --\n" )
  self:WriteLine("")
  self.file:write( string.rep("-",70) .. "\n" )
  self.file:write( string.rep("-",70) .. "\n" )
  self:WriteLine("")
  self:WriteLine("System Specs")
  self.file:write( string.rep("-",70) .. "\n" )
  self:ColLine("Resolution:   "..Spring.GetConfigInt("XResolution").."x"..Spring.GetConfigInt("YResolution"))
  self:ColLine("DepthBuffer:  "..Spring.GetConfigInt("DepthBufferBits").."bit")
  self:ColLine("DualScreen:   "..Spring.GetConfigInt("DualScreenMode"))
  self:ColLine("FSAA:         ".. (((Spring.GetConfigInt("FSAA")>0)and("on"))or("off")) .. " (" .. Spring.GetConfigInt("FSAALevel") ..")" )
  self:ColLine("GroundDecals: "..Spring.GetConfigInt("GroundDecals"))
  self:ColLine("GroundDetail: "..Spring.GetConfigInt("GroundDetail"))
  self:ColLine("GrassDetail:  "..Spring.GetConfigInt("GrassDetail"))
  self:ColLine("3DTrees:      "..Spring.GetConfigInt("3DTrees"))
  self:ColLine("DynamicSky:   "..Spring.GetConfigInt("DynamicSky"))
  self:ColLine("AdvSky:       "..Spring.GetConfigInt("AdvSky"))
  self.file:write( string.rep("-",70) .. "\n" )
  self:WriteLine("Map: " .. (Game.mapName) )

--[[
  self:WriteLine("Resolution:   "..Spring.GetConfigInt("XResolution").."x"..Spring.GetConfigInt("YResolution"))
  --self:WriteLine("VSync:        "..Spring.GetConfigInt("VSync"))
  self:WriteLine("3DTrees:      "..Spring.GetConfigInt("3DTrees"))
  self:WriteLine("DepthBuffer:  "..Spring.GetConfigInt("DepthBufferBits").."bit")
  self:WriteLine("DualScreen:   "..Spring.GetConfigInt("DualScreenMode"))
  self:WriteLine("DynamicSky:   "..Spring.GetConfigInt("DynamicSky"))
  self:WriteLine("AdvSky:       "..Spring.GetConfigInt("AdvSky"))
  self:WriteLine("FSAA:         "..Spring.GetConfigInt("FSAA"))
  self:WriteLine("FSAALevel:    "..Spring.GetConfigInt("FSAALevel"))
  self:WriteLine("GrassDetail:  "..Spring.GetConfigInt("GrassDetail"))
  self:WriteLine("GroundDecals: "..Spring.GetConfigInt("GroundDecals"))
  self:WriteLine("GroundDetail: "..Spring.GetConfigInt("GroundDetail"))
  self:WriteLine("HighResLos:   "..Spring.GetConfigInt("HighResLos"))
  --self:WriteLine("ReflectiveWater: "..Spring.GetConfigInt("ReflectiveWater"))
  --self:WriteLine(": "..Spring.GetConfigInt(""))
--]]

  self:WriteLine("")
  self.file:write( string.rep("-",70) .. "\n" )
  self.file:write( string.rep("-",70) .. "\n" )
  self:WriteLine("")
  self:WriteLine("Results")
  self:WriteLine("")

  self.file:flush()
end


function output:WriteTest(test,fpsTable)
  self.file:write("-----[ ".. (test.name) .." ]-------------------------------------------------------\n")
  local avgFPS = 0
  local minFPS = math.huge
  local maxFPS = 0
  for i,fps in pairs(fpsTable) do
    --self:WriteLine(fps)
    avgFPS=avgFPS+fps
    if (fps>maxFPS) then maxFPS=fps end
    if (fps<minFPS) then minFPS=fps end
  end
  if ((#fpsTable) > 0) then
    self:WriteLine("min. FPS: " .. round(minFPS,0))
    self:WriteLine("avg. FPS: " .. round(avgFPS/(#fpsTable),0))
    self:WriteLine("max. FPS: " .. round(maxFPS,0))
  end
  self.file:flush()
end


function output:WriteTestRuntime(test,runtime,ideal)
  self.file:write("-----[ ".. (test.name) .." ]-------------------------------------------------------\n")
  self:WriteLine("runtime: " .. math.floor((runtime/ideal)*100) .. "% (" .. runtime .. "s/" .. ideal .. "s)")
  self.file:flush()
end


function output:Open()
--[[
  local error
  self.file,error = io.open("benchmark.txt","w")
  if self.file==nil then
    Spring.Echo(error)
    return false
  end
--]]
  self.file = {write = function(_,...)Spring.Echo(...);end, flush = function() end}
end

function output:Close()
  self.file:write("----------------------------------------------------------------------\n")
  self.file:write("----------------------------------------------------------------------\n")
  self.file:close()
end