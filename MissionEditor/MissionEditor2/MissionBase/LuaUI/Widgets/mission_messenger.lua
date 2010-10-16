-- $Id: mission_messenger.lua 3171 2008-11-06 09:06:29Z det $
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:GetInfo()
  return {
    name      = "Messenger",
    desc      = "Displays Messages. Click on them to make them go away.",
    author    = "quantum",
    date      = "June 29, 2007",
    license   = "GPL v2 or later",
    layer     = -4,
    enabled   = true,  --  loaded by default?
  }
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local viewSizeX, viewSizeY
local gl, GL = gl, GL
local fontHandler = loadstring(VFS.LoadFile(LUAUI_DIRNAME.."modfonts.lua", VFS.ZIP_FIRST))()
local messages = {}

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function Split(s, separator)
  local results = {}
  for part in s:gmatch("[^"..separator.."]+") do
    results[#results + 1] = part
  end
  return results
end

-- remove first n elemets from t, return them
local function Take(t, n)
  local removed = {}
  for i=1, n do
    removed[#removed+1] = table.remove(t, 1)
  end
  return removed
end

-- appends t1 to t2 in-place
local function Append(t1, t2)
  local l = #t1
  for i = 1, #t2 do
    t1[i + l] = t2[i]
  end
end

local function WordWrap(text, font, maxWidth, size)
  fontHandler.UseFont(font)
  text = text:gsub("\r\n", "\n")
  local spaceWidth = fontHandler.GetTextWidth(" ", size)
  local allLines = {}
  local paragraphs = Split(text, "\n")
  for _, paragraph in ipairs(paragraphs) do
    lines = {}
    local words = Split(paragraph, "%s")
    local widths = {}
    for i, word in ipairs(words) do
      widths[i] = fontHandler.GetTextWidth(fontHandler.StripColors(word), size)
    end
    repeat
      local width = 0
      local i = 1
      for j=1, #words do
        newWidth = width + widths[j]
        if (newWidth > maxWidth) then
          break
        else
          width = newWidth + spaceWidth
        end
        i = j
      end
      Take(widths, i)
      lines[#lines+1] = table.concat(Take(words, i), " ")
    until (i > #words)
    if (#words > 0) then
      lines[#lines+1] = table.concat(words, " ")
    end
    Append(allLines, lines)
  end
  return allLines
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local function DrawBox(width, height)
	gl.Color(0, 0, 0, 0.5)
	gl.Vertex(0.5, 0.5)
  gl.Color(0, 0, 0, 0.8)
	gl.Vertex(0.5, height + 0.5)
  gl.Color(0, 0, 0, 0.5)
	gl.Vertex(width + 0.5, height + 0.5)
  gl.Color(0.4, 0.4, 0.4, 0.5)
	gl.Vertex(width+ 0.5, 0.5)
end

local function DrawBorders(width, height)
	gl.Color(1, 1, 1, 1)
	gl.Vertex(0.5, 0.5)
	gl.Vertex(0.5, height + 0.5)
	gl.Vertex(width + 0.5, height + 0.5)
	gl.Vertex(width + 0.5, 0.5)
end

local function List(width, height, texture)
  if (texture) then
    gl.Color(1, 1, 1, 1)
    gl.Texture(texture)
    gl.TexRect(0, 0, width, height)
  else
    gl.BeginEnd(GL.QUADS, DrawBox, width, height)
    gl.BeginEnd(GL.LINE_LOOP, DrawBorders, width, height)
  end
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local Message  = {
  font      = "LuaUI/Fonts/FreeSansBold_16",
  marginX   = 20,
  marginY   = 20,
  spacing   = 10,
  width     = 300,
  height    = 100,
  x1        = 200,
  y1        = 200,
  autoSize  = true,
  relX      = 0.5,
  relY      = 0.5,
  pause     = false,
}
Message.__index = Message
WG.Message = Message

function Message:Show(m)
  setmetatable(m, self)
  fontHandler.UseFont(m.font)
  m.fontSize = fontHandler.GetFontSize()
  messages[m] = true
  if (type(m.text) == 'string') then
    m.text = WordWrap(m.text, m.font, m.width - m.marginX * 2)
  end
  if (m.text and m.autoSize and #m.text > 0 and not m.texture) then
    local maxWidth = 0
    for _, line in ipairs(m.text) do
      maxWidth = math.max(maxWidth, fontHandler.GetTextWidth(fontHandler.StripColors(line), m.fontSize))
    end
    m.width = maxWidth + m.marginX * 2
    m.height = #m.text * m.fontSize + (#m.text - 1) * m.spacing + m.marginY * 2
  end
  m.displayList = gl.CreateList(List, m.width, m.height, m.texture)
  local _, _, paused = Spring.GetGameSpeed()
  if m.pause and not paused then
    Spring.SendCommands"pause"
  end
  return message
end


function Message:Draw(viewSizeX, viewSizeY)
  if (self.relX) then
    self.x1 = self.relX*viewSizeX-self.width/2
  end
  if (self.relY) then
    self.y1 = self.relY*viewSizeY-self.height/2
  end
  self.x1 = math.floor(self.x1)
  self.y1 = math.floor(self.y1) 
  gl.PushMatrix()
  gl.Translate(self.x1, self.y1, 0)
  gl.CallList(self.displayList)
  if (self.text) then
    -- gl.Translate(-0.5, -0.5, 0)
    fontHandler.UseFont(self.font)
    gl.Color(1, 1, 1, 1)
    for i, line in ipairs(self.text) do
      local x = self.marginX
      local y = self.height - self.marginY - 
                self.fontSize * i -
                self.spacing * (i - 1)
      fontHandler.Draw(line, x, y)
    end
  end
  gl.PopMatrix()
end


function Message:Delete()
  local _, _, paused = Spring.GetGameSpeed()
  if self.pause and paused then
    Spring.SendCommands"pause"
  end
  gl.DeleteList(self.displayList)
  messages[self] = nil
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

function widget:DrawScreen()
  local viewSizeX, viewSizeY = gl.GetViewSizes()
  for message in pairs(messages) do
    message:Draw(viewSizeX, viewSizeY)
  end
end


function widget:MousePress(x, y, button)
  local capture
  for message in pairs(messages) do
    if ( x > message.x1 and x < message.x1 + message.width   and
         y > message.y1 and y < message.y1 + message.height) then
      message.closing  = true
      capture = true
    end
  end
  return capture
end


function widget:MouseRelease(x, y, button)
  local capture
  for message in pairs(messages) do
    if (message.closing and
         x > message.x1 and x < message.x1 + message.width   and
         y > message.y1 and y < message.y1 + message.height) then
      capture = true
      message:Delete()
    end
  end
  return capture
end

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------