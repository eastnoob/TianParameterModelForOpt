//using Grasshopper.Kernel.Geometry;
using MoreLinq;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TianParameterModelForOpt._4_repair;
using Plane = Rhino.Geometry.Plane;

namespace TianParameterModelForOpt
{
    internal static class GenerateTheBuildingBrepDirectly
    {
        // 核心方法
        public static Brep GenerateSingleFloor(Curve baseOffsetedCurve, Curve landCurve, double xLength, double ylength, double zHight)
        {
            //// 旋转90度，形成直角坐标系
            //Curve verticalCurve = RotateCurveClockwiseBy90Degrees(baseOffsetedCurve);

            //// 找到指向内部的endPoint
            //// 默认就是end
            //Point3d verticalStartPoint = verticalCurve.PointAtStart;
            //Point3d verticalEndPoint = verticalCurve.PointAtEnd;

            //Point3d realEndPoint = GetEndpointWithinLandCurve(verticalCurve, landCurve);

            Plane plane;

            //if (realEndPoint.Equals(verticalEndPoint))
            //{

            Line lineX = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd);
            //Line lineY = new Line(verticalStartPoint, realEndPoint);

            Vector3d xAxis = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd).Direction;
            //Vector3d yAxis = new Line(verticalStartPoint, realEndPoint).Direction;
            Vector3d yAxis = CreateRightVextorThatOrientToInside(baseOffsetedCurve, AreaMassProperties.Compute(landCurve).Centroid);

            Point3d origin = baseOffsetedCurve.PointAtStart;
            plane = new Rhino.Geometry.Plane(origin, xAxis, yAxis);

            //if (plane.Normal.Z == -1)
            //{
            //    plane.Flip();
            //    plane = new Plane(plane.Origin, new Vector3d(0, 0, 1));
            //}


            //}
            //else
            //{
            //    Vector3d xAxis = baseOffsetedCurve.PointAtStart - baseOffsetedCurve.PointAtEnd;
            //    Vector3d yAxis = realEndPoint - verticalEndPoint ;

            //    Point3d origin = baseOffsetedCurve.PointAtStart;
            //    plane = new Rhino.Geometry.Plane(origin, xAxis, yAxis);
            //}

            // 生成Brep
            Brep floorBrep = CreateBox(plane, xLength, ylength, zHight);

