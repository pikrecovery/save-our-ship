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
        private static readonly Material PowerPlantSolarBarFilledMatIonizingGasPressure = SolidColorMaterials.SimpleSolidColorMaterial(Color.red, false);//1

        private static readonly float HalfEarthPressure = GasMixture.EarthNorm.totalPressure * 0.5f;

        private GasMixture gas = GasMixture.Vacuum;

        private float MaxPressure => (props as CompProperties_AirTank).maxPressure;

        private int BarType = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref gas, "gas");
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

            if(BarType == 1)
            {
                r.filledMat = PowerPlantSolarBarFilledMatDispersing;
            }
            else if(BarType ==2) {
                r.filledMat = PowerPlantSolarBarFilledMatIonizingGasPressure;
                if (r.fillPercent == .99f)
                {
                    r.fillPercent = .49f;
                } else if(r.fillPercent == .49f)
                {
                    r.fillPercent = 0f;
                } else
                {
                    r.fillPercent = .99f;
                }
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

            var def = currentMap.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);
            var calc = new ShipDefinition.GasCalculator(def);
            var rg = parent.GetRoomGroup();
            var roomGas = def.GetGas(rg);

            bool canReleaseGas = gas.totalPressure >= (MaxPressure * 0.10f);

            if (currentMap.IsSpace())
            {
                bool ContainingRoomHasLifeSupport = AtmosphereAssistant.IsLivingViable(roomGas.mixture);

                bool TankEmpty = gas.totalPressure <= 0;

                bool ShouldDisperseInLifeSupport = !AtmosphereAssistant.IsOxygenRich(roomGas.mixture);

                if (ContainingRoomHasLifeSupport)
                {

                    if(roomGas.mixture.O2Partial >= 8f)//don't deplete it to the point you harm your pawns
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
                            gas += new GasMixture(10000, 0, SpaceConstants.EarthNorm.Co2Partial);
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
        }
    }
}
