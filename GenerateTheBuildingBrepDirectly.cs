//using Grasshopper.Kernel.Geometry;
using MoreLinq;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Plane = Rhino.Geometry.Plane;

namespace TianParameterModelForOpt
{
    internal static class GenerateTheBuildingBrepDirectly
    {
        // 核心方法
        public static Brep GenerateSingleFloor(Curve baseOffsetedCurve, Curve landCurve, double xLength, double ylength, double zHight)
        {
            // 旋转90度，形成直角坐标系
            Curve verticalCurve = RotateCurveClockwiseBy90Degrees(baseOffsetedCurve);

            // 找到指向内部的endPoint
            // 默认就是end
            Point3d verticalStartPoint = verticalCurve.PointAtStart;
            Point3d verticalEndPoint = verticalCurve.PointAtEnd;

            Point3d realEndPoint = GetEndpointWithinLandCurve(verticalCurve, landCurve);

            Plane plane;

            //if (realEndPoint.Equals(verticalEndPoint))
            //{

            Line lineX = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd);
            Line lineY = new Line(verticalStartPoint, realEndPoint);

            Vector3d xAxis = new Line(baseOffsetedCurve.PointAtStart, baseOffsetedCurve.PointAtEnd).Direction;
            Vector3d yAxis = new Line(verticalStartPoint, realEndPoint).Direction;

            Point3d origin = baseOffsetedCurve.PointAtStart;
            plane = new Rhino.Geometry.Plane(origin, xAxis, yAxis);

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



        /*        // 第一步，通过两条线生成一个plane
                public static Plane CreateAPlaneBaseOnTwoVerticalCurve(Line line1, Line line2)
                {
                    var xAxis = line1.Direction;
                    var yAxis = line2.Direction;
                    var origin = line1.From;

                    var zAxis = Vector3d.CrossProduct(xAxis, yAxis);
                    var plane = new Rhino.Geometry.Plane(origin, xAxis, yAxis);
                    return plane;
                }*/


        // 第二步，构建直角坐标系
        // 2.1 旋转一条线
        public static Curve RotateCurveClockwiseBy90Degrees(Curve curve)
        {
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

            return box;
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
            List<Point3d> intersections = new List<Point3d>();

            // 做出来相交的点

            foreach (Curve originalCurve1 in curveWithOffset.Keys)
            {
                foreach (Curve originalCurve2 in curveWithOffset.Keys)
                {
                    bool isIntersection = Intersect.IsNeedIntersectedOrNot(originalCurve1, originalCurve2, 0.001);

                    if (isIntersection == true)
                    {
                        var offsetIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveWithOffset[originalCurve1], curveWithOffset[originalCurve2], 0.001, 0.001);

                        if (offsetIntersection.Count == 0)
                        {
                            Line curve1 = new Line(curveWithOffset[originalCurve1].PointAtStart, curveWithOffset[originalCurve1].PointAtEnd);
                            Line curve2 = new Line(curveWithOffset[originalCurve2].PointAtStart, curveWithOffset[originalCurve2].PointAtEnd);

                            double parameter1;
                            double parameter2;


                            bool lineLineIntersection = Rhino.Geometry.Intersect.Intersection.LineLine(curve1, curve2, out parameter1, out parameter2, 0.001, false);

                            if (lineLineIntersection == true)
                                intersections.Add(curve1.PointAt(parameter1));

                            else
                                Console.WriteLine("没交点");
                        }
                        else
                        {
                            intersections.Add(offsetIntersection[0].PointA);
                        }
                    }
                    else
                        Console.WriteLine("没交点");
                }
            }

            //排序并组成图形
            Point3d[] sortedPointList = Point3d.SortAndCullPointList(intersections, 0.001);
            PolylineCurve judgeCurve = new PolylineCurve(sortedPointList);
            if(judgeCurve.IsClosed != true)
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
            if (centroidOfJudgeBrep.Z > 0){ extrude.Transform(Transform.Translation(new Vector3d(0, 0, -buildingHeight / 2))); }
            else { extrude.Transform(Transform.Translation((new Vector3d(0, 0, buildingHeight / 2)))); }

            if (extrude.IsSolid == false)
            {
                extrude.CapPlanarHoles(0.01);
            }


            return extrude;
            //extrude.Transform(Transform.Translation(0, 0, buildingHeight/2));

            //if(extrude.IsSolid == false)
            //{
            //    extrude.CapPlanarHoles(0.01);
            //}
            //return extrude;



            //double t0, t1, t;
            //double angleTolerance = RhinoMath.ToRadians(10.0); // 角度容差
            //double curvatureTolerance = RhinoMath.SqrtEpsilon; // 曲率容差

            //if (JudgeCurve.GetNextDiscontinuity(Continuity.C1_locus_continuous, JudgeCurve.Domain.Min, JudgeCurve.Domain.Max, angleTolerance, curvatureTolerance, out t))
            //{
            //    // 发现了不连续点，说明曲线自交
            //    // 这里可以处理自交的情况
            //}
        }
        //public static List<Point3d> SortPointsByCurve(List<Point3d> points, Curve landCurve)
        //{
        //    // 获取landCurve的拐点
        //  List<Point3d> curvePoints = landCurve.GetCurvaturePoints(points);

        //  // 将拐点按照它们在曲线上的顺序排序
        //    curvePoints.Sort((a, b) =>
        //    {
        //        double tA = landCurve.ClosestParameter(a);
        //        double tB = landCurve.ClosestParameter(b);
        //        return tA.CompareTo(tB);
        //    });

        //    // 将点按照它们到每个拐点的距离排序
        //    List<Point3d> sortedPoints = curvePoints.Select(p =>
        //    {
        //        Point3d closestPoint = landCurve.ClosestPoint(p);
        //        double t = landCurve.ClosestParameter(closestPoint);
        //        double distance = closestPoint.DistanceTo(p);
        //        return new { Point = p, T = t, Distance = distance };
        //    })
        //    .OrderBy(x => x.T)
        //    .ThenBy(x => x.Distance)
        //    .Select(x => x.Point)
        //    .ToList();

        //    return sortedPoints;

        //}
    }
}


        //
