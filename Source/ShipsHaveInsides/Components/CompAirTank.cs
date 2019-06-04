using ShipsHaveInsides.MapComponents;
using ShipsHaveInsides.Mod;
using ShipsHaveInsides.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompAirTank : ThingComp
    {
        private static readonly Vector2 BarSize = new Vector2(0.8f, 0.07f);
        private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f), false);//0
        private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f), false);

        private static readonly Material PowerPlantSolarBarFilledMatDispersing = SolidColorMaterials.SimpleSolidColorMaterial(Color.magenta, false);//1
        private static readonly Material PowerPlantSolarBarFilledMatIonizingGasPressure = SolidColorMaterials.SimpleSolidColorMaterial(Color.red, false);//2
        private static readonly Material PowerPlantSolarBarFilledMatCo2Hazard = SolidColorMaterials.SimpleSolidColorMaterial(Color.blue, false);//2

        private static readonly float HalfEarthPressure = GasMixture.EarthNorm.totalPressure * 0.5f;

        private GasMixture gas = GasMixture.Vacuum;

        private float MaxPressure => (props as CompProperties_AirTank).maxPressure;

        private int BarType = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref gas, "gas");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action launch = new Command_Action
            {
                action = TryRepressurize,
                defaultLabel = "ShipInsideRePressurize".Translate(),
                defaultDesc = "ShipInsideRePressurizeDesc".Translate()
            };

            launch.hotKey = KeyBindingDefOf.Misc2;
            launch.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            List<Gizmo> gizmos = base.CompGetGizmosExtra().ToList();

            gizmos.Add(launch);

            return gizmos;
        }

        private void TryRepressurize()
        {
            
            Map currentMap = parent.Map;

            var def = currentMap.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);
            var calc = new ShipDefinition.GasCalculator(def);
            var rg = parent.GetRoomGroup();
            var roomGas = def.GetGas(rg);

            gas += new GasMixture(0, 1000, 1000);

            var pressureDif = GasMixture.EarthNorm.totalPressure - roomGas.mixture.totalPressure;
            if (pressureDif > 0.0f)
            {
                //release enough gas (or whatever's left) to get the gas up to 101.325 kPa.
                var pressureNeeded = pressureDif * rg.CellCount;

                if (pressureNeeded > gas.totalPressure)
                {
                    calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, gas.totalPressure / (float)rg.CellCount), rg.CellCount));
                }
                else
                {
                    calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, pressureDif), rg.CellCount));
                }
                gas -= pressureNeeded;

                calc.Execute();

                if (gas.totalPressure < 0)
                {
                    gas += GasMixture.atPressure(SpaceConstants.EarthNorm, 0);
                }
            }
        }


        public override string CompInspectStringExtra()
        {
            return new StringBuilder(base.CompInspectStringExtra())
                .Append("Total Pressure: ")
                .Append(gas.totalPressure.ToString("0.00"))
                .AppendLine(" kPa")
                .Append("Max Pressure: ")
                .Append(MaxPressure.ToString("0.00"))
                .Append(" kPa")
                .ToString();
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest();
            r.center = this.parent.DrawPos + Vector3.up * 0.1f;
            r.size = BarSize;
            r.fillPercent = gas.totalPressure / MaxPressure;
            //if()

            if (BarType == 1)
            {
                r.filledMat = PowerPlantSolarBarFilledMatDispersing;
            }
            else if (BarType == 2) {
                r.filledMat = PowerPlantSolarBarFilledMatIonizingGasPressure;
                r.fillPercent = gas.totalPressure / (MaxPressure * 0.005f);
            } else if(BarType == 3) {
                r.filledMat = PowerPlantSolarBarFilledMatCo2Hazard;
            } else
            {
                r.filledMat = PowerPlantSolarBarFilledMat;
            }

            r.unfilledMat = PowerPlantSolarBarUnfilledMat;
            r.margin = 0.15f;
            Rot4 rotation = this.parent.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (Find.TickManager.TicksAbs % 50 != 0)
            {
                return;
            }

            Map currentMap = parent.Map;

            if (currentMap.IsSpace())
            {
                var def = currentMap.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);
                var calc = new ShipDefinition.GasCalculator(def);
                var rg = parent.GetRoomGroup();
                var roomGas = def.GetGas(rg);

                bool roomHasLifeSupport = !AtmosphereAssistant.IsMixtureToxic(roomGas.mixture);

                var pressureDif = GasMixture.EarthNorm.totalPressure - roomGas.mixture.totalPressure;

                bool roomAtPressure = pressureDif <= 0.0f;

                bool TankEmpty = gas.totalPressure <= 0;
                bool TankFull = gas.totalPressure >= MaxPressure;
                bool TankHasOxygen = gas.O2Partial > 0;
                bool TankHasCo2 = gas.Co2Partial > 0;

                if (roomAtPressure)
                {
                    bool recalc = false;


                    if (AtmosphereAssistant.IsMixtureCo2Toxic(roomGas.mixture))
                    {
                        if (!TankFull)
                        {
                            gas += new GasMixture(0, 0, 1);
                            calc.GasExchange(rg, removed: new GasVolume(new GasMixture(0, 0, 1), rg.CellCount));
                            recalc = true;
                            BarType = 2;
                        }
                    }
                    else
                    {
                        if (BarType == 2)
                            BarType = 0;

                        if(TankHasCo2 && roomGas.mixture.Co2Partial < 1) //dump some co2 in for plants etc
                        {
                            gas -= new GasMixture(0, 0, 0.5f);
                            calc.GasExchange(rg, added: new GasVolume(new GasMixture(0, 0, 0.5f), rg.CellCount));
                            recalc = true;
                            BarType = 2;
                        }
                    }

                    if (AtmosphereAssistant.MixtureHasInertGas(roomGas.mixture))
                    {
                        if (!TankFull)
                        {
                            gas += new GasMixture(1, 0, 0);
                            calc.GasExchange(rg, removed: new GasVolume(new GasMixture(1, 0, 0), rg.CellCount));
                            recalc = true;
                        }
                    }

                    if (recalc)
                    {
                        calc.Execute();
                        pressureDif = GasMixture.EarthNorm.totalPressure - roomGas.mixture.totalPressure;
                        roomAtPressure = pressureDif <= 0.0f;
                    }

                    if (TankHasOxygen)
                    {
                        if (!roomAtPressure && !AtmosphereAssistant.IsOxygenRich(roomGas.mixture))
                        {
                            gas -= new GasMixture(0, 1, 0);
                            calc.GasExchange(rg, added: new GasVolume(new GasMixture(0, 1, 0), rg.CellCount));
                            recalc = true;

                            if (BarType == 0)
                            {
                                BarType = 1;
                            }
                        }
                        else
                        {
                            if (BarType == 1)
                            {
                                BarType = 0;
                            }
                        }
                    }

                    if (recalc)
                        calc.Execute();
                }
                else
                {
                    bool canReleaseGas = gas.totalPressure >= (MaxPressure * 0.005f);

                    if (AtmosphereAssistant.IsMixtureOxygenated(roomGas.mixture, 10f))
                    {
                        if (!TankFull)
                        {
                            float drainRate = 1f;

                            if (roomGas.mixture.O2Partial > 30)
                            {
                                drainRate = 2f;
                            }

                            gas += SpaceConstants.ShipNorm * (2f * drainRate);
                            // gas += new GasMixture(90, 0, SpaceConstants.EarthNorm.Co2Partial);
                            calc.GasExchange(rg, removed: new GasVolume(new GasMixture(0, drainRate, 0), rg.CellCount));
                        }
                    }


                    if (canReleaseGas)
                    {
                        var pressureNeeded = pressureDif * rg.CellCount;

                        if (pressureNeeded > gas.totalPressure)
                        {
                            calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, gas.totalPressure / (float)rg.CellCount), rg.CellCount));
                        }
                        else
                        {
                            calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, pressureDif), rg.CellCount));
                        }
                        gas -= pressureNeeded;

                        BarType = 1;

                        calc.Execute();

                        if (gas.totalPressure < 0)
                        {
                            gas += GasMixture.atPressure(SpaceConstants.EarthNorm, 0);
                        }
                    }
                    else
                    {
                        if (BarType != 0)
                            BarType = 0;

                        if (gas.totalPressure <= (MaxPressure * 0.005f) && roomGas.mixture.totalPressure < HalfEarthPressure)
                        {
                            gas += new GasMixture(100, 0, SpaceConstants.EarthNorm.Co2Partial);
                            BarType = 2;
                        }
                        else
                        {
                            if (BarType == 2)
                                BarType = 0;
                        }

                    }

                    if (AtmosphereAssistant.IsMixtureCo2Toxic(roomGas.mixture))
                    {
                        if (!TankFull)
                        {
                            gas += new GasMixture(0, 0, 1);
                            calc.GasExchange(rg, removed: new GasVolume(new GasMixture(0, 0, 1), rg.CellCount));
                            calc.Execute();
                            BarType = 3;
                        }
                    }
                    else
                    {
                        if (BarType == 3)
                            BarType = 0;
                    }
                }
            } else
            {
                if (gas.totalPressure >= MaxPressure)
                    return;
                //don't steal gas from the ship while in atmo. Deal with this later.
                //calc.GasExchange(rg, removed: new GasVolume(roomGas.mixture, 2f));
                //calc.Execute();

                gas += GasMixture.EarthNorm * 2f;

                if (gas.totalPressure > MaxPressure)
                    gas = GasMixture.atPressure(gas, MaxPressure);
            }
        }






            //determine room status


            /*
            base.CompTick();

            if (Find.TickManager.TicksAbs % 50 != 0)
            {
                return;
            }

            Map currentMap = parent.Map;

            var def = currentMap.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);
            var calc = new ShipDefinition.GasCalculator(def);
            var rg = parent.GetRoomGroup();
            var roomGas = def.GetGas(rg);

            bool canReleaseGas = gas.totalPressure >= (MaxPressure * 0.10f);

            if (currentMap.IsSpace())
            {
                bool ContainingRoomHasLifeSupport = AtmosphereAssistant.IsLivingViable(roomGas.mixture);

                bool TankEmpty = gas.totalPressure <= 0;
                bool TankFull = gas.totalPressure >= MaxPressure;
                bool TankHasOxygen = gas.O2Partial > 0;

                bool ShouldDisperseInLifeSupport = !AtmosphereAssistant.IsOxygenRich(roomGas.mixture);

                if (roomGas.mixture.totalPressure > HalfEarthPressure)
                {
                    if(!TankFull)//recycle inert gas into tanks
                    {
                        if(roomGas.mixture.InertComponentsPartial > 0)
                        {
                            gas += new GasMixture(1, 0, 1);
                            calc.GasExchange(rg, removed: new GasVolume(new GasMixture(1, 0, 0), rg.CellCount));
                        }
                    }

                }

                if (ContainingRoomHasLifeSupport)
                {

                    if(roomGas.mixture.O2Partial >= 10f)//don't deplete it to the point you harm your pawns
                    {
                        if (gas.totalPressure <= MaxPressure)
                        {
                            float drainRate = 1f;

                            if(roomGas.mixture.O2Partial > 30)
                            {
                                drainRate = 2f;
                            }

                            gas += SpaceConstants.ShipNorm * (2f * drainRate);
                           // gas += new GasMixture(90, 0, SpaceConstants.EarthNorm.Co2Partial);
                            calc.GasExchange(rg, removed: new GasVolume(new GasMixture(0, drainRate, 0), rg.CellCount));
                        }
                    }

                    if (ShouldDisperseInLifeSupport)
                    {
                        if (!TankEmpty && canReleaseGas)
                        {
                            //TODO condense into one method
                            var pressureDif = GasMixture.EarthNorm.totalPressure - roomGas.mixture.totalPressure;
                            if (pressureDif > 0.0f)
                            {
                                //release enough gas (or whatever's left) to get the gas up to 101.325 kPa.
                                var pressureNeeded = pressureDif * rg.CellCount;

                                if (pressureNeeded > gas.totalPressure)
                                {
                                    calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, gas.totalPressure / (float)rg.CellCount), rg.CellCount));
                                }
                                else
                                {
                                    calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, pressureDif), rg.CellCount));
                                }
                                gas -= pressureNeeded;

                                BarType = 1;

                                calc.Execute();

                                if(gas.totalPressure < 0)
                                {
                                    gas += GasMixture.atPressure(SpaceConstants.EarthNorm, 0);
                                }
                            } else
                            {
                                if(BarType == 1)
                                    BarType = 0;
                            }
                        } else
                        {
                            if (BarType == 1)
                                BarType = 0;
                        }
                    } else
                    {
                        if (BarType == 1)
                            BarType = 0;
                    }



                    if (roomGas.mixture.totalPressure < HalfEarthPressure && !(roomGas.mixture.totalPressure <= 0))
                    {
                        if (gas.totalPressure <= (MaxPressure * 0.3))
                        {
                            gas += new GasMixture(1000, 0, SpaceConstants.EarthNorm.Co2Partial);
                            BarType = 2;
                        } else
                        {
                            if (BarType == 2)
                                BarType = 0;
                        }
                    } else
                    {
                        if (BarType == 2)
                            BarType = 0;
                    }

                    calc.Execute();
                }
                else
                {
                    if (!TankEmpty)
                    {
                        //TODO condense into one method
                        var pressureDif = GasMixture.EarthNorm.totalPressure - roomGas.mixture.totalPressure;
                        if (pressureDif > 0.0f)
                        {
                            //release enough gas (or whatever's left) to get the gas up to 101.325 kPa.
                            var pressureNeeded = pressureDif * rg.CellCount;

                            if (pressureNeeded > gas.totalPressure)
                            {
                                calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, gas.totalPressure / (float)rg.CellCount), rg.CellCount));
                            }
                            else
                            {
                                calc.GasExchange(rg, added: new GasVolume(GasMixture.atPressure(gas, pressureDif), rg.CellCount));
                            }
                            gas -= pressureNeeded;

                            BarType = 1;

                            calc.Execute();

                            if (gas.totalPressure < 0)
                            {
                                gas += GasMixture.atPressure(SpaceConstants.EarthNorm, 0);
                            }
                        } else
                        {
                            if (BarType == 1)
                                BarType = 0;
                        }
                    } else if (TankEmpty)
                    {
                        if (BarType == 1)
                            BarType = 0;
                    }
                }
            }
            else
            {
                if (gas.totalPressure >= MaxPressure)
                    return;
                //don't steal gas from the ship while in atmo. Deal with this later.
                //calc.GasExchange(rg, removed: new GasVolume(roomGas.mixture, 2f));
                //calc.Execute();

                gas += GasMixture.EarthNorm * 2f;

                if (gas.totalPressure > MaxPressure)
                    gas = GasMixture.atPressure(gas, MaxPressure);
            }
        }*/
        }
}
