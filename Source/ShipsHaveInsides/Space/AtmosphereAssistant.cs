using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShipsHaveInsides.Space
{
    class AtmosphereAssistant
    {
        public static bool IsMixtureToxic(GasMixture gas)
        {

            if(IsMixtureCo2Toxic(gas))//conditions for hypercapnia
            {
                return true;
            }

            if (!IsMixtureOxygenated(gas))
            {
                return true;
            }

            return false;
        }

        public static bool IsMixtureCo2Toxic(GasMixture gas)
        {
            if (gas.Co2Partial >= 5f)//conditions for hypercapnia
            {
                return true;
            }

            return true;
        }

        public static bool IsMixtureOxygenated(GasMixture gas)
        {
            if (gas.O2Partial >= 6f)
            {
                return true;
            }

            return false;
        }

        public static bool IsMixtureOxygenated(GasMixture gas, float partial)
        {
            if (gas.O2Partial >= partial)
            {
                return true;
            }

            return false;
        }

        public static bool MixtureHasInertGas(GasMixture gas)
        {
            return gas.InertComponentsPartial > 0;
        }

        public static bool IsOxygenRich(GasMixture gas)
        {
            return gas.O2Partial >= SpaceConstants.EarthNorm.O2Partial;
        }

    }
}
