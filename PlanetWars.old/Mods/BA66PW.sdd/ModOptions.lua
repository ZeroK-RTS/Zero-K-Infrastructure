local options={
	{
		key="deathmode",
		name="Game end mode",
		desc="What it takes to eliminate a team",
		type="list",
		def="com",
		items={
			{key="killall", name="Kill Everything", desc="Every last unit must be eliminated, no exceptions!"},
		--{key="minors", name="Nothing of value left", desc="The team has no factories and no units left, just defenses and economy"},
			{key="com", name="Kill all enemy Commanders", desc="When a team has no Commanders left it loses"},
			{key="comcontrol", name="No Commander, No Control", desc="A player without a Commander cannot issue orders"},
		}
	},
  {
    key    = 'planetwars',
    name   = 'Planet Wars Options',
    desc   = 'A string is put here by the Planet Wars server to set up ingame conditions',
    type   = 'string',
    def    = false,
  },
}
return options
