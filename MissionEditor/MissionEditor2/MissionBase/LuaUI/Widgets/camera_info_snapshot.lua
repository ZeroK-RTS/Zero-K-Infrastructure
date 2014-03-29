function widget:GetInfo()
  return {
    name      = "Camera Info Snapshot",
    desc      = "Prints your camera position and rotation with F10",
    author    = "KingRaptor (L.J. Lim)",
    date      = "2012.10.27",
    license   = "Public Domain",
    layer     = 0,
    enabled   = true,
  }
end

function widget:KeyPress(key)
  if key == 291 then	-- F10
    local cam = Spring.GetCameraState()
    -- only free cam has rx/ry values, so we need to get them from the dx/dy/dz values for other camera modes
    if not cam.rx then
      cam.rx = cam.dy
    end
    if not cam.ry then
      cam.ry = math.atan2(cam.dz, -cam.dx) - math.pi/2
    end
    Spring.Echo(math.floor(cam.px+0.5), math.floor(cam.py+0.5), math.floor(cam.pz+0.5), math.deg(cam.rx), math.deg(cam.ry))
  end
end