unitDef = {
  unitname            = "armpw",
  name                = "Glaive",
  description         = "Raider Bot",
  acceleration        = 0.36,
  bmcode              = "1",
  brakeRate           = 0.2,
  buildCostEnergy     = 65,
  buildCostMetal      = 65,
  builder             = false,
  buildPic            = "armpw.png",
  buildTime           = 65,
  canAttack           = true,
  canGuard            = true,
  canMove             = true,
  canPatrol           = true,
  canstop             = "1",
  category            = "LAND",
  corpse              = "DEAD",
  defaultmissiontype  = "Standby",
  explodeAs           = "SMALL_UNITEX",
  footprintX          = 2,
  footprintZ          = 2,
  iconType            = "kbotraider",
  idleAutoHeal        = 20,
  idleTime            = 300,
  leaveTracks         = true,
  maneuverleashlength = "640",
  mass                = 32.5,
  maxDamage           = 190,
  maxSlope            = 36,
  maxVelocity         = 3.8,
  maxWaterDepth       = 22,
  minCloakDistance    = 75,
  movementClass       = "kbot2",
  noAutoFire          = false,
  noChaseCategory     = "FIXEDWING SATELLITE SUB",
  objectName          = "spherebot.s3o",
  seismicSignature    = 4,
  selfDestructAs      = "SMALL_UNITEX",

  sfxtypes            = {

    explosiongenerators = {
      "custom:emg_shells_l",
      "custom:flashmuzzle1",
    },

  },

  side                = "ARM",
  sightDistance       = 425,
  smoothAnim          = true,
  steeringmode        = "2",
  TEDClass            = "KBOT",
  trackOffset         = 0,
  trackStrength       = 8,
  trackStretch        = 1,
  trackType           = "ComTrack",
  trackWidth          = 18,
  turnRate            = 1250,
  upright             = true,
  workerTime          = 0,

  weapons             = {

    {
      def                = "EMG",
      badTargetCategory  = "FIXEDWING",
      onlyTargetCategory = "FIXEDWING LAND SINK SHIP SWIM FLOAT GUNSHIP HOVER",
    },

  },


  weaponDefs          = {

    EMG = {
      name                    = "peewee",
      alphaDecay              = 0.1,
      areaOfEffect            = 8,
      burst                   = 3,
      burstrate               = 0.1,
      colormap                = "1 0.95 0.4 1   1 0.95 0.4 1    0 0 0 0.01    1 0.7 0.2 1",
      craterBoost             = 1,
      craterMult              = 1,

      damage                  = {
        default = 11,
        planes  = 11,
        subs    = 0.55,
      },

      endsmoke                = "0",
      explosionGenerator      = "custom:FLASHPLOSION",
      impactOnly              = true,
      impulseBoost            = 0,
      impulseFactor           = 0.4,
      intensity               = 0.7,
      interceptedByShieldType = 1,
      lineOfSight             = true,
      noGap                   = false,
      noSelfDamage            = true,
      range                   = 185,
      reloadtime              = 0.31,
      renderType              = 4,
      rgbColor                = "1 0.95 0.4",
      separation              = 1.5,
      size                    = 1.75,
      sizeDecay               = 0,
      soundStart              = "flashemg",
      sprayAngle              = 1180,
      stages                  = 10,
      startsmoke              = "0",
      tolerance               = 5000,
      turret                  = true,
      weaponTimer             = 0.1,
      weaponType              = "Cannon",
      weaponVelocity          = 500,
    },

  },


  featureDefs         = {

    DEAD  = {
      description      = "Wreckage - Glaive",
      blocking         = false,
      category         = "corpses",
      damage           = 380,
      energy           = 0,
      footprintX       = 2,
      footprintZ       = 2,
      height           = "40",
      hitdensity       = "100",
      metal            = 19.5,
      object           = "spherebot_dead.s3o",
      reclaimable      = true,
      reclaimTime      = 78,
    },

  },

}

return lowerkeys({ armpw = unitDef })
