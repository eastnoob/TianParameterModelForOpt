using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt._4_repair
{
    internal static class Find
    {
        public static Brep FindTheLargestBrep(Brep[] multipleBreps)
        {
            // 判断最大的
            Brep maxVolumeBrep = null;
            double maxVolume = 0.0;

            foreach (Brep brep in multipleBreps)
            {
                //VolumeMassProperties vmp = VolumeMassProperties.Compute(brep);
                //double volume = vmp.Volume;
                double volume = brep.GetVolume();

                if (volume > maxVolume)
                {
                    maxVolume = volume;
                    maxVolumeBrep = brep;
                }
            }
            //if (maxVolumeBrep != null)
            //    maxVolumeBrep = multipleBreps[0];
            return maxVolumeBrep;
        }


        public static Curve FindTheLargestCurve(Curve[] multipleCurves)
        {
            // 判断最大的
            Curve maxLengthCurve = null;
            double maxVolume = 0.0;

            foreach (Curve curve in multipleCurves)
            {
                //VolumeMassProperties vmp = VolumeMassProperties.Compute(brep);
                //double volume = vmp.Volume;
                double volume = curve.GetLength();

                if (volume > maxVolume)
                {
                    maxVolume = volume;
                    maxLengthCurve = curve;
                }
            }
            //if (maxVolumeBrep != null)
            //    maxVolumeBrep = multipleBreps[0];
            return maxLengthCurve;
        }

        public static Curve FindTheShortestCurve(List<Curve> curvesList)
        {
            double shortestLength = double.MaxValue;
            Curve shortestCurve = null;

            foreach (Curve curve in curvesList)
            {
                double length = curve.GetLength();

                if (length < shortestLength)
                {
                    shortestLength = length;
                    shortestCurve = curve;
                }
            }
            return shortestCurve;
        }

        public static bool CheckOverlap(Curve curve1, Curve curve2)
        {
            // 设置相交计算的容忍度
            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // 计算曲线的交集
            CurveIntersections intersections = Intersection.CurveCurve(curve1, curve2, tolerance, tolerance);

            // 检查交集是否存在
            if (intersections != null && intersections.Count > 0)
            {
                foreach (IntersectionEvent ie in intersections)
                {
                    // 如果交集的类型是区间重叠，那么两条线部分重合
                    if (ie.IsOverlap)
                    {
                        RhinoApp.WriteLine("The lines overlap");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
            // 如果没有找到重叠，那么两条线不重叠
            RhinoApp.WriteLine("The lines do not overlap");
        }
    }
}
