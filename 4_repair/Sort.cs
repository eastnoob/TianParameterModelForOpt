using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt._4_repair
{
    internal static class Sort
    {
        //public static bool IsSelfIntersecting(Curve curve)
        //{
        //    double t = curve.Domain.Min;
        //    double max = curve.Domain.Max;
        //    double s;
        //    while (t != RhinoMath.UnsetValue)
        //    {
        //        Rhino.Geometry.Continuity c = Rhino.Geometry.Continuity.C1_locus_continuous;

        //        t = curve.GetNextDiscontinuity(Continuity.C1_locus_continuous, t, curve.Domain.Max, out s);
        //        t = curve.GetNextDiscontinuity(c, t, max, out s);

        //        if (t != RhinoMath.UnsetValue)
        //        {
        //            if (s > t)
        //            {
        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}



        public static List<Point3d> SortPoints(List<Point3d> points, List<Point3d> refPoints)
        {
            // 存储已参照的Point3d索引和距离
            var usedPoints = new Dictionary<int, double>();

            // 为每个refPoint找寻最近的points，按距离排序
            foreach (var rp in refPoints)
            {
                var pointsDist = points.Select(p => new
                {
                    Index = points.IndexOf(p),
                    Distance = p.DistanceTo(rp),
                })
                .OrderBy(p => p.Distance);

                // 处理每个refPoint的结果
                foreach (var pd in pointsDist)
                {
                    if (!usedPoints.ContainsKey(pd.Index))
                    {
                        usedPoints.Add(pd.Index, pd.Distance);
                        break;
                    }
                }
            }

            // 将结果保存到新的列表中
            var newPoints = new List<Point3d>();
            foreach (var up in usedPoints.OrderBy(p => p.Value))
            {
                newPoints.Add(points[up.Key]);
            }

            return newPoints;
        }
    }
}
