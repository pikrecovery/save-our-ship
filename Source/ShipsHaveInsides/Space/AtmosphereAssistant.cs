using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShipsHaveInsides.Space
{
    class AtmosphereAssistant
    {
        public static bool IsLivingViable(GasMixture gas)
        {

            if(gas.Co2Partial >= 5f)//conditions for hypercapnia
            {
                return false;
            }

            if (gas.O2Partial <= 6f)
            {
                return false;
            }

            return true;
        }

        public static bool IsOxygenRich(GasMixture gas)
        {
            return gas.O2Partial >= SpaceConstants.EarthNorm.O2Partial;
        }

    }
}
