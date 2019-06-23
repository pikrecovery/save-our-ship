
using System;
using System.Collections.Generic;
using RimWorld.Planet;
using ShipsHaveInsides.MapComponents;
using ShipsHaveInsides.Mod;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class AirShipWorldObject : MapParent, ILoadReferenceable
    {
        private Material cachedMat;
        private static readonly Color PlayerCaravanColor = new Color(1f, 0.863f, 0.33f);
        public Caravan_GotoMoteRenderer gotoMote = new Caravan_GotoMoteRenderer();
        private int? targetTile = null;
        private Vector3? curPos = null;

        private const float drawHeight = 50f;
        private static readonly Material WorldLineMatWhite = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, Color.white, WorldMaterials.WorldLineRenderQueue);

        private Guid? shipGuid = null;

        private ShipDefinition ShipDefinitionObject = null;
        public ShipDefinition ShipDefinition { get => ShipDefinitionObject; set => ShipDefinitionObject = value; }
        public override void ExposeData()
        {
            base.ExposeData();

            //TODO: pretty sure scribe uses a key value database, generify this
            Scribe_Values.Look(ref curPos, "curPos");

            string guid = null;

            Scribe_Values.Look(ref guid, "shipGuid");//TODO: for multiple ships i think we will need to use an incremental identifier

            if(guid != null)
            {
                //Guid val = Guid.
                shipGuid = new Guid(guid);
            }

            /*Scribe_Deep.Look(ref ShipDefinitionObject, "shipDefinitionObject");

           if(ShipDefinitionObject != null)
            {
                ShipDefinition = ShipDefinitionObject;
            }*/
        }

        public override void FinalizeLoading()
        {
            base.FinalizeLoading();
            if (shipGuid == null)
            {
                ShipDefinition[] shipDefinitions = Map.GetSpaceAtmosphereMapComponent().GetShipDefinitions().ToArray();

                ShipDefinition def = shipDefinitions[0];

                if (def.shipIdentifier == null)
                {
                    def.computeGUID();
                }

                shipGuid = def.shipIdentifier;

                ShipDefinition searchDef = Map.GetSpaceAtmosphereMapComponent().getDefinitionByIdentifier((Guid)shipGuid);

                if (searchDef != null)
                {
                    ShipDefinition = searchDef;
                }
            } else
            {
                ShipDefinition searchDef = Map.GetSpaceAtmosphereMapComponent().getDefinitionByIdentifier((Guid)shipGuid);

                if (searchDef != null)
                {
                    ShipDefinition = searchDef;
                }
            }
        }


        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                {
                    Color color = Faction != null ? (!Faction.IsPlayer ? Faction.Color : PlayerCaravanColor) : Color.white;
                    cachedMat = MaterialPool.MatFrom(def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.DynamicObjectRenderQueue);
                }
                return cachedMat;
            }
        }

        public override Vector3 DrawPos {
            get
            {
                if(!curPos.HasValue)
                {
                    Vector3 tileCenter = Find.WorldGrid.GetTileCenter(Tile);
                    curPos = tileCenter + (tileCenter.normalized * drawHeight);
                }

                return curPos.Value;
            }
        }

        private void DrawVertWorldLineBetween(Vector3 A, Vector3 B, Material material, float widthFactor = 1f)
        {
            if (!(Mathf.Abs(A.x - B.x) < 0.005f) || !(Mathf.Abs(A.y - B.y) < 0.005f) || !(Mathf.Abs(A.z - B.z) < 0.005f))
            {
                Vector3 pos = (A + B) / 2f;
                float magnitude = (A - B).magnitude;
                Quaternion q = Quaternion.LookRotation(A - B, Find.WorldCamera.transform.position - pos);
                Vector3 s = new Vector3(0.2f * Find.WorldGrid.averageTileSize * widthFactor, 1f, magnitude);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(pos, q, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, WorldCameraManager.WorldLayer);
            }
        }


        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            float d = 0.05f;
            if (targetTile.HasValue)
            {
                Vector3 tileCenter = DrawPos - (DrawPos.normalized * (drawHeight - d));
                Vector3 tileCenter2 = Find.WorldGrid.GetTileCenter(targetTile.Value);
                Vector3 finalPos = tileCenter2 + (tileCenter2.normalized * drawHeight);
                tileCenter2 += tileCenter2.normalized * d;

                DrawVertWorldLineBetween(DrawPos, tileCenter, WorldLineMatWhite, 2f);

                Vector3 curVec = tileCenter;
                for (float i=0f;i<=1f;i+=0.01f)
                {
                    Vector3 newVec = Vector3.Slerp(tileCenter, tileCenter2, i);
                    GenDraw.DrawWorldLineBetween(curVec, newVec, WorldLineMatWhite, 4f);
                    curVec = newVec;
                }
                GenDraw.DrawWorldLineBetween(curVec, tileCenter2);
                DrawVertWorldLineBetween(tileCenter2, finalPos, WorldLineMatWhite, 2f);
            }

            gotoMote.RenderMote();
        }

        internal void ClickedNewTile(int tileClicked)
        {
            //gotoMote.OrderedToTile(tileClicked);//TODO: this properly

            targetTile = tileClicked;
            ShipDefinition.Tile = targetTile;

            Vector3 tileCenter = Find.WorldGrid.GetTileCenter(targetTile.Value);

            curPos = tileCenter + (tileCenter.normalized * drawHeight);
        }

        public override string Label => ShipDefinition?.Name ?? "Untitled ship";
    }
}
