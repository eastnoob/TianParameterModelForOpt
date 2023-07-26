//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TianParameterModelForOpt._4_repair;

//namespace TianParameterModelForOpt
//{
//    internal static class DebugMethods
//    {
//        public static Brep[] CreateAllFloors(Dictionary<Curve, Curve> curveWithOffsetedResults, Dictionary<Curve, string> offsetBehavioursOfLandcurves, Curve landCurve)
//        {

//            List<Brep> copies = new List<Brep>();

//            Dictionary<Curve, Brep> curveWithBrep = new Dictionary<Curve, Brep>();

//            double buildingDepth = 4;

//            double floorHight = 5;

//            // 此时curveWithOffsetedResults每个只有一条线
//            foreach (KeyValuePair<Curve, Curve> pair in curveWithOffsetedResults)
//            {
//                // 如果是end线，那么生成的体块深度应该是building spacing，仅用来裁剪
//                if (offsetBehavioursOfLandcurves[pair.Key] == "end")
//                {
//                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, pair.Value.GetLength() + 10, 2, floorHight);
//                    curveWithBrep[pair.Key] = curveBrep;
//                }

//                // 如果是end-boundage线，那么直接不生成
//                else if (offsetBehavioursOfLandcurves[pair.Key] == "end-boundage")
//                {
//                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, pair.Value.GetLength() + 10, -2, floorHight);
//                    curveWithBrep[pair.Key] = curveBrep;
//                }

//                else
//                {
//                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, pair.Value.GetLength(), buildingDepth, floorHight);
//                    curveWithBrep[pair.Key] = curveBrep;
//                }

//            }

//            List<Brep> sideBreps = new List<Brep>();
//            List<Brep> endBreps = new List<Brep>();


//            // edge 就加， end就减
//            foreach (Curve curve in curveWithBrep.Keys)
//            {
//                if (!offsetBehavioursOfLandcurves[curve].Contains("end"))
//                {
//                    sideBreps.Add(curveWithBrep[curve]);
//                }
//                else
//                {
//                    endBreps.Add(curveWithBrep[curve]);
//                }
//            }

//            Brep[] finalBrep;

//            Brep[] solidBreps = Brep.CreateBooleanUnion(sideBreps, 0.01);

//            foreach (Brep brep in solidBreps)
//            {
//                if (brep.IsSolid == false)
//                    brep.CapPlanarHoles(0.01);
//            }


//            if (endBreps.Count() > 0)
//            {
//                foreach (Brep brep in endBreps)
//                {
//                    if (brep.IsSolid == false)
//                        brep.CapPlanarHoles(0.01);
//                }

//                finalBrep = Brep.CreateBooleanDifference(solidBreps, endBreps, 0.01);
//            }

//            else
//            {
//                finalBrep = solidBreps;
//            }
                

//            foreach(Brep brep in finalBrep)
//            {
//                if(brep.IsSolid == false)
//                    brep.CapPlanarHoles(0.01);
//            }

//            if (finalBrep.Length > 1)
//                finalBrep = new Brep[] { Find.FindTheLargestBrep(finalBrep) };

//            //return new Brep[] { Find.FindTheLargestBrep(finalBrep) };
//            else { }

//            // 修复形态
//            brepOfTheBuilding = Brep.CreateBooleanIntersection(judgeBrepArray, singleBlockBreps, 0.001);

//            return finalBrep;
//            return endBreps.ToArray();

//        }
//    }
//}
