function widget:GetInfo()
  return {
    name      = "Beauty Shot",
    desc      = "Positions camera for glamor shot of a unit",
    author    = "KingRaptor (L.J. Lim)",
    date      = "2012.12.17",
    license   = "Public Domain",
    layer     = 0,
    enabled   = true,
  }
end

local defaults = {time = 0, maxCamOffset = 10, minHeading = 15, maxHeading = 165, minPitch = -10, maxPitch = 15, minDistance = 150, maxDistance = 200}
local MAX_TRIES = 50
local tau = 2*math.pi

-- workaround bug where negative args throw an empty interval error
-- fixes https://github.com/ZeroK-RTS/Zero-K/issues/2906
local function randomCustom(min, max)
  if min == max then
    return min
  end
  if min > max then
    min, max = max, min
  end
  local distFromZero = 0
  if min < 0 then
    distFromZero = -min
  end
  return math.random(min + distFromZero, max + distFromZero) - distFromZero
end

local function BeautyShot(unitID, params)
  params = params or {}
  for i,v in pairs(defaults) do
    params[i] = params[i] or v
  end
  
  local validUnit
  local x, y, z = params.x, params.y, params.z
  local spec = Spring.GetSpectatingState()
  local unitExists = unitID and Spring.ValidUnitID(unitID)
  if unitExists and (spec or Spring.GetUnitLosState(unitID, Spring.GetMyAllyTeamID()).los) then
    validUnit = true
  end
  if not (x and y and z) then
    if validUnit then
      _,_,_,x,y,z = Spring.GetUnitPosition(unitID, true)
    else
      Spring.Log(widget:GetInfo().name, LOG.ERROR, "No valid unit and no position, cannot make beauty shot")
      return
    end
  end
  for i=1,MAX_TRIES do
    local camFromTargetHeading = randomCustom(params.minHeading, params.maxHeading)
    if math.random() > 0.5 then
      camFromTargetHeading = -camFromTargetHeading
    end
    camFromTargetHeading = math.rad(camFromTargetHeading)
    if validUnit then
      local unitHeading = Spring.GetUnitHeading(unitID)*tau/65536
      camFromTargetHeading = camFromTargetHeading + unitHeading
    end
    local angleMod = randomCustom(-params.maxCamOffset, params.maxCamOffset)
    angleMod = math.rad(angleMod)
    local dist2d = params.distance or randomCustom(params.minDistance,params.maxDistance)
    if validUnit then
      dist2d = dist2d + Spring.GetUnitRadius(unitID)
    end
    local camPitch = randomCustom(params.minPitch, params.maxPitch)
    camPitch = math.rad(camPitch)
    local dy = dist2d*math.tan(camPitch)
    --local dist = (dist2d^2 + dy^2)^0.5
    local dx = dist2d*math.sin(camFromTargetHeading)
    local dz = dist2d*math.cos(camFromTargetHeading)
    local camHeading = camFromTargetHeading - math.pi
    local rx = -camPitch
    local px, py, pz = x + dx, y + dy, z + dz
    if py > (Spring.GetGroundHeight(px, pz) + 40) then
      Spring.SetCameraState({px = x + dx, py = y + dy, pz = z + dz, rx = rx, ry = camHeading + angleMod}, params.time)
      break
    end
  end
end

WG.BeautyShot = BeautyShot
