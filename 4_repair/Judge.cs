﻿using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace TianParameterModelForOpt._4_repair
{
    internal class Judge
    {
        Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve;
        Land thisLand;
        Brep[] judgeBrep;

        public Judge(Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve, Land thisLand)
        {
            this.allEdgeWithOffsetBaseCurve = allEdgeWithOffsetBaseCurve;
            this.thisLand = thisLand;
            this.judgeBrep = CreateJudgeBrep();

        }

        /// <summary>
        /// 重载，使用单个Brep作为输出
        /// </summary>
        /// <param name="unIntersectedBUildingBrep"></param>
        /// <returns></returns>
        public Brep RepairFloorBlock(Brep unIntersectedBUildingBrep)
        {
            //// 尝试使用NodeInCode的方法
            //var intersectBrep = Rhino.NodeInCode.Components.FindComponent("SolidIntersection");
            //if (intersectBrep == null)
            //{
            //    Console.WriteLine("Cannot find SolidIntersection");
            //    return null;
            //}
            //Brep[] output = null;

            //var intersectBrep_function = intersectBrep.Delegate as dynamic;

            //var repairedBrepTest = intersectBrep_function(this.judgeBrep[0], unIntersectedBUildingBrep)[0];

            ////var intersectBrep_function = (System.Func<object, object, object>)intersectBrep.Delegate;

            //return repairedBrepTest;

            // repair
            Brep[] repairedBrep = Brep.CreateBooleanIntersection(this.judgeBrep[0], unIntersectedBUildingBrep, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            if (judgeBrep != null && unIntersectedBUildingBrep != null)
            {
                if (repairedBrep == null)
                {
                    System.Threading.Thread.Sleep(500); // 5000毫秒 = 5秒
                    repairedBrep = Brep.CreateBooleanIntersection(this.judgeBrep[0], unIntersectedBUildingBrep, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                }
            }

            // 尝试处理None的问题

            //Console.WriteLine("judgedSingleBlockBrep.Length: ");

            if (repairedBrep == null/* || judgedSingleBlockBrep.Length == 0 */)
            {
                
                Console.WriteLine("Need repair ! !");
                System.Threading.Thread.Sleep(500); // 5000毫秒 = 5秒

                
                Brep[] judgedSingleBlockBreps = new Brep[] { unIntersectedBUildingBrep };


                if (unIntersectedBUildingBrep == null)
                {
                    Console.WriteLine("Cannot be completed!");
                    Trace.WriteLine("Cannot be completed!");
                    repairedBrep = null;
                }


                else
                {
                    // 换用Split方法
                    Brep[] judged = unIntersectedBUildingBrep.Split(judgeBrep, 0.01);

                    var largestOne = Find.FindTheLargestBrep(judged);
                    if (largestOne.IsSolid == false)
                        largestOne = largestOne.CapPlanarHoles(0.01);

                    repairedBrep = new Brep[] { largestOne };

                    Console.WriteLine("Repair Complete ! !");
                    Trace.WriteLine("Repair Complete ! !");
                }

                if(repairedBrep != null && repairedBrep.Length != 0)
                    return repairedBrep[0];
                else
                    return null;
            }

            else if(repairedBrep.Length > 1)
            {
                Brep needToReturn = Find.FindTheLargestBrep(repairedBrep);
                return needToReturn;
            }

            else if(repairedBrep.Length == 0)
            {
                Console.WriteLine("Need repair ! !");
                System.Threading.Thread.Sleep(500); // 5000毫秒 = 5秒


                Brep[] judgedSingleBlockBreps = new Brep[] { unIntersectedBUildingBrep };


                if (unIntersectedBUildingBrep == null)
                {
                    Console.WriteLine("Cannot be completed!");
                    Trace.WriteLine("Cannot be completed!");
                    repairedBrep = null;
                }


                else
                {
                    // 换用Split方法
                    Brep[] judged = unIntersectedBUildingBrep.Split(judgeBrep, 0.01);

                    var largestOne = Find.FindTheLargestBrep(judged);

                    if (largestOne.IsSolid == false)
                        largestOne = largestOne.CapPlanarHoles(0.01);

                    repairedBrep = new Brep[] { largestOne };

                    Console.WriteLine("Repair Complete ! !");
                    Trace.WriteLine("Repair Complete ! !");
                }

                if (repairedBrep != null && repairedBrep.Length != 0)
                    return repairedBrep[0];
                else
                    return null;
            }
            
            else 
                return repairedBrep[0];
        }


        public Brep[] RepairFloorBlock (Brep[] unIntersectedBUildingBrep)
        {

            // repair
            Brep[] repairedBrep = Brep.CreateBooleanIntersection(this.judgeBrep, unIntersectedBUildingBrep, 0.001);

            if(judgeBrep != null && unIntersectedBUildingBrep != null)
            {
                if(repairedBrep == null)
                {
                    System.Threading.Thread.Sleep(500); // 5000毫秒 = 5秒
                    repairedBrep = Brep.CreateBooleanIntersection(this.judgeBrep, unIntersectedBUildingBrep, 0.001);
                }
            }

            // 尝试处理None的问题

            //Console.WriteLine("judgedSingleBlockBrep.Length: ");

            if (repairedBrep == null/* || judgedSingleBlockBrep.Length == 0 */)
            {
                Console.WriteLine("Need repair ! !");
                Brep maxvolumebrep = null;

                if (unIntersectedBUildingBrep.Length > 1)
                {
                    // 取出最大的那一个
                    Brep[] unionSecond = Brep.CreateBooleanUnion(unIntersectedBUildingBrep, 0.001);

                    if (unionSecond.Length > 1)
                    {
                        //Brep maxvolumebrep = null;
                        double maxvolume = 0.0;

                        foreach (Brep brep in unIntersectedBUildingBrep)
                        {
                            VolumeMassProperties vmp = VolumeMassProperties.Compute(brep);
                            double volume = vmp.Volume;

                            if (volume > maxvolume)
                            {
                                maxvolume = volume;
                                maxvolumebrep = brep;
                            }
                        }
                    }

                    else if (unionSecond.Length == 0)
                        maxvolumebrep = null;
                    else
                        maxvolumebrep = unionSecond[0];
                }
                Brep[] judgedSingleBlockBreps = new Brep[] { maxvolumebrep };


                if (maxvolumebrep == null)
                {
                    Console.WriteLine("Cannot be completed!");
                    Trace.WriteLine("Cannot be completed!");
                    repairedBrep = null;
                }


                else
                {
                    // 换用Split方法
                    Brep[] judged = maxvolumebrep.Split(judgeBrep, 0.01);

                    var largestOne = Find.FindTheLargestBrep(judged);
                    if (largestOne.IsSolid == false)
                        largestOne = largestOne.CapPlanarHoles(0.01);

                    repairedBrep = new Brep[] { largestOne };

                    Console.WriteLine("Repair Complete ! !");
                    Trace.WriteLine("Repair Complete ! !");
                }

                return repairedBrep;
            }
            else
            {
                return repairedBrep;
            }
        }   

        public Brep[] CreateJudgeBrep()
        {
            Brep judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, thisLand, 100);

            if (judgeBrep == null)
            {
                Curve judgeBase = thisLand.landCurve;
                judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, thisLand, 100);
            }

            Brep[] judgeBrepArray = new Brep[] { judgeBrep };
            return judgeBrepArray;
        }

        /// <summary>
        /// 平面曲面生成
        /// </summary>
        /// <returns></returns>
        public Brep[] CreateJudgeBrep(string condition = "brep")
        {
            Brep judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, thisLand, 100);

            if (judgeBrep == null)
            {
                Curve judgeBase = thisLand.landCurve;
                judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, thisLand, 100);
            }

            Brep[] judgeBrepArray = new Brep[] { judgeBrep };
            return judgeBrepArray;
        }


        public Dictionary<Curve, string> OffsetBehavioursOfLandcurves(Dictionary<string, string> offsetBehavioursOfDirections)
        {
            Dictionary<Curve, string> offsetBehavioursOfLandcurves = new Dictionary<Curve, string>();

            foreach (string direction in offsetBehavioursOfDirections.Keys)
            {
                foreach (Curve curve in allEdgeWithOffsetBaseCurve.Keys)
                {
                    if (thisLand.dispatchedEdges[direction].Contains(curve))
                    {
                        offsetBehavioursOfLandcurves[curve] = offsetBehavioursOfDirections[direction];
                    }
                }
            }
            return offsetBehavioursOfLandcurves;
        }

        public static Brep[] CreateJudgeBrepOfTheLand(
                                                Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve, 
                                                Land land)
        {
            

            // 建立JudgeBrep

            Brep judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, land, 100);

            if (judgeBrep == null)
            {
                Curve judgeBase = land.landCurve;
                judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, land, 100);
            }

            Brep[] judgeBrepArray = new Brep[] { judgeBrep };

            return judgeBrepArray;
        }
        

        public Dictionary<string, List<Curve>> DispatchEdgeThroughDirectionZones(Curve closeCurve, List<Curve> landCurves)
        {
           var absulatTolerance =  RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //// 将曲线分解为线段
            //List<Curve> landCurves = new List<Curve>();

            //if (closeCurve.IsClosed)
            //{
            //    Curve[] segments = closeCurve.DuplicateSegments();
            //    if (segments != null && segments.Length > 0)
            //    {
            //        landCurves.AddRange(segments);
            //    }
            //}
            //// 如果没有闭合，则先将其闭合再执行同样操作
            //else
            //{
            //    Curve closedCurve = closeCurve.DuplicateCurve();
            //    closedCurve.MakeClosed(0.001);
            //    Curve[] segments = closedCurve.DuplicateSegments();
            //    if (segments != null && segments.Length > 0)
            //    {
            //        landCurves.AddRange(segments);
            //    }
            //}

            double offset = 1;

            BoundingBox boundingBox = closeCurve.GetBoundingBox(true);

            double offsetOriginX = boundingBox.Diagonal.X / 3;
            double offsetOriginY = boundingBox.Diagonal.Y / 3;

            Point3d center = boundingBox.Center;

            Point3d horizontalStart = new Point3d(center.X, boundingBox.Min.Y, 0);
            Point3d horizontalEnd = new Point3d(center.X, boundingBox.Max.Y, 0);

            Point3d verticalStart = new Point3d(boundingBox.Min.X, center.Y, 0);
            Point3d verticalEnd = new Point3d(boundingBox.Max.X, center.Y, 0);

            Vector3d diagonal = boundingBox.Diagonal;
            double length = diagonal.Length;
            double extensionFactor = length / 2.0;

            Point3d horizontalStartExtended = Point3d.Add(horizontalStart, new Vector3d(horizontalStart - center) * extensionFactor);
            Point3d horizontalEndExtended = Point3d.Add(horizontalEnd, new Vector3d(horizontalEnd - center) * extensionFactor);

            Point3d verticalStartExtended = Point3d.Add(verticalStart, new Vector3d(verticalStart - center) * extensionFactor);
            Point3d verticalEndExtended = Point3d.Add(verticalEnd, new Vector3d(verticalEnd - center) * extensionFactor);

            Point3d leftStart = new Point3d(horizontalStartExtended.X - diagonal.X / 2.0 - offset, horizontalStartExtended.Y, 0);
            Point3d leftEnd = new Point3d(horizontalEndExtended.X - diagonal.X / 2.0 - offset, horizontalEndExtended.Y, 0);

            Point3d rightStart = new Point3d(horizontalStartExtended.X + diagonal.X / 2.0 + offset, horizontalStartExtended.Y, 0);
            Point3d rightEnd = new Point3d(horizontalEndExtended.X + diagonal.X / 2.0 + offset, horizontalEndExtended.Y, 0);

            Point3d topStart = new Point3d(verticalStartExtended.X, verticalStartExtended.Y + diagonal.Y / 2.0 + offset, 0);
            Point3d topEnd = new Point3d(verticalEndExtended.X, verticalEndExtended.Y + diagonal.Y / 2.0 + offset, 0);

            Point3d bottomStart = new Point3d(verticalStartExtended.X, verticalStartExtended.Y - diagonal.Y / 2.0 - offset, 0);
            Point3d bottomEnd = new Point3d(verticalEndExtended.X, verticalEndExtended.Y - diagonal.Y / 2.0 - offset, 0);

            // Generate the left, right, top, and bottom zones
            Curve westZone = GenerateZone(horizontalStartExtended, horizontalEndExtended, leftStart, leftEnd);
            westZone.Translate(new Vector3d(-offsetOriginX, 0, 0));

            Curve eastZone = GenerateZone(horizontalStartExtended, horizontalEndExtended, rightStart, rightEnd);
            eastZone.Translate(new Vector3d(offsetOriginX, 0, 0));

            Curve northZone = GenerateZone(verticalStartExtended, verticalEndExtended, topStart, topEnd);
            northZone.Translate(new Vector3d(0, offsetOriginY, 0));

            Curve southZone = GenerateZone(verticalStartExtended, verticalEndExtended, bottomStart, bottomEnd);
            southZone.Translate(new Vector3d(0, -offsetOriginY, 0));

            // Function to generate a zone
            Curve GenerateZone(Point3d startExtended, Point3d endExtended, Point3d start, Point3d end)
            {
                LineCurve upCurve = new LineCurve(startExtended, start);
                LineCurve downCurve = new LineCurve(endExtended, end);
                LineCurve startCurve = new LineCurve(startExtended, endExtended);
                LineCurve endCurve = new LineCurve(start, end);

                Curve[] curves = new Curve[] { upCurve, downCurve, startCurve, endCurve };

                return Curve.JoinCurves(curves)[0];
            }

            var relationshipDicFour = new Dictionary<string, List<Curve>>();
            relationshipDicFour["north"] = new List<Curve>();
            relationshipDicFour["south"] = new List<Curve>();
            relationshipDicFour["east"] = new List<Curve>();
            relationshipDicFour["west"] = new List<Curve>();

            List<Curve> haveBeenUsed = new List<Curve>();

            foreach (Curve landEdge in landCurves)
            {
                Point3d midpt = landEdge.PointAtNormalizedLength(0.5);

                if (northZone.Contains(midpt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside
                    && !haveBeenUsed.Contains(landEdge))
                {
                    relationshipDicFour["north"].Add(landEdge);
                    haveBeenUsed.Add(landEdge);
                }
                else if (southZone.Contains(midpt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside
                    && !haveBeenUsed.Contains(landEdge))
                {
                    relationshipDicFour["south"].Add(landEdge);
                    haveBeenUsed.Add(landEdge);
                }
                else if (westZone.Contains(midpt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside
                    && !haveBeenUsed.Contains(landEdge))
                {
                    relationshipDicFour["west"].Add(landEdge);
                    haveBeenUsed.Add(landEdge);
                }
                else if (eastZone.Contains(midpt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside
                    && !haveBeenUsed.Contains(landEdge))
                {
                    relationshipDicFour["east"].Add(landEdge);
                    haveBeenUsed.Add(landEdge);
                }
            }

            return relationshipDicFour;
        }
    }
}