            return floorBrep;
        }

        // 直接生成指向点
        public static Vector3d CreateRightVextorThatOrientToInside(Curve curve, Point3d targetPoint)
        {
            // 获取曲线的中点
            double t;
            curve.LengthParameter(curve.GetLength() / 2, out t);
            Point3d midPoint = curve.PointAt(t);

            // 创建向量
            Vector3d vector = targetPoint - midPoint;

            // 获取曲线在中点的切线向量
            Vector3d tangent = curve.TangentAt(t);

            // 计算垂直于切线向量、且大致指向目标点的向量
            Vector3d perpVector = Vector3d.CrossProduct(tangent, Vector3d.ZAxis);
            if (perpVector.Length < 10) { perpVector *= 10; }
            if (Vector3d.VectorAngle(perpVector, vector) > Math.PI / 2)
            {
                perpVector = -perpVector;
            }

            // 创建线段
            Line perpLine = new Line(midPoint, midPoint + perpVector * 10);

            return perpVector;
        }


        // 第二步，构建直角坐标系
        // 2.1 旋转一条线
        public static Curve RotateCurveClockwiseBy90Degrees(Curve curve)
        {
            //curve.Shorten()
            var midpoint = curve.PointAtNormalizedLength(0.5);
            //var xform = Transform.Rotation(-Math.PI / 2, midpoint);
            var rotatedCurve = curve.DuplicateCurve();
            rotatedCurve.Rotate(90, Plane.WorldXY.Normal, midpoint);
            return rotatedCurve;
        }

        // 2.2 判断其方向
        public static Point3d GetEndpointWithinLandCurve(Curve curve, Curve landCurve)
        {
            var startPoint = curve.PointAtStart;
            var endPoint = curve.PointAtEnd;

            Dictionary<Point3d, int> pointsWithScore = new Dictionary<Point3d, int> {
                {startPoint, 0},
                {endPoint, 0}
            };
            // 先算距离分
            var centroid = AreaMassProperties.Compute(landCurve).Centroid;

            return centroid;

            var startDistance = startPoint.DistanceTo(centroid);
            var endDistance = startPoint.DistanceTo(centroid);

            if (startDistance > endDistance)
                pointsWithScore[startPoint] += 1;
            else
                pointsWithScore[endPoint] += 1;

            // 再算内外分
            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // create a list to store the endpoints within landCurve
            var endpoints = new List<Point3d>();

            // check if endpoint is within landCurve
            if (landCurve.Contains(startPoint, Plane.WorldXY, tolerance) == PointContainment.Inside)
            {
                pointsWithScore[startPoint] += 1;
            }

            if (landCurve.Contains(endPoint, Plane.WorldXY, tolerance) == PointContainment.Inside)
            {
                pointsWithScore[endPoint] += 1;
            }


            if (pointsWithScore[startPoint] > pointsWithScore[endPoint])
                return startPoint;

            else if (pointsWithScore[startPoint] < pointsWithScore[endPoint])
                return endPoint;

            else
                return centroid;

            // if there are no endpoints within landCurve, return the centroid of landCurve
            if (endpoints.Count == 0)
            {
                return AreaMassProperties.Compute(landCurve).Centroid;
            }

            // if there are two endpoints within landCurve, return the midpoint
            else if (endpoints.Count == 2)
            {
                return AreaMassProperties.Compute(landCurve).Centroid;
            }

            else
            {
                // if there is only one endpoint within landCurve, return that endpoint
                return endpoints[0];
            }
        }

        // 第三部，生成box

        public static Brep CreateBox(Rhino.Geometry.Plane plane, double xLength, double yLength, double zHight)
        {

            var xSize = new Interval(0, xLength);
            var ySize = new Interval(0, yLength);
            var zSize = new Interval(0, zHight);



            var box = new Box(plane, xSize, ySize, zSize).ToBrep();

            if (box == null)
            {
                return box;
            }
            else {
                Point3d centroidOfJudgeBrep = AreaMassProperties.Compute(box).Centroid;

                // 如果upperFace的起点Z坐标大于0，则extrude向下移动
                if (centroidOfJudgeBrep.Z <= 0 || plane.Normal.Z <= -1)
                {
                    box.Transform(Transform.Translation(new Vector3d(0, 0, zHight)));

                    //var newZ = new Interval(0, -zHight);
                    //var newBox = new Box(plane, xSize, ySize, newZ).ToBrep();
                    //if (newBox != null)
                    //{
                    //    if (newBox.IsSolid == false)
                    //    {
                    //        newBox.CapPlanarHoles(0.01);
                    //    }
                    //    else
                    //    {
                    //        box = newBox;
                    //    }
                    //}
                }
                return box;

            }

        }

        /// <summary>
        /// 建立一个JudgeBrep,用于通过并集删除不需要的部分
        /// </summary>
        /// <param name="curveWithOffset"></param>
        /// <param name="land"></param>
        /// <param name="buildingHeight"></param>
        /// <returns></returns>
        public static Brep CreateJudgeBrepBase(Dictionary<Curve, Curve> curveWithOffset, Land land, double buildingHeight)
        {

            Curve baseJudgeCurveRegin = land.landCurve;
            bool problem = false;

            foreach (Curve offseted in curveWithOffset.Values)
            {
                Curve judgeCurveOfThisCurve = GenerateTheBuildingBrepDirectly.GenerateJudgeCurve(offseted, land.landCurve, 100, -100);

                Curve realJudge = null;

                if (judgeCurveOfThisCurve != null)
                {
                    Curve[] judged = Curve.CreateBooleanDifference(baseJudgeCurveRegin, judgeCurveOfThisCurve, 0.01);

                    if (judged.Length > 1)
                    {
                        realJudge = Find.FindTheLargestCurve(judged);
                    }
                    else if (judged.Length < 1)
                    {
                        //problem = true;
                        continue;
                    }
                    else if(judged == null)
                    {
                        continue;
                    }
                    else
                    {
                        realJudge = judged[0];
                    }
                }
                else
                {
                    problem = true;
                    continue;
                }

                baseJudgeCurveRegin = realJudge;
            }

            Brep JudgeBrepBase = Extrusion.Create(baseJudgeCurveRegin, 100, true).ToBrep();

            if (/*problem == true || */JudgeBrepBase == null)
            {
                List<Point3d> intersections = new List<Point3d>();

                // 做出来相交的点

                foreach (Curve originalCurve1 in curveWithOffset.Keys)
                {
                    foreach (Curve originalCurve2 in curveWithOffset.Keys)
                    {
                        bool isIntersection = Intersect.IsNeedIntersectedOrNot(originalCurve1, originalCurve2, 0.001);

                        if (isIntersection == true)
                        {
                            var offsetIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveWithOffset[originalCurve1], curveWithOffset[originalCurve2], 0.01, 0.01);

                            if (offsetIntersection.Count == 0)
                            {
                                Line curve1 = new Line(curveWithOffset[originalCurve1].PointAtStart, curveWithOffset[originalCurve1].PointAtEnd);
                                Line curve2 = new Line(curveWithOffset[originalCurve2].PointAtStart, curveWithOffset[originalCurve2].PointAtEnd);

                                double parameter1;
                                double parameter2;


                                bool lineLineIntersection = Rhino.Geometry.Intersect.Intersection.LineLine(curve1, curve2, out parameter1, out parameter2, 0.001, false);

                                if (lineLineIntersection == true)
                                {
                                    if (land.landCurve.Contains(curve1.PointAt(parameter1), Plane.WorldXY, 0.01) == PointContainment.Inside
                                    || land.landCurve.Contains(curve1.PointAt(parameter1), Plane.WorldXY, 0.01) == PointContainment.Coincident)

                                        intersections.Add(curve1.PointAt(parameter1));
                                }


                                else
                                    Console.WriteLine("没交点");
                            }
                            else
                            {
                                if (land.landCurve.Contains(offsetIntersection[0].PointA, Plane.WorldXY, 0.01) == PointContainment.Inside
                                    || land.landCurve.Contains(offsetIntersection[0].PointA, Plane.WorldXY, 0.01) == PointContainment.Coincident)

                                    intersections.Add(offsetIntersection[0].PointA);
                            }
                        }
                        else
                            Console.WriteLine("没交点");
                    }
                }

                //排序并组成图形
                Point3d[] sortedPointList = Point3d.SortAndCullPointList(intersections, 0.1);
                PolylineCurve judgeCurve = new PolylineCurve(sortedPointList);
                if (judgeCurve.IsClosed != true)
                {
                    List<Point3d> withFirst = new List<Point3d>(sortedPointList);
                    withFirst.Add(sortedPointList[0]);
                    judgeCurve = new PolylineCurve(withFirst);
                }


                // 挤出JudgeCurve为实体, 挤出的长度是buildingHeight
                Brep baseSurf = Brep.CreatePlanarBreps(judgeCurve, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)[0];
                //var extrude = Extrusion.Create(baseSurf[0], (Plane.WorldXY.Normal * 100).Length, true).ToBrep();
                //var extrude = Extrusion.Create(baseSurf,100, true).ToBrep();


                var extrude = Extrusion.Create(judgeCurve, (Vector3d.ZAxis * 100).Length, true).ToBrep();


                // 计算Brep对象的面积和质心
                Point3d centroidOfJudgeBrep = AreaMassProperties.Compute(extrude).Centroid;


                // 如果upperFace的起点Z坐标大于0，则extrude向下移动
                if (centroidOfJudgeBrep.Z > 0) { extrude.Transform(Transform.Translation(new Vector3d(0, 0, -buildingHeight / 2))); }
                else { extrude.Transform(Transform.Translation((new Vector3d(0, 0, buildingHeight / 2)))); }

                if (extrude.IsSolid == false)
                {
                    extrude.CapPlanarHoles(0.01);
                }
                return extrude;
            }


            else
            {
                var move = 0 - AreaMassProperties.Compute(JudgeBrepBase).Centroid.Z;

                JudgeBrepBase.Transform(Transform.Translation(new Vector3d(0, 0, move)));

                //var test = AreaMassProperties.Compute(JudgeBrepBase).Centroid.Z; 

                return JudgeBrepBase;
            }
            ////return baseJudgeCurveRegin;

            //Brep originalJudgeBrep;
            //originalJudgeBrep = Extrusion.Create(land.landCurve, (Vector3d.ZAxis * buildingHeight).Length, true).ToBrep();

            //Trans(originalJudgeBrep);

            ////Brep finalJudgeBrep = originalJudgeBrep;

            //bool problem = false;

            //foreach (Curve originalCurve in curveWithOffset.Values)
            //{
            //    Brep[] newBreps = null;
            //    Brep newBrep = null;

            //    if (originalCurve.GetLength() <= 10)
            //    {
            //        continue;
            //    }

            //    Brep usedToDifferent = GenerateTheBuildingBrepDirectly.GenerateJudgeFloor(originalCurve,
            //        land.landCurve, originalCurve.GetLength() * 10,
            //        -originalCurve.GetLength() * 10,
            //        buildingHeight);

            //    if (usedToDifferent != null)
            //    {
            //        Trans(usedToDifferent);
            //    }
            //    else
            //    {
            //        continue;
            //    }

            //    if (DoBrepsIntersect(originalJudgeBrep, usedToDifferent))
            //    {
            //        newBreps = Brep.CreateBooleanDifference(originalJudgeBrep, usedToDifferent, 0.01);
            //    }

            //    else
            //    { continue; }

            //    if (newBreps.Length > 1)
            //    {
            //        newBrep = Find.FindTheLargestBrep(newBreps);
            //        if (!newBrep.IsSolid)
            //        {
            //            newBrep.CapPlanarHoles(0.01);
            //            originalJudgeBrep = newBrep;
            //        }
            //    }

            //    else if (newBreps.Length == 1)
            //    {
            //        newBrep = newBreps[0];
            //        if (!newBrep.IsSolid)
            //        {
            //            newBrep.CapPlanarHoles(0.01);
            //            originalJudgeBrep = newBrep;
            //        }
            //    }
            //    else if (newBreps == null || newBreps.Length == 0 || newBreps.Contains(null))
            //    {
            //        // 换用Split方法
            //        Brep[] judged = originalJudgeBrep.Split(usedToDifferent, 0.01);

            //        if (judged == null || judged.Length == 0 || judged.Contains(null))
            //        {
            //            problem = true;
            //            break;
            //        }

            //        else
            //        {
            //            var largestOne = Find.FindTheLargestBrep(judged);

            //            if (largestOne.IsSolid == false)
            //            { newBrep = largestOne.CapPlanarHoles(0.01); }
            //            else
            //            { newBrep = largestOne; }

            //            originalJudgeBrep = newBrep;
            //        }
            //    }
            //}

            //if (problem == false)
            //{
            //    return originalJudgeBrep;
            //}
            //else
            //{
            //    List<Point3d> intersections = new List<Point3d>();

            //    // 做出来相交的点

            //    foreach (Curve originalCurve1 in curveWithOffset.Keys)
            //    {
            //        foreach (Curve originalCurve2 in curveWithOffset.Keys)
            //        {
            //            bool isIntersection = Intersect.IsNeedIntersectedOrNot(originalCurve1, originalCurve2, 0.001);

            //            if (isIntersection == true)
            //            {
            //                var offsetIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveWithOffset[originalCurve1], curveWithOffset[originalCurve2], 0.01, 0.01);

            //                if (offsetIntersection.Count == 0)
            //                {
            //                    Line curve1 = new Line(curveWithOffset[originalCurve1].PointAtStart, curveWithOffset[originalCurve1].PointAtEnd);
            //                    Line curve2 = new Line(curveWithOffset[originalCurve2].PointAtStart, curveWithOffset[originalCurve2].PointAtEnd);

            //                    double parameter1;
            //                    double parameter2;


            //                    bool lineLineIntersection = Rhino.Geometry.Intersect.Intersection.LineLine(curve1, curve2, out parameter1, out parameter2, 0.001, false);

            //                    if (lineLineIntersection == true)
            //                    {
            //                        if (land.landCurve.Contains(curve1.PointAt(parameter1), Plane.WorldXY, 0.01) == PointContainment.Inside
            //                        || land.landCurve.Contains(curve1.PointAt(parameter1), Plane.WorldXY, 0.01) == PointContainment.Coincident)

            //                            intersections.Add(curve1.PointAt(parameter1));
            //                    }


            //                    else
            //                        Console.WriteLine("没交点");
            //                }
            //                else
            //                {
            //                    if (land.landCurve.Contains(offsetIntersection[0].PointA, Plane.WorldXY, 0.01) == PointContainment.Inside
            //                        || land.landCurve.Contains(offsetIntersection[0].PointA, Plane.WorldXY, 0.01) == PointContainment.Coincident)

            //                        intersections.Add(offsetIntersection[0].PointA);
            //                }
            //            }
            //            else
            //                Console.WriteLine("没交点");
            //        }
            //    }

            //    //排序并组成图形
            //    Point3d[] sortedPointList = Point3d.SortAndCullPointList(intersections, 0.1);
            //    PolylineCurve judgeCurve = new PolylineCurve(sortedPointList);
            //    if (judgeCurve.IsClosed != true)
            //    {
            //        List<Point3d> withFirst = new List<Point3d>(sortedPointList);
            //        withFirst.Add(sortedPointList[0]);
            //        judgeCurve = new PolylineCurve(withFirst);
            //    }


            //    // 挤出JudgeCurve为实体, 挤出的长度是buildingHeight
            //    Brep baseSurf = Brep.CreatePlanarBreps(judgeCurve, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)[0];
            //    //var extrude = Extrusion.Create(baseSurf[0], (Plane.WorldXY.Normal * 100).Length, true).ToBrep();
            //    //var extrude = Extrusion.Create(baseSurf,100, true).ToBrep();


            //    var extrude = Extrusion.Create(judgeCurve, (Vector3d.ZAxis * 100).Length, true).ToBrep();


            //    // 计算Brep对象的面积和质心
            //    Point3d centroidOfJudgeBrep = AreaMassProperties.Compute(extrude).Centroid;


            //    // 如果upperFace的起点Z坐标大于0，则extrude向下移动
            //    if (centroidOfJudgeBrep.Z > 0) { extrude.Transform(Transform.Translation(new Vector3d(0, 0, -buildingHeight / 2))); }
            //    else { extrude.Transform(Transform.Translation((new Vector3d(0, 0, buildingHeight / 2)))); }

            //    if (extrude.IsSolid == false)
            //    {
            //        extrude.CapPlanarHoles(0.01);
            //    }
            //    return extrude;
            //}


            //void Trans(Brep brep, double height)
            //{
            //    Point3d centroid = AreaMassProperties.Compute(brep).Centroid;

            //    var movedistance = 0 - centroid.Z;

            //    brep.Transform(Transform.Translation(new Vector3d(0, 0, movedistance / 2)));
            //    //if (centroid.Z > 0)
            //    //{ 
            //    //    brep.Transform(Transform.Translation(new Vector3d(0, 0, -buildingHeight / 2))); 
            //    //}
            //    //else
            //    //{ 
            //    //    brep.Transform(Transform.Translation((new Vector3d(0, 0, buildingHeight / 2))));
            //    //}

            //    if (brep.IsSolid == false)
            //    {
            //        brep.CapPlanarHoles(0.01);
            //    }
            //    //return brep;
            //}

            //bool DoBrepsIntersect(Rhino.Geometry.Brep brep1, Rhino.Geometry.Brep brep2)
            //{
            //    const double tolerance = 0.001;
            //    Curve[] intersectionCurve;
            //    Point3d[] pts;
            //    bool intersection = Rhino.Geometry.Intersect.Intersection.BrepBrep(brep1, brep2, tolerance, out intersectionCurve, out pts);

            //    bool isIntersect = true;


            //    if (intersectionCurve == null || pts == null || intersectionCurve.Length == 0 || pts.Length == 0)
            //    {
            //        isIntersect = false;
            //    }

            //    // 如果交集列表非空，那么两个BREP相交
            //    //return intersection?.Count > 0;
            //    return isIntersect;
            //}
        }











