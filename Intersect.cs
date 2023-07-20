using Rhino.Geometry.Intersect;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace TianParameterModelForOpt
{
    public static class Intersect
    {

        /// <summary>
        /// 输入两条线，确定它们要不要做intersect这个动作(暂时废弃)
        /// </summary>
        /// <param name="curve1">第一条线</param>
        /// <param name="curve2">第二条线</param>
        /// <param name="baseCurve">基地线</param>
        /// <param name="tolerance">容差</param>
        /// <param name="yes_or_no">输出：这两条线到底要不要相交</param>
        /// <returns>输出两者的交点intersection</returns>
        public static Point3d judgeIfNeedIntersection(Curve curve1, Curve curve2, Curve baseCurve, out bool yes_or_no, double tolerance = 0.001)
        {

            // 仅当originCrv中的两条曲线相交时候（有交点），才将offsetedCrv中的曲线进行相交操作，并传回交点们，布尔表示相交了还是没有

            //double absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            yes_or_no = false;

            List<Point3d> intersectionPoints = new List<Point3d>();

            // 若原始线相交
            if (Intersection.CurveCurve(curve1, curve2, tolerance, tolerance).Count > 0)
            {
                // 而且两条原始线不是同一条线
                if (curve1.Equals(curve2) != true)
                {
                    // 那么偏移结果就得有
                    yes_or_no = true;
                }
            }

            //    for (int i = 0; i < originCrv.Count; i++)
            //{
            //    for (int j = 0; j < originCrv.Count; j++)
            //    {
            //        /*                    if (i != j && originCrv[i].Intersect(offsetedCrv[j], out var intersectionPoint, 0, 0))
            //        */
            //        if (i != j
            //            && Intersection.CurveCurve(originCrv[i], originCrv[j], absulatTolerance, absulatTolerance).Count > 0)
            //        {
            //            yes_or_no = true;
            //        }
            //    }
            //}

            if (yes_or_no == true)
            {
                // !将offsetedCrv中的曲线进行相交操作，并传回交点们（不使用循环，仅处理两条边）
                //CurveIntersections events = Intersection.CurveCurve(offsetedCrv[0], offsetedCrv[1], absulatTolerance, absulatTolerance);

                // 获取 line1 的起点和终点
                Point3d start1 = curve1.PointAtStart;
                Point3d end1 = curve1.PointAtEnd;

                // 获取 line2 的起点和终点
                Point3d start2 = curve2.PointAtStart;
                Point3d end2 = curve2.PointAtEnd;

                // 创建 Line 对象
                Line lineObj1 = new Line(start1, end1);
                Line lineObj2 = new Line(start2, end2);

                Point3d intersection;
                // 先判断作为curve的两条curve是否相交
                var curveCurveIntersection = Intersection.CurveCurve(curve1, curve2, tolerance, tolerance);

                // line Line intersect用
                double para1;
                double para2;

                if (curveCurveIntersection.Count > 0)
                {
                    intersection = curveCurveIntersection[0].PointA;
                    // 如果相交，则检查交点是否在给定曲线内
                    if (baseCurve.Contains(intersection, Rhino.Geometry.Plane.WorldXY, tolerance) == PointContainment.Inside)
                    {
                        // 输出交点
                        return intersection;
                    }
                    // 判断 line1 和 line2 是否相交
                }
                else if (Intersection.LineLine(lineObj1, lineObj2, out para1, out para2) == true)
                {

                    intersection = lineObj1.PointAt(para1);
                    // 如果相交，则检查交点是否在给定曲线内
                    if (baseCurve.Contains(intersection, Rhino.Geometry.Plane.WorldXY, tolerance) == PointContainment.Inside)
                    {
                        // 输出交点
                        return intersection;

                    }
                    else
                    {
                        Point3d intersection2 = lineObj1.PointAt(para1);

                        Rhino.RhinoApp.WriteLine("交点不在曲线内。");

                        return intersection;
                    }
                }

                else
                {
                    Rhino.RhinoApp.WriteLine("直线无交点。");
                }

            }

            //for (int i = 0; i < events.Count; i++)
            //{
            //    intersectionPoints.Add(events[i].PointA); // or events[i].PointB
            //}
            /*                for (int i = 0; i < offsetedCrv.Count; i++)
                            {
                                for (int j = 0; j < offsetedCrv.Count; j++)
                                {
                                    if (i != j 
                                        && Intersection.CurveCurve(offsetedCrv[i], offs)
                                        offsetedCrv[i].Intersect(offsetedCrv[j], out var intersectionPoint, 0, 0))
                                    {
                                        intersectionPoints.Add(intersectionPoint);
                                    }
                                }
                            }*/
            return Point3d.Unset;
        }

        /// <summary>
        /// 建立原始线与intersection的关系，用来参考后续的图形生成
        /// </summary>
        /// <param name="ptBelongToEdge">一个字典，key是原始边缘，value这条线生成出来的所有的intersection</param>
        /// <param name="curve"></param>
        /// <param name="intersection"></param>
        public static void determineIntersectBelongToCurve(Dictionary<Curve, List<Point3d>> ptBelongToEdge, Curve curve, Point3d intersection)
        {
            ptBelongToEdge[curve].Add(intersection);
        }

        /// <summary>
        /// ***** Intersection的核心，用于生成对应于每条原始边的偏移结果的交点，用于生成分散的矩形地面
        /// </summary>
        /// <param name="curveWithItsOffset1"></param>
        /// <param name="curveWithItsOffset2"></param>
        /// <param name="tolerance"></param>
        /// <param name="curveWithIntersection1">传回第一原始边及其所对应的intersection</param>
        /// <param name="curveWithIntersection2">传回第二原始边及其所对应的intersection</param>
        /// <returns>
        /// bool yes_or_no, 如果交了就传回true
        /// </returns>
        //public static bool GetIntersections(Dictionary<Curve, List<Curve>> curveWithItsOffset1, Dictionary<Curve, List<Curve>> curveWithItsOffset2, Curve landCurve, double tolerance,
        //                                                 out Dictionary<Curve, List<Point3d>> curveWithIntersection1, 
        //                                                 out Dictionary<Curve, List<Point3d>> curveWithIntersection2 )

        public static bool GetIntersections(KeyValuePair<Curve, List<Curve>> curveWithItsOffset1, KeyValuePair<Curve, List<Curve>> curveWithItsOffset2, Curve landCurve, double tolerance,
                                                         out Dictionary<Curve, List<Point3d>> curveWithIntersection1,
                                                         out Dictionary<Curve, List<Point3d>> curveWithIntersection2)
        {
            // 装intersection
            List<Curve> intersections = new List<Curve>();

            List<Point3d> outsideIntersection = new List<Point3d>();
            List<Point3d> insideIntersection = new List<Point3d>();

            Curve originCurve1 = curveWithItsOffset1.Key;
            Curve originCurve2 = curveWithItsOffset2.Key;

            List<Point3d> outsideOffsets = new List<Point3d>();

            // 判断两条原始线是否相交
            bool needIntersection = IsNeedIntersectedOrNot(originCurve1, originCurve2, tolerance);

            // 先IsNeedIntersectedOrNot，判断这两条原始线是否相交
            if (needIntersection == true)
            {
                // 若相交，则GetIntersectionOfOffsetResults，以得到交点
                // 先判断每个字典的list的长度，判断其是不是end类
                // 偏移结果长度为1，属于end类，需要与内外两个都做intersect
                //curveWithItsOffset2.Values.First().Count();

                // ! 可能犀牛的版本不支持这个方法
                var countOfFirstOffset = curveWithItsOffset1.Value?.Count;
                var countOfSecondOffset = curveWithItsOffset2.Value?.Count;

                // 如果第一个是end
                if (countOfFirstOffset == 1)
                {
                    // 如果第二个不是end
                    if (countOfSecondOffset != 1)
                    {
                        // inside 和 outside的offset【可能反了】
                        Curve insideOffset2 = curveWithItsOffset2.Value[0];
                        Curve outsideOffset2 = curveWithItsOffset2.Value[1];

                        Curve endOffset1 = curveWithItsOffset1.Value[0];

                        // end的边与in，out都做intersect
                        insideIntersection.AddRange(GetIntersectionOfOffsetResults(insideOffset2, endOffset1, landCurve, tolerance));
                        outsideIntersection.AddRange(GetIntersectionOfOffsetResults(outsideOffset2, endOffset1, landCurve, tolerance));
                    }
                    // 如果第二个也是end
                    else if (countOfSecondOffset == 1)
                    {
                        // 什么也不做
                    }
                }

                // 如果第一个不是end
                else
                {
                    // 如果第二个不是end
                    if (countOfSecondOffset != 1)
                    {
                        Curve insideOffset1 = curveWithItsOffset1.Value[0];
                        Curve outsideOffset1 = curveWithItsOffset1.Value[1];

                        Curve insideOffset2 = curveWithItsOffset2.Value[0];
                        Curve outsideOffset2 = curveWithItsOffset2.Value[1];

                        // 外与外交，内与内交
                        insideIntersection.AddRange(GetIntersectionOfOffsetResults(insideOffset1, insideOffset2, landCurve, tolerance));
                        outsideIntersection.AddRange(GetIntersectionOfOffsetResults(outsideOffset1, outsideOffset2, landCurve, tolerance));


                    }

                    // 如果第二个是end
                    else if (countOfSecondOffset == 1)
                    {
                        Curve insideOffset1 = curveWithItsOffset1.Value[0];
                        Curve outsideOffset1 = curveWithItsOffset1.Value[1];

                        Curve endOffset2 = curveWithItsOffset2.Value[0];

                        // end的边1与in2，out2都做intersect
                        insideIntersection.AddRange(GetIntersectionOfOffsetResults(insideOffset1, endOffset2, landCurve, tolerance));
                        outsideIntersection.AddRange(GetIntersectionOfOffsetResults(outsideOffset1, endOffset2, landCurve, tolerance));
                    }
                }

            }

            else
            {
                Rhino.RhinoApp.WriteLine("两条原始线不相交。");
                curveWithIntersection1 = new Dictionary<Curve, List<Point3d>>();
                curveWithIntersection2 = new Dictionary<Curve, List<Point3d>>();
                return false;


                //return false;
            }

            // 建立原始线1与intersection的关系字典
            curveWithIntersection1 = new Dictionary<Curve, List<Point3d>>();
            curveWithIntersection1[originCurve1] = new List<Point3d>();
            curveWithIntersection1[originCurve1].AddRange(insideIntersection);
            //curveWithIntersection1[originCurve1] = insideIntersection;
            //curveWithIntersection1.Add(originCurve1, insideIntersection);   
            curveWithIntersection1.Values.First().AddRange(outsideIntersection);

            // 建立原始线2与intersection的关系字典
            curveWithIntersection2 = new Dictionary<Curve, List<Point3d>>();
            curveWithIntersection2[originCurve2] = new List<Point3d>();
            curveWithIntersection2[originCurve2].AddRange(insideIntersection);
            //curveWithIntersection2[originCurve2] = insideIntersection;
            //curveWithIntersection2.Add(originCurve2, insideIntersection);
            curveWithIntersection2.Values.First().AddRange(outsideIntersection);

            // 传回两条线是否有交点的信息
            return needIntersection;
        }


        /// ========================================================= 更小的函数
        /// 1. 判断两条线是否相交
        public static bool IsNeedIntersectedOrNot(Curve curve1, Curve curve2, double tolerance)
        {

            // 仅当originCrv中的两条曲线相交时候（有交点），才将offsetedCrv中的曲线进行相交操作，并传回交点们，布尔表示相交了还是没有

            //double absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            bool yes_or_no = false;

            //List<Point3d> intersectionPoints = new List<Point3d>();

            // 若原始线相交
            if (Rhino.Geometry.Intersect.Intersection.CurveCurve(curve1, curve2, tolerance, tolerance).Count > 0)
            {
                // 而且两条原始线不是同一条线
                if (curve1.Equals(curve2) != true)
                {
                    // 那么偏移结果就得有
                    yes_or_no = true;
                }
            }
            return yes_or_no;
        }

        /// 2. 返回交点 
        /// <summary>
        /// 2. 返回交点,返回两条线的交点
        /// </summary>
        /// <param name="offsectedCurve1"> 偏移出的线1 </param>
        /// <param name="offsectedCurve2"> 偏移出的线2 </param>
        /// <param name="landcurve">地块</param>
        /// <param name="tolerance">宽容度</param>
        /// <returns>List Point3d 装着所有的点</returns>
        public static List<Point3d> GetIntersectionOfOffsetResults(Curve offsectedCurve1, Curve offsectedCurve2, Curve landcurve, double tolerance)
        {
            List<Point3d> resultList = new List<Point3d>();

            // 获取 line1 的起点和终点
            Point3d start1 = offsectedCurve1.PointAtStart;
            Point3d end1 = offsectedCurve1.PointAtEnd;

            // 获取 line2 的起点和终点
            Point3d start2 = offsectedCurve2.PointAtStart;
            Point3d end2 = offsectedCurve2.PointAtEnd;

            // 创建 Line 对象
            Line lineObj1 = new Line(start1, end1);
            Line lineObj2 = new Line(start2, end2);

            Point3d intersection;
            // 先判断作为curve的两条curve是否相交
            var curveCurveIntersection = Intersection.CurveCurve(offsectedCurve1, offsectedCurve2, tolerance, tolerance);

            // line Line intersect用
            double para1;
            double para2;

            // 1. 如果curve本身就有交点，就不用麻烦，直接用
            // 
            if (curveCurveIntersection.Count > 0)
            {
                intersection = curveCurveIntersection[0].PointA;
                // 如果相交，则检查交点是否在给定曲线内,在的话就是它了
                if (landcurve.Contains(intersection, Rhino.Geometry.Plane.WorldXY, tolerance) != PointContainment.Outside)
                {
                    resultList.Add(intersection);
                    // 输出交点
                    return resultList;
                }
                // 判断 line1 和 line2 是否相交
                else
                {
                    // 不做任何事情
                }
            }

            // 2.如果curve本身没有交点，但是Line之间有交点，则得求Line的交点
            else if (Intersection.LineLine(lineObj1, lineObj2, out para1, out para2) == true)
            {
                intersection = lineObj1.PointAt(para1);
                // 如果相交，则检查交点是否在给定曲线内
                if (landcurve.Contains(intersection, Rhino.Geometry.Plane.WorldXY, tolerance) == PointContainment.Inside)
                {
                    resultList.Add(intersection);
                    // 输出交点
                    return resultList;
                }

                // 2.2 交点不在范围里视作没有交点，输出空列表
                else
                {
                    Point3d intersection2 = lineObj1.PointAt(para1);

                    Rhino.RhinoApp.WriteLine("交点不在曲线内。");

                    return resultList; // 空
                }
            }

            // 2.如果line也没有交点，就真没有，输出空列表
            else
            {
                Rhino.RhinoApp.WriteLine("直线无交点。");
                return resultList; // 空
            }
        // 无结果则输出空列表
        return resultList; // 这句话必加，空
        }
    }

}
