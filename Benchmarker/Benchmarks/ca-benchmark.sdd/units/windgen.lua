unitDef = {
  unitname          = "windgen",
  name              = "Wind Generator",
  activateWhenBuilt = true,
  corpse            = "DEAD",
  objectName        = "windgen.s3o",
  selfDestructAs    = "SMALL_BUILDINGEX",
  explodeAs         = "SMALL_BUILDINGEX",
  selfDestructCountdown = 1,

  featureDefs       = {

    DEAD  = {
      description      = "Wreckage - Wind Generator",
      blocking         = true,
      featureDead      = "DEAD2",
      object           = "windgen.s3o",
    },

    DEAD2  = {
      description      = "Wreckage - Wind Generator",
      blocking         = true,
      object           = "windgen.s3o",
    },

  },

}

return lowerkeys({ windgen = unitDef })
