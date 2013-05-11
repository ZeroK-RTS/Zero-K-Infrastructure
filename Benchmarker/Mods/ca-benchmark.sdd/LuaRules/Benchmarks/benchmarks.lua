local fpsCamID = 0;
local ovCamID  = 1;

function ResetCamera(height)
  local groundHeight = Spring.GetGroundHeight(Game.mapSizeX*0.5,Game.mapSizeZ*0.5);
  local pos = { Game.mapSizeX*0.5, groundHeight+(height or 3000) , Game.mapSizeZ*0.5 };
  local dir = { 0, -1, 0 };
  local rot = { -math.pi*0.5, math.pi, 0 };
  Spring.SetCameraState( {mode=fpsCamID, px=pos[1],py=pos[2],pz=pos[3], dx=dir[1],dy=dir[2],dz=dir[3], rx=rot[1],ry=rot[2],rz=rot[3]} , 0.0);
end


local benchmarks = {
  {
    name = "MemoryLeaks1",
    duration = 5,
    waitAfter = 1,
    start = function() ResetCamera() end,
  },

  {
    name = "Terrain Rendering",
    duration = 15,
    waitAfter = 3,
    gameframe = function()
      if not(Spring.GetGameFrame() % 30 < 1) then
        return
      end

      local transTime = 1;

      local x,y = math.random(),math.random();
      local groundHeight = Spring.GetGroundHeight(Game.mapSizeX*x,Game.mapSizeZ*y);
      local pos = { Game.mapSizeX*x, groundHeight , Game.mapSizeZ*y };
      local dir = { 0, math.sqrt(2), -math.sqrt(2)};
      Spring.SetCameraState( {mode=ovCamID, px=pos[1],py=pos[2],pz=pos[3], dx=dir[1],dy=dir[2],dz=dir[3]} , transTime);
    end,
  },

  {
    name = "Terrain Texture Loading",
    duration = 30,
    gameframe = function()
      --if (Spring.GetGameFrame() % 30 ~= 1) then
      --  return
      --end

      local transTime = 0.0;

      local x,y = math.random(),math.random();
      local camHeight = 500;
      local groundHeight = Spring.GetGroundHeight(Game.mapSizeX*x,Game.mapSizeZ*y);
      local pos = { Game.mapSizeX*x, groundHeight+camHeight , Game.mapSizeZ*y };
      local dir = { pos[1]-Game.mapSizeX*0.5, 0 , pos[3]-Game.mapSizeZ*0.5 };
      --local rot = { dir[1]*math.pi*2,dir[2]*math.pi*2,dir[3]*math.pi*2 };
      local rot = { 0, (dir[3]/math.sqrt(dir[1]*dir[1] + dir[3]*dir[3])) * math.pi*2, 0 };
      Spring.SetCameraState( {mode=fpsCamID, px=pos[1],py=pos[2],pz=pos[3], dx=dir[1],dy=dir[2],dz=dir[3], rx=rot[1],ry=rot[2],rz=rot[3]} , transTime);
    end,
    finish = function() ResetCamera() end,
  },

  {
    name = "Feature Creation",
    duration = 3,
    waitAfter = 1,
    start = function()
      local y = 0
      for x=64,Game.mapSizeX-128,(Game.mapSizeX-128)/10 do
        for z=64,Game.mapSizeZ-128,(Game.mapSizeZ-128)/50 do
          Spring.CreateFeature("windgen", x,y,z)
        end
      end
    end,
  },

  {
    name = "Feature Rendering",
    duration = 5,
    waitAfter = 1,
    finish = function()
      local allFeatures = Spring.GetAllFeatures()
      for i=1,#allFeatures do
        Spring.DestroyFeature(allFeatures[i])
      end
    end,
  },

  {
    name = "Unit Creation",
    duration = 3,
    waitAfter = 1,
    start = function()
      local y = 0
      for x=64,Game.mapSizeX-128,(Game.mapSizeX-128)/10 do
        for z=64,Game.mapSizeZ-128,(Game.mapSizeZ-128)/50 do
          Spring.CreateUnit("windgen", x,y,z, "n", Spring.GetGaiaTeamID() )
        end
      end
    end,
  },

  {
    name = "Unit Rendering",
    duration = 5,
    waitAfter = 1,
  },

  {
    name = "Unit Overhead",
    duration = 5,
    waitAfter = 1,
    start = function()
      ResetCamera(0)
    end,
    finish = function()
      ResetCamera()
    end,
  },

  {
    name = "SelfDestruct",
    duration = 4,
    waitAfter = 1,
    start = function()
      local allUnits = Spring.GetAllUnits()
      Spring.GiveOrderToUnitArray(allUnits,CMD.SELFD,{},{})
    end,
    finish = function()
      local allFeatures = Spring.GetAllFeatures()
      for i=1,#allFeatures do
        Spring.DestroyFeature(allFeatures[i])
      end
    end,
  },

  {
    name = "War",
    type = "runtime",
    duration = 10,
    waitAfter = 4,
    start = function()
      local warriors = {"armpw"}
      local teams = 2
      local y = 0
      for x=64,Game.mapSizeX-128,(Game.mapSizeX-128)/50 do
        for z=64,Game.mapSizeZ-128,(Game.mapSizeZ-128)/50 do
          Spring.CreateUnit(warriors[(math.floor(x+z)%#warriors)+1], x,y,z, "n", (math.floor(x+z)%teams)+1 )
        end
      end
    end,
    finish = function()
      local allUnits = Spring.GetAllUnits()
      for i=1,#allUnits do
        Spring.DestroyUnit(allUnits[i],false,true)
      end
      local allFeatures = Spring.GetAllFeatures()
      for i=1,#allFeatures do
        Spring.DestroyFeature(allFeatures[i])
      end
    end,
  },

  {
    name = "MemoryLeaks2",
    duration = 5,
    start = function() ResetCamera() end,
  },
}

return benchmarks