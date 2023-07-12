using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    internal static class Offset
    {

        // 构造函数
        private static readonly double absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

        /*-----------------------------------------偏移的核心方法--------------------------------------------------*/

        /// <summary>
        /// insideCurve, 用Mymethods中的 OffsetCurveAlongDirection来让边界向内偏移
        /// </summary>
        /// <param name="sideCurve"></param>
        /// <param name="spacing"></param>
        /// <param name="depth"></param>
        /// <param name="land"></param>
        /// <returns></returns>
        public static List<Curve> offsetSideCurve(Curve sideCurve, Curve land, double spacing, double depth)
        {
            // 
            // 说明
            // 输入：sideCurve，一条边界曲线
            // 输出：insideCurve，向内偏移后的曲线
            // 功能：根据输入的边界曲线，向内偏移一定距离，返回偏移后的曲线
            Curve insideCurve = Building.OffsetTowardsRightDirection(sideCurve, spacing, land);
            Curve outsideCurve = Building.OffsetTowardsRightDirection(sideCurve, depth, land);

            List<Curve> result = new List<Curve> { insideCurve, outsideCurve };

            return result;

        }

        public static List<Curve> offsetEndCurve(Curve endCurve, Curve land, double spacing)
        {
            Curve inEndCurve = Building.OffsetTowardsRightDirection(endCurve, spacing, land);


            List<Curve> result = new List<Curve> { inEndCurve };

            return result;
        }

        /*------------------------------------------------偏移的附属方法--------------------------------------------------*/

        /// <summary>
        ///  将curve按照正确的方向，偏移一个distance的距离
        /// </summary>
        /// <param name="curve 输入曲线"></param>
        /// <param name="distance 偏移距离"></param>
        /// <param name="land 所在的地块"></param>
        /// <returns></returns>
        public static Curve OffsetTowardsRightDirection(Curve curve, double distance, Curve land)
        {
            // 获取offsetPoint，使用getOffsetPoint方法
            Point3d offsetPoint = GetDirections(curve, land);

            //进行偏移，方向点为offsetPoint，距离为distance，基准平面为世界坐标系XY平面
            Curve offsetCurve = curve.Offset(offsetPoint, Plane.WorldXY.Normal, distance, absulatTolerance, CurveOffsetCornerStyle.Sharp)[0];

            return offsetCurve;
        }


        /// <summary>
        /// 获取输入curve所对应的正确偏移方向点
        /// </summary>
        /// <param name="curve"  输入曲线></param>
        /// <param name="land"  地块></param>
        /// <returns></returns>
        public static Point3d GetDirections(Curve curve, Curve land)
        {

            // 旋转复制curve，创建在需要平面上垂直于曲线curve的verticalLine
            Curve verticalLine = curve.DuplicateCurve();
            verticalLine.Rotate(90, Vector3d.ZAxis, curve.PointAtStart);


            // 获取线的两个端点
            Point3d[] endPoints = { verticalLine.PointAtStart, verticalLine.PointAtEnd };

            // 检查端点是否在地块内
            int count = 0;
            foreach (Point3d endPoint in endPoints)
            {
                if (land.Contains(endPoint, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                {
                    count++;
                }
            }

            // 根据端点的数量返回方向
            Point3d offsetPoint = endPoints[0];

            if (endPoints.Length == 2)
            {
                // 创建判断点
                Curve offsetCurve = curve.Offset(endPoints[0], Rhino.Geometry.Plane.WorldXY.Normal, 0.01, absulatTolerance, CurveOffsetCornerStyle.Sharp)[0];
                double[] paras = offsetCurve.DivideByCount(7, true);
                Point3d[] judgePoints = new Point3d[paras.Length];

                for (int i = 0; i < paras.Length; i++)
                {
                    judgePoints[i] = offsetCurve.PointAtNormalizedLength(paras[i]);
                }

                // 若judgepoints有三个点在land外，则返回endpoints[1]，否则返回endpoints[0]
                int countInside = 0;
                foreach (Point3d judgePoint in judgePoints)
                {
                    if (land.Contains(judgePoint, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                    {
                        countInside++;
                    }
                }
                if (countInside >= 3)
                {
                    offsetPoint = endPoints[0];
                }
                else
                {
                    offsetPoint = endPoints[1];
                }
            }
            else
            {
                offsetPoint = endPoints[0];
            }

            return offsetPoint;
        }
    }
}
