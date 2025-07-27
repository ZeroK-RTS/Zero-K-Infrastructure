--
-- Custom Options Definition Table format
--
-- A detailed example of how this format works can be found
-- in the spring source under:
-- AI/Skirmish/NullAI/data/AIOptions.lua
--
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

local options = {
	{ -- section
		key    = 'performance',
		name   = 'Performance Relevant Settings',
		desc   = 'These settings may be relevant for both CPU usage and AI difficulty.',
		type   = 'section',
	},
	{ -- bool
		key     = 'cheating',
		name    = 'LOS cheating',
		desc    = 'Enable LOS cheating',
		type    = 'bool',
		section = 'performance',
		def     = false,
	},
	{ -- bool
		key     = 'ally_aware',
		name    = 'Alliance awareness',
		desc    = 'Consider allies presence while making expansion desicions',
		type    = 'bool',
		section = 'performance',
		def     = true,
	},
	{ -- bool
		key     = 'comm_merge',
		name    = 'Merge neighbour Circuits',
		desc    = 'Merge spatially close Circuit ally commanders',
		type    = 'bool',
		section = 'performance',
		def     = true,
	},
	{ -- number
		key     = 'ally_base',
		name    = 'Avoid building in allied bases',
		desc    = 'Range of an avoidance zone near allied factories',
		type    = 'number',
		section = 'performance',
		def     = 0,  -- ceil(multiples of 128) in elmos
	},
-- 	{ -- number (int->uint)
-- 		key     = 'random_seed',
-- 		name    = 'Random seed',
-- 		desc    = 'Seed for random number generator (int)',
-- 		type    = 'number',
-- 		def     = 1337
-- 	},

	{ -- string
		key     = 'disabledunits',
		name    = 'Disabled units',
		desc    = 'Disable usage of specific units.\nSyntax: armwar+armpw+raveparty\nkey: disabledunits',
		type    = 'string',
		def     = '',
	},
--	{ -- string
--		key     = 'json',
--		name    = 'JSON',
--		desc    = 'Per-AI config.\nkey: json',
--		type    = 'string',
--		def     = '',
--	},

	{ -- list
		key     = 'profile',
		name    = 'Difficulty profile',
		desc    = 'Difficulty or play-style of AI (see init.as).\nkey: profile',
		type    = 'list',
		def     = 'default',
		items   = {
			{
				key  = 'default',
				name = 'Default',
				desc = 'Default config.',
			},
			{
				key  = 'previous',
				name = 'Previous',
				desc = 'Previous default config.',
			},
			{
				key  = 'easy',
				name = 'Easy',
				desc = 'Lobotomized AI.',
			},
		},
	},
}

return options
