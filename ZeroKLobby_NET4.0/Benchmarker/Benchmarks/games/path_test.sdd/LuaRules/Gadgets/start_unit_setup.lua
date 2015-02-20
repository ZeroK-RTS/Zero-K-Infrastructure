function gadget:GetInfo()
  return {
    name      = "StartSetup",
    desc      = "Implements initial setup: start units, resources, boost and plop for construction",
    author    = "Licho, CarRepairer, Google Frog, SirMaverick",
    date      = "2008-2010",
    license   = "GNU GPL, v2 or later",
    layer     = 0,
    enabled   = true  --  loaded by default?
  }
end

-- nothing here; this is just to override ZK's base gadget
return