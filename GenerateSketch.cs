using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    public static class GenerateSketch
    {


        /// <summary>
        /// 方法1：生成单个草图
        /// </summary>
        /// <param name="pts"> 一条边偏移并相交之后获得的所有点</param>
        /// <param name="distance"> </param>
        /// <param name="land"></param>
        /// <returns></returns>
        public static Curve DrawSingleBlockSketch(List<Point3d> pts/*, double distance, Curve land*/)
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


        /// <summary>
        /// 方法2：布尔单个草图，形成一个整体的建筑轮廓
        /// </summary>
        /// <param name="single_blocks">装有一个land中所有边生成的单个图形的List</param>
        /// <returns>一个经过布尔后的图形，就是这个边的建筑轮廓</returns>
        /// <exception cref="System.Exception"></exception>
        public static Curve BoolSingleBlockSketchsUnion(List<Curve> single_blocks)
        {

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



        /*---------------------------------------------------核心方法----------------------------------------------------------*/
        /// <summary>
        /// 判断每一个land方向应该如何生成图
        /// </summary>
        /// <param name="condition"> land的building type，由JudgeGenerateBehaviour生成</param>
        /// <param name="boundageDirections"> 本land中所有处于边缘的方向集合 </param>
        /// <returns>一个direction，key是东西南北，value是edge或者end，以及带不带boundage</returns>

        public static Dictionary<string, string> DetermineLandcurvesOffsetBehaviours(List<string> condition, List<string> boundageDirections)
        {
            // 边分为boundage和非boundage，二者之间要进行区别
            // land具有多个属性，需要根据每个属性决定各个边的行为
            // 在condition中的方向的所有边采取edge的偏移方式，不在的则采用end的偏移方式

            // 四个方向和键，和四个空的字符串
            Dictionary<string, string> edgeProcessCondition = new Dictionary<string, string>()
            {
                { "north", "" },
                { "south", "" },
                { "east", ""},
                { "west", ""}
            };

            /*------------------------------先根据condition判断四边的行为------------------------------------*/

            foreach (string direction in edgeProcessCondition.Keys)
            {
                //Curve emptyCurve = new Polyline().ToNurbsCurve();
                //emptyCurve.UserData.Add("Identifier", 1);

                // 先判断是不是NO
                if (condition.Contains("NO"))
                    // 证明这个不生成
                    return edgeProcessCondition;

                // 如果生成了，那么所有condition中的方向的边都是edge，所有不在condition中的方向的边都是end
                else if (condition.Contains(direction))
                    edgeProcessCondition[direction] += "edge";

                else
                    edgeProcessCondition[direction] += "end";

                /*------------------------------再判断是不是boudage------------------------------------*/

                // 如果不是NO，则需要判断其是否在boundage里面,如果在，则获得标记
                if (boundageDirections.Contains(direction))
                    edgeProcessCondition[direction] += "-boundage";
            }

            return edgeProcessCondition;
        
        }

        // 生成图形


    }

}
