using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
    partial class PlanetStructure
    {
        public void PoweredTick(int turn) {
            if (IsActive) return;
            if (!IsActive) {
                if (ActivatedOnTurn == null) ActivatedOnTurn = turn;
                if (StructureType.TurnsToActivate == null) IsActive = true;
                else if (ActivatedOnTurn != null && ActivatedOnTurn.Value + StructureType.TurnsToActivate.Value <= turn) IsActive = true;

            }

        }


        public void UnpoweredTick(int turn) {
            if ((StructureType.UpkeepEnergy ?? 0) > 0) {
                IsActive = false;
                ActivatedOnTurn = null;
            }

        }


    }
}
