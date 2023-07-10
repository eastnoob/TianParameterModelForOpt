using Eto.Forms;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    public static class MyMethods
    {


        // 构造器
        private static readonly double absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

        public static List<Point3d> GrahamScan(List<Point3d> points)
        {
            /// <summary>
            /// 点的排序算法

            // 如果点的数量小于 3，直接返回
            if (points.Count < 3)
                return points;

            // 找到最下面的点，并将其放在列表的第一个位置
            int minIndex = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].Y < points[minIndex].Y || (points[i].Y == points[minIndex].Y && points[i].X < points[minIndex].X))
                    minIndex = i;
            }
            Point3d temp = points[0];
            points[0] = points[minIndex];
            points[minIndex] = temp;

            // 根据极角排序点
            Point3d pivot = points[0];
            points.Sort((a, b) => GetAngle(pivot, a).CompareTo(GetAngle(pivot, b)));

            // 执行 Graham 扫描算法
            List<Point3d> hull = new List<Point3d>();
            hull.Add(points[0]);
            hull.Add(points[1]);
            for (int i = 2; i < points.Count; i++)
            {
                while (hull.Count >= 2 && Orientation(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            return hull;
        }

        private static double GetAngle(Point3d a, Point3d b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Atan2(dy, dx);
        }

        private static int Orientation(Point3d p, Point3d q, Point3d r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (val == 0)
                return 0;
            return (val > 0) ? 1 : 2;
        }

        /*以下是处理四种建筑形态的方法*/

        public static Tuple<List<Tuple<Point3d, Point3d>>, List<Tuple<Point3d, Point3d>>> ProcessB(Dictionary<Tuple<Point3d, Point3d>, double> origns_lengths, List<Tuple<Point3d, Point3d>> first_out_spliters_twin, List<Tuple<Point3d, Point3d>> first_inner_spliters_twin, string condition = "short")
        {
            /// <summary>
            /// 
            /// </summary>

            List<Tuple<Point3d, Point3d>> out_spliters_twin = new List<Tuple<Point3d, Point3d>>();
            List<Tuple<Point3d, Point3d>> inner_spliters_twin = new List<Tuple<Point3d, Point3d>>();

            if (condition == "short")
            {
                List<string> min_dir = new List<string>();

                double min_length = origns_lengths.Values.Min();
                foreach (var orign in origns_lengths)
                {
                    if (orign.Value == min_length)
                    {
                        foreach (var dir_with_edges in relationship_dic)
                        {
                            if (dir_with_edges.Value.Contains(orign.Key.Item1) || dir_with_edges.Value.Contains(orign.Key.Item2))
                            {
                                min_dir.Add(dir_with_edges.Key);
                            }
                        }
                    }
                }

                List<double> len_of_short = new List<double>();
                foreach (var direction in min_dir)
                {
                    List<double> len_of_single_short = new List<double>();
                    foreach (var edge in relationship_dic[direction])
                    {
                        len_of_single_short.Add(origns_lengths[new Tuple<Point3d, Point3d>(edge, edge)]);
                    }
                    len_of_short.AddRange(len_of_single_short);
                }

                double max_len = len_of_short.Max();
                Tuple<Point3d, Point3d> longest_edge = null;
                foreach (var orign in origns_lengths)
                {
                    if (orign.Value == max_len)
                    {
                        longest_edge = orign.Key;
                        break;
                    }
                }

                List<Tuple<Point3d, Point3d>> abandoned_origns = new List<Tuple<Point3d, Point3d>>();
                foreach (var direction in min_dir)
                {
                    if (relationship_dic[direction].Contains(longest_edge.Item1) || relationship_dic[direction].Contains(longest_edge.Item2))
                    {
                        foreach (var edge in relationship_dic[direction])
                        {
                            if (edge != longest_edge.Item1 && edge != longest_edge.Item2)
                            {
                                abandoned_origns.Add(new Tuple<Point3d, Point3d>(edge, edge));
                            }
                        }
                    }
                }

                List<Tuple<Point3d, Point3d>> abandoned_edges = new List<Tuple<Point3d, Point3d>>();
                foreach (var orign in abandoned_origns)
                {
                    abandoned_edges.AddRange(offsted[orign]);
                }

                foreach (var pair in first_out_spliters_twin.Concat(first_inner_spliters_twin))
                {
                    if (abandoned_edges.Contains(pair.Item1) || abandoned_edges.Contains(pair.Item2))
                    {
                        continue;
                    }
                    else
                    {
                        if (first_out_spliters_twin.Contains(pair))
                        {
                            out_spliters_twin.Add(pair);
                        }
                        else if (first_inner_spliters_twin.Contains(pair))
                        {
                            inner_spliters_twin.Add(pair);
                        }
                    }
                }

                return Tuple.Create(out_spliters_twin, inner_spliters_twin);
            }
            else if (condition == "general")
            {
                foreach (var pair in first_out_spliters_twin.Concat(first_inner_spliters_twin))
                {
                    foreach (var edge in offset_with_orign.Keys)
                    {
                        if (offseted_and_beused.Contains(pair.Item1) && offseted_and_beused.Contains(pair.Item2))
                        {
                            if (CurveCurveIntersection(offset_with_orign[pair.Item1], offset_with_orign[pair.Item2], tolerance: tol) != null)
                            {
                                if (first_out_spliters_twin.Contains(pair))
                                {
                                    out_spliters_twin.Add(pair);
                                }
                                else if (first_inner_spliters_twin.Contains(pair))
                                {
                                    inner_spliters_twin.Add(pair);
                                }
                            }
                        }
                    }
                }

                return Tuple.Create(out_spliters_twin, inner_spliters_twin);
            }
            else
            {
                throw new ArgumentException("Invalid condition value.");
            }
        }


        // 判断两条线有没有延长线上的交点的方法
        public static void CheckIntersection(Curve line1, Curve line2, Curve land)
        {
            // 获取 line1 的起点和终点
            Point3d start1 = line1.PointAtStart;
            Point3d end1 = line1.PointAtEnd;

            // 获取 line2 的起点和终点
            Point3d start2 = line2.PointAtStart;
            Point3d end2 = line2.PointAtEnd;

            // 创建 Line 对象
            Line lineObj1 = new Line(start1, end1);
            Line lineObj2 = new Line(start2, end2);

            // 判断 line1 和 line2 是否相交
            double para1;
            double para2;
            Point3d intersection;

            if (Intersection.LineLine(lineObj1, lineObj2, out para1, out para2))
            {
                // 计算交点坐标，让其成为一点物体
                intersection = line1.PointAt(para1);
                // 如果相交，则检查交点是否在给定曲线内
                if (land.Contains(intersection))
                {
                    // 输出交点
                    Rhino.RhinoApp.WriteLine("交点坐标为：" + intersection.ToString());
                }
                else
                {
                    Rhino.RhinoApp.WriteLine("交点不在曲线内。");
                }
            }
            else
            {
                Rhino.RhinoApp.WriteLine("直线无交点。");
            }
        }

        // 让曲线朝着land内部垂直偏移

        public static Curve OffsetCurveAlongDirection(Curve curve, Curve land, double distance)

        {
            // 计算封闭曲线的质心
            Point3d centroid = land.GetBoundingBox(false).Center;

            // 计算曲线的方向向量
            Vector3d direction = centroid - curve.PointAtStart;

/*            // 设置曲线的偏移方向
            Vector3d curveOffsetDirection = direction;*/

            // 偏移曲线
            Curve offsetCurve = curve.Offset(centroid, Plane.WorldXY.Normal, distance, absulatTolerance, CurveOffsetCornerStyle.Sharp)[0];

            return offsetCurve;
        }


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
                Curve offsetCurve = curve.Offset(endPoints[0],Rhino.Geometry.Plane.WorldXY.Normal, 0.01, absulatTolerance, CurveOffsetCornerStyle.Sharp)[0];
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
