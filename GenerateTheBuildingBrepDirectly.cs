//using Grasshopper.Kernel.Geometry;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    internal static class GenerateTheBuildingBrepDirectly
    {
        // 核心方法
        public static Plane GenerateSingleFloor(Curve baseOffsetedCurve, Curve landCurve)
        {
            // 旋转90度，形成直角坐标系
            Curve verticalCurve = RotateCurveClockwiseBy90Degrees(baseOffsetedCurve);

            // 找到指向内部的endPoint
            // 默认就是end
            Point3d verticalStartPoint = verticalCurve.PointAtStart;
            Point3d verticalEndPoint = verticalCurve.PointAtEnd;

            Point3d realEndPoint = GetEndpointWithinLandCurve(verticalCurve, landCurve);

            Plane plane;

            if (realEndPoint.Equals(verticalEndPoint))
            {
                Vector3d xAxis = baseOffsetedCurve.PointAtStart - baseOffsetedCurve.PointAtEnd;
                Vector3d yAxis = verticalStartPoint - verticalEndPoint;

                Point3d origin = baseOffsetedCurve.PointAtStart;
                plane = new Rhino.Geometry.Plane(origin, xAxis, yAxis);
            }
            else
            {
                Vector3d xAxis = baseOffsetedCurve.PointAtStart - baseOffsetedCurve.PointAtEnd;
                Vector3d yAxis = verticalEndPoint - verticalStartPoint;

                Point3d origin = baseOffsetedCurve.PointAtStart;
                plane = new Rhino.Geometry.Plane(origin, xAxis, yAxis);
            }

            return plane;
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
            var xform = Transform.Rotation(-Math.PI / 2, midpoint);
            var rotatedCurve = curve.DuplicateCurve();
            rotatedCurve.Transform(xform);
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
            if (landCurve.Contains(startPoint, Plane.WorldXY, tolerance) != PointContainment.Outside)
            {
                pointsWithScore[startPoint] += 1;
            }

            if (landCurve.Contains(endPoint, Plane.WorldXY, tolerance) != PointContainment.Outside)
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

        public static Brep CreateBox(Plane plane, double xLength, double yLength, double zHight)
        {
            var xSize = new Interval(0,xLength);
            var ySize = new Interval(0,yLength);
            var zSize = new Interval(0,zHight);

            var box = new Box(plane, xSize, ySize, zSize).ToBrep();

            return box;
        }



        

        // 



}