public static Brep GenerateJudgeFloor(Curve baseOffsetedCurve, Curve landCurve, double xLength, double ylength, double zHight)
        {

            Plane plane;

            Line lineX = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd);
            //Line lineY = new Line(verticalStartPoint, realEndPoint);

            Vector3d xAxis = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd).Direction*10;
            //Vector3d yAxis = new Line(verticalStartPoint, realEndPoint).Direction;
            Vector3d yAxis = CreateRightVextorThatOrientToInside(baseOffsetedCurve, AreaMassProperties.Compute(landCurve).Centroid);

            Point3d origin = baseOffsetedCurve.PointAtNormalizedLength(0.5);

            plane = new Rhino.Geometry.Plane(origin, xAxis*10, yAxis*10);

            Brep CreateJudgeBox(Rhino.Geometry.Plane baseplane, double xLen, double yLen, double zHigh)
            {

                var xSize = new Interval(-xLen/2, xLen/2);
                var ySize = new Interval(0, yLen);
                var zSize = new Interval(0, zHigh);

                //var boxtest = new Box(baseplane, xSize, ySize, zSize);
                //var test2 = boxtest.ToExtrusion();
                var box = new Box(baseplane, xSize, ySize, zSize).ToBrep();

                if (box == null)
                {
                    return box;
                }
                else
                {
                    Point3d centroidOfJudgeBrep = AreaMassProperties.Compute(box).Centroid;

                    var moveDistance = 0 - centroidOfJudgeBrep.Z;
                    if(moveDistance != 0)
                    {
                        box.Transform(Transform.Translation(new Vector3d(0, 0, moveDistance)));
                    }
                    //// 如果upperFace的起点Z坐标大于0，则extrude向下移动
                    //if (centroidOfJudgeBrep.Z < 0 || plane.Normal.Z <= -1)
                    //{
                    //    box.Transform(Transform.Translation(new Vector3d(0, 0, zHight)));
                    //}
                    return box;
                }
            }

            // 生成Brep
            Brep floorBrep = CreateJudgeBox(plane, xLength, ylength, zHight);

            return floorBrep;
        }


        public static Curve GenerateJudgeCurve(Curve baseOffsetedCurve, Curve landCurve, double xLength, double ylength)
        {

            Plane plane;

            Line lineX = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd);
            //Line lineY = new Line(verticalStartPoint, realEndPoint);

            Vector3d xAxis = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd).Direction * 10;
            //Vector3d yAxis = new Line(verticalStartPoint, realEndPoint).Direction;
            Vector3d yAxis = CreateRightVextorThatOrientToInside(baseOffsetedCurve, AreaMassProperties.Compute(landCurve).Centroid);

            Point3d origin = baseOffsetedCurve.PointAtNormalizedLength(0.5);

            //plane = new Rhino.Geometry.Plane(origin, xAxis * 10, yAxis * 10);

            Curve inin = baseOffsetedCurve.DuplicateCurve();
            Curve insideSide = inin.Extend(CurveEnd.Both, xLength, CurveExtensionStyle.Line);

            Curve otherSide = insideSide.DuplicateCurve();
            otherSide.Translate(yAxis * ylength);


            Curve startEnd = new LineCurve(insideSide.PointAtStart, otherSide.PointAtStart);
            Curve endEnd = new LineCurve(insideSide.PointAtEnd, otherSide.PointAtEnd);

            Curve judgeCurveRegin = Curve.JoinCurves(new List<Curve> { startEnd, insideSide, endEnd, otherSide })[0];

            if (judgeCurveRegin.IsClosed == false)
            {
                judgeCurveRegin.MakeClosed(0.01);
            }
            //// 生成Brep
            //Brep floorBrep = CreateJudgeBox(plane, xLength, ylength, zHight);

            return judgeCurveRegin;
        }
    }
}

