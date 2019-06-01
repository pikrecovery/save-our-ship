using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShipsHaveInsides.Space
{
    class SpaceConstants
    {
        public readonly static GasMixture EarthNorm = new GasMixture(81.02f, 20.265f, 0.04f);
        public readonly static GasMixture SpaceSuitNorm = new GasMixture(0.0f, 20.265f, 0.0f);
        public readonly static GasMixture ShipNorm = new GasMixture(0.0f, 20.265f, 0.0f);
        public readonly static GasMixture Vacuum = new GasMixture(0.0f, 0.0f, 0.0f);
    }
}
