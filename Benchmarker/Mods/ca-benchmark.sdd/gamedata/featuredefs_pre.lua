--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local system = VFS.Include('gamedata/system.lua')

local luaFiles = {}
do
  luaFiles      = VFS.DirList('features/', '*.lua')
end

for _, filename in ipairs(luaFiles) do
  local fdEnv = {}
  fdEnv._G = fdEnv
  fdEnv.Shared = shared
  fdEnv.GetFilename = function() return filename end
  setmetatable(fdEnv, { __index = system })
  local success, fds = pcall(VFS.Include, filename, fdEnv)
  if (not success) then
    Spring.Echo('Error parsing ' .. filename .. ': ' .. fds)
  elseif (fds == nil) then
    Spring.Echo('Missing return table from: ' .. filename)
  else
    for fdName, fd in pairs(fds) do
      if ((type(fdName) == 'string') and (type(fd) == 'table')) then
        fd.filename = filename
        FeatureDefs[fdName] = fd
      end
    end
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
