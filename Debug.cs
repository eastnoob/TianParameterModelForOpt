using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    internal static class Debug
    {

        public static Brep[] CreateAllFloors(Dictionary<Curve, Curve> curveWithOffsetedResults, Dictionary<Curve, string> offsetBehavioursOfLandcurves, Curve landCurve, Dictionary<Curve, Curve> directionWithCurve)
        {

            List<Brep> copies = new List<Brep>();

            Dictionary<Curve, Brep> curveWithBrep = new Dictionary<Curve, Brep>();

            double buildingDepth = 4;

            double floorHight = 5;

            // 此时curveWithOffsetedResults每个只有一条线
            foreach (KeyValuePair<Curve, Curve> pair in curveWithOffsetedResults)
            {
                // 如果是end线，那么生成的体块深度应该是building spacing，仅用来裁剪
                if (offsetBehavioursOfLandcurves[pair.Key] == "end")
                {
                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, pair.Value.GetLength(), 2, floorHight);
                    curveWithBrep[pair.Key] = curveBrep;
                }

                // 如果是end-boundage线，那么直接不生成
                else if (offsetBehavioursOfLandcurves[pair.Key] == "end-boundage")
                {
                    curveWithBrep[pair.Key] = null;
                }

                else
                {
                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, pair.Value.GetLength(), buildingDepth, floorHight);
                    curveWithBrep[pair.Key] = curveBrep;
                }

            }

            List<Brep> sideBreps = new List<Brep>();
            List<Brep> endBreps = new List<Brep>();

            // edge 就加， end就减
            foreach (Curve curve in curveWithBrep.Keys)
            {
                if (!offsetBehavioursOfLandcurves[curve].Contains("end"))
                {
                    sideBreps.Add(curveWithBrep[curve]);
                }
                else
                {
                    endBreps.Add(curveWithBrep[curve]);
                }
            }
            Brep[] solidBreps = Brep.CreateBooleanUnion(sideBreps, 0.001);
            Brep[] finalBrep = Brep.CreateBooleanDifference(solidBreps, endBreps, 0.001);

            return solidBreps;

        }
    }

}
