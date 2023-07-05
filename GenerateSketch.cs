using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    public static class GenerateSketch
    {
        
        //方法1：生成单个草图
        /*-- 生成sketch的两个核心方法，一个生成单个线对应的块，另一个将块布尔并集 --*/
        public static Curve drawSingleBlockSketch(List<Point3d> pts, double distance, Curve land)
        {
            double absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // 若list不为空，则对于列表进行排序，并连成polyline
            if (pts.Count != 0)
            {
                Point3d[] ptsSort = Point3d.SortAndCullPointList(pts, absulatTolerance);
                Polyline polyline = new Polyline(ptsSort);
                if (polyline.IsClosed == false)
                {
                    polyline.Add(polyline[0]);
                }
                return polyline.ToNurbsCurve();
            }
            else
                return null;

        }


        //方法2：布尔单个草图
        public static Curve BoolSingleBlockSketchsUnion(List<Curve> single_blocks)
        {
            /// <summary>
            /// 将传入的所有组成一个建筑的轮廓进行布尔运算，返回一个建筑的轮廓
            ///
            // 排除未闭合的曲线
            for (int i = 0; i < single_blocks.Count; i++)
            {
                Curve curve = single_blocks[i];
                if (!curve.IsClosed)
                {
                    Point3d startPoint = curve.PointAtStart;
                    Point3d endPoint = curve.PointAtEnd;
                    double t;
                    curve.ClosestPoint(startPoint, out t);
                    Curve newCurve = Curve.CreateControlPointCurve(new Point3d[] { startPoint, endPoint });
                    single_blocks[i] = newCurve;
                }
            }
            // 如果列表里只含有一个元素，则直接返回这个元素并结束函数
            if (single_blocks.Count == 1)
                return single_blocks[0];

            // 对列表中的第一个Curve进行初始化
            Curve unionCurve = single_blocks[0];

            // 对列表中的所有Curve进行布尔并集操作
            for (int i = 1; i < single_blocks.Count; i++)
            {
                Curve curve = single_blocks[i];
                Curve[] result = Curve.CreateBooleanUnion(new Curve[] { unionCurve, curve }, 0.001);

                if (result.Length == 1)
                {
                    unionCurve = result[0];
                }

                else if (result.Length > 1)
                {
                    // 如果并集操作后的结果不止一个Curve，则取出所有curve中闭合且面积最大的那一个，非闭合的和其他的被排除
                    double maxArea = 0;
                    int maxAreaIndex = 0;
                    for (int j = 0; j < result.Length; j++)
                    {
                        if (result[j].IsClosed)
                        {
                            double area = AreaMassProperties.Compute(result[j]).Area;
                            if (area > maxArea)
                            {
                                maxArea = area;
                                maxAreaIndex = j;
                            }
                        }
                    }
                    unionCurve = result[maxAreaIndex];

                }
                else
                    throw new System.Exception("布尔并集操作失败");
            }

            return unionCurve;
        }

    }

}
