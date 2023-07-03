using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Grasshopper.Kernel.Geometry;
using Rhino.Geometry.Intersect;
using static Rhino.Render.ChangeQueue.Light;
using System.Security.Cryptography;
using System.Runtime.Remoting.Messaging;

namespace TianParameterModelForOpt
{
    public class Land
    {
        // 成员变量，包含以下属性：baseCurves, lands, roomDepth, roomWidth, corridorWidth, staircaseWidth, elevatorWidth, buildingSpacing
        public List<Curve> baseCurves;
        public List<Curve> lands;
        public Curve land;
        public Curve baseField;
        public double roomDepth;
        public double roomWidth;
        public double corridorWidth;
        public double staircaseWidth;
        public double elevatorWidth;
        public double buildingSpacing;
        public double absulatTolerance;

        // 构造器，包含以下属性：base, lands, roomDepth, roomWidth, corridorWidth, staircaseWidth, elevatorWidth, buildingSpacing, 都是单个物体，而不是list
        public Land(List<Curve> baseCurves, List<Curve> lands, double roomDepth, double roomWidth, double corridorWidth, double staircaseWidth, double elevatorWidth, double buildingSpacing)
        {
            this.baseCurves = baseCurves;
            this.lands = lands;
            this.land = land;
            this.baseField = baseField;
            this.roomDepth = roomDepth;
            this.roomWidth = roomWidth;
            this.corridorWidth = corridorWidth;
            this.staircaseWidth = staircaseWidth;
            this.elevatorWidth = elevatorWidth;
            this.buildingSpacing = buildingSpacing;
            this.absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        }

        /*-- 生成sketch的两个核心方法，一个生成单个线对应的块，另一个将块布尔并集 --*/
        public Curve drawSingleBlockSketch(List<Point3d> pts, double distance, Curve land)
        {
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
            {
                // 丢出异常
/*                throw new Exception("空列表");
*/
                return null;
            }

        }


        public Curve drawSingleBlockSketch(Point3d pt1, Point3d pt2, double distance, Curve land)
        {
            /// <summary>
            /// 建筑轮廓的最小单元的核心生成逻辑
            /// 

            // 传入两个点，以及一个距离值连接两点，让两个点沿着连接点的两方向垂线方向各自复制一个距离，形成两组复制点。如果其中一组的某个复制的点在不在land之内，则删除这一组，最后只传回一组点
            
            // 1. 生成两个点的连接线
            Line baseLine = new Line(pt1, pt2);
            // 2. 生成两个点的连接线的单位向量
            Vector3d baseLineVec = baseLine.Direction;
            // 3. 生成两个点的连接线的单位向量的垂直向量
            Vector3d baseLineVecPerp = Vector3d.CrossProduct(baseLineVec, Vector3d.ZAxis);
            // 4. 生成两个点的连接线的单位向量的垂直向量的单位向量
            Vector3d baseLineVecPerpUnit = Vector3d.Divide(baseLineVecPerp, baseLineVecPerp.Length);
            // 5. 生成两个点的连接线的单位向量的垂直向量的单位向量的距离
            Vector3d baseLineVecPerpUnitDistance = Vector3d.Multiply(baseLineVecPerpUnit, distance);
            // 6. 生成两个点的连接线的单位向量的垂直向量的单位向量的距离的两个方向的点
            Point3d pt1Perp1 = Point3d.Add(pt1, baseLineVecPerpUnitDistance);
            Point3d pt1Perp2 = Point3d.Subtract(pt1, baseLineVecPerpUnitDistance);

            Point3d pt2Perp1 = Point3d.Add(pt2, baseLineVecPerpUnitDistance);
            Point3d pt2Perp2 = Point3d.Subtract(pt2, baseLineVecPerpUnitDistance);
            // 7. 判断两组复制点是否在land之内
            Point3d[] pts1 = { pt1Perp1, pt1Perp2 };
            Point3d[] pts2 = { pt2Perp1, pt2Perp2 };

            Point3d[] usable = new Point3d[2];

            if (land.Contains(pts1[0], Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside 
                && land.Contains(pts1[1], Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)

            {
                usable = pts1;
            }
            else if (land.Contains(pts2[0], Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside 
                && land.Contains(pts2[1], Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
            {
                usable = pts2;
            }
            else
            {
                usable = pts1;
            }

            // 8. 生成单位矩形
            Point3d[] usableSort = Point3d.SortAndCullPointList(usable, absulatTolerance);
            Polyline usablePolyline = new Polyline(usableSort);

            if(usablePolyline.IsClosed == false)
            {
                usablePolyline.Add(usablePolyline[0]);
            }

            /*Rectangle3d rect = new Rectangle3d;*/
            return usablePolyline.ToNurbsCurve();



        }

        public Curve boolSingleBlockSketchsUnion(List<Curve> single_blocks)
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
                    unionCurve = result[0];
                else
                    throw new System.Exception("布尔并集操作失败");
            }

            // 删除多余的点
            Curve[] splitCurves = unionCurve.Split(0.001);

            // 如果只有一个Curve，则直接返回
            if (splitCurves.Length == 1)
                return splitCurves[0];

            // 否则将所有Curve连接成一个新的Curve
            PolyCurve polyCurve = new PolyCurve();
            foreach (Curve curve in splitCurves)
            {
                if (curve.IsPolyline())
                    polyCurve.Append(curve.ToPolyline());
                else
                    polyCurve.Append(curve);
            }

            return polyCurve;
        }

        /*-----------------------------------------------------------*/

        /*offset行为相关的函数*/
        // TODO 没做完
        public Curve generateSketchFromCurveAccordingToCondition(Curve curve, string condition)
        {
            /// <summary>
            /// 行为分为side和edge两种
            /// side表示曲线是建筑的末端，则只需要偏移一次就行，同时其不需要生成体块
            /// edge表示曲线是建筑的体，需要偏移两次，第一次是楼间距的一半，第二次则是楼的宽度，且需要生成体块

            if condition == "side"
            {
                return offsetCurveInside(curve);
            }
            else if condition == "outside"
            {
                return offsetCurveOutside(curve);
            }
            else if condition == "both"
            {
                return offsetCurveBoth(curve);
            }
            else
            {
                throw new System.Exception("offset行为分类错误");
            }
        }

        public Point3d judgeIfNeedIntersection(List<Curve> originCrv, List<Curve> offsetedCrv, out bool yes_or_no)
        {
            ///<summary>
            /// 仅当originCrv中的两条曲线相交时候（有交点），才将offsetedCrv中的曲线进行相交操作，并传回交点们，布尔表示相交了还是没有
            /// </summary>

            // 仅当originCrv中的两条曲线相交时候（有交点），才将offsetedCrv中的曲线进行相交操作，并传回交点们，布尔表示相交了还是没有
            yes_or_no = false;

            List<Point3d> intersectionPoints = new List<Point3d>();

            for (int i = 0; i < originCrv.Count; i++)
            {
                for (int j = 0; j < originCrv.Count; j++)
                {
/*                    if (i != j && originCrv[i].Intersect(offsetedCrv[j], out var intersectionPoint, 0, 0))
*/                  if (i != j 
                        && Intersection.CurveCurve(originCrv[i], originCrv[j], absulatTolerance, absulatTolerance).Count > 0)
                    {
                        yes_or_no = true;
                    }
                }
            }

            if (yes_or_no == true)
            {
                // !将offsetedCrv中的曲线进行相交操作，并传回交点们（不使用循环，仅处理两条边）
                //CurveIntersections events = Intersection.CurveCurve(offsetedCrv[0], offsetedCrv[1], absulatTolerance, absulatTolerance);

                // 获取 line1 的起点和终点
                Point3d start1 = offsetedCrv[0].PointAtStart;
                Point3d end1 = offsetedCrv[0].PointAtEnd;

                // 获取 line2 的起点和终点
                Point3d start2 = offsetedCrv[1].PointAtStart;
                Point3d end2 = offsetedCrv[1].PointAtEnd;

                // 创建 Line 对象
                Line lineObj1 = new Line(start1, end1);
                Line lineObj2 = new Line(start2, end2);

                Point3d intersection;
                // 先判断作为curve的两条curve是否相交
                var curveCurveIntersection = Intersection.CurveCurve(offsetedCrv[0], offsetedCrv[1], absulatTolerance, absulatTolerance);

                // line Line intersect用
                double para1;
                double para2;

                if (curveCurveIntersection.Count > 0)
                {
                    intersection = curveCurveIntersection[0].PointA;
                    // 如果相交，则检查交点是否在给定曲线内
                    if (baseField.Contains(intersection, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                    {
                        // 输出交点
                        return intersection;

                        //Rhino.RhinoApp.WriteLine("交点坐标为：" + intersection.ToString());
                    }
                    //else
                    //{
                    //    Point3d intersection2 = lineObj1.PointAt(para1);

                    //    Rhino.RhinoApp.WriteLine("交点不在曲线内。");
                    //}

                    // 判断 line1 和 line2 是否相交
                }
                else if (Intersection.LineLine(lineObj1, lineObj2, out para1, out para2) == true)
                {

                    intersection = lineObj1.PointAt(para1);
                    // 如果相交，则检查交点是否在给定曲线内
                    if (baseField.Contains(intersection, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                    {
                        // 输出交点
                        return intersection;

                        //Rhino.RhinoApp.WriteLine("交点坐标为：" + intersection.ToString());
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

        public void intersectBelongToCurve(Dictionary<Curve, List<Point3d>> ptBelongToEdge, Curve curve, Point3d intersection)
        /*public Dictionary<Curve, List<Point3d>> buildRelationship()*/
        {
            /// <summary>
            /// 用于确定点属于哪条边，一条边一半又两个点，用来生成矩形底面
            /// </summary>

            ptBelongToEdge[curve].Add(intersection);
        }    

        public offsetSideCurve(Curve sideCurve, condition)
        {

        }
    
    
    }




        public double getBuildingDepth()
        {
            // 函数说明
            // 输入：无
            // 输出：buildingDepth，建筑深度
            // 功能：根据房间深度，走廊宽度, 计算建筑深度

            double buildingDepth = roomDepth + corridorWidth;

            return buildingDepth;
        }

        public Point3d[] getEdgesCentPt(Curve land)
        {
            // 说明
            // 输入：land，一块地块
            // 输出：edgeCtPts，地块边界中点数组
            // 功能：根据地块，返回地块边界中点

            Curve[] landCurves = land.DuplicateSegments();
            Point3d[] edgeCtPts = new Point3d[landCurves.Length];

            for (int i = 0; i < landCurves.Length; i++)
            {
                edgeCtPts[i] = landCurves[i].PointAtNormalizedLength(0.5);
            }

            return edgeCtPts;
        }

        public Point3d getCurveDirections(Curve curve)
        {
            // 函数说明
            // 输入：curve，一条曲线，包围盒
            // 输出：offsetPt，曲线中点的偏移点
            // 功能：根据曲线的中点，判断曲线的方向，返回偏移点


            //Curve land；
            Point3d offsetPt = curve.PointAtNormalizedLength(0.5);

            Curve verticalLine = curve.DuplicateCurve();
            verticalLine.Rotate(RhinoMath.ToRadians(90), Vector3d.ZAxis, curve.PointAtNormalizedLength(0.5));

            Point3d[] endPts = { verticalLine.PointAtStart, verticalLine.PointAtEnd };

            offsetPt = endPts[0];

            if (endPts.Length == 2)
            {
                Curve offsetCurve = curve.Offset(Plane.WorldXY, 0.01, 0.001, CurveOffsetCornerStyle.Sharp)[0];

                offsetCurve.DivideByCount(6, true, out Point3d[] judgePts);

                int count = 0;

                foreach (Point3d pt in judgePts)
                {
                    if (curve.Contains(pt, Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                    {
                        count++;
                    }
                }

                if (count >= 3)
                {
                    offsetPt = endPts[1];
                }
                else
                {
                    offsetPt = endPts[0];
                }
            }

            return offsetPt;
        }



        public Polyline SortPointList(Curve land, List<Point3d> rmovDuplicatePtList)
        {
            // 函数说明
            // 输入：land，一块地块，rmovDuplicatePtList，去重后的点列表
            // 输出：sortedPoints，排序后的点列表
            // 功能：根据地块的中心点，对点列表进行排序

            Point3d centroid = land.GetBoundingBox(true).Center;

            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<(double, Point3d)> sortable = new List<(double, Point3d)>();

            foreach (Point3d pt in rmovDuplicatePtList)
            {
                //！！ 计算可能不达预期
                double angle = Vector3d.VectorAngle(centroid - pt, Vector3d.XAxis, Plane.WorldXY);

                if (angle < 0) angle += 360; // correct for negative angles
                sortable.Add((angle, pt));
            }

            List<Point3d> sortedPoints = sortable.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();

            return new Polyline(sortedPoints);
        }

        public Polyline DivideDirectionZone(Curve baseCurve, string part = "north")
        {
            // 函数说明
            // 输入：baseCurve，一条曲线，part，分区
            // 输出：directionZone，方向区域
            // 功能：根据曲线，以及要输出的分区，返回方向区域

            // 计算多边形区域的边界框
            BoundingBox boundingBox = baseCurve.GetBoundingBox(true);
            // 获取边界框的角点
            Point3d[] boundingBoxCorners = boundingBox.GetCorners();

            // ! 删除重复的点，并将第一个点添加到列表的末尾，以便在后面的步骤中将多边形闭合(有可能排除不了相似的点)
            Point3d[] boundingBoxCornersNoDuplicate = boundingBoxCorners.Distinct().ToArray();
            List<Point3d> boundingBoxCornersList = new List<Point3d>(boundingBoxCornersNoDuplicate);
            // 加回第一个点
            boundingBoxCornersList.Add(boundingBoxCornersNoDuplicate[0]);

            // 创建一个多段线，该多段线由边界框中的点组成
            Polyline boundingBoxPolyline = new Polyline(boundingBoxCornersList);
            // 将多段线的边缘分配到四个方向（北、南、东、西）中的每一个
            Dictionary<string, Curve> directionsDict = DispatchEdgesThroughDirection(boundingBoxPolyline);
            // 计算每个方向的中心点和端点，并将它们存储在相应的列表中
            Point3d[] centPts = new Point3d[4];
            centPts[0] = directionsDict["north"].PointAtNormalizedLength(0.5);
            centPts[1] = directionsDict["south"].PointAtNormalizedLength(0.5);
            centPts[2] = directionsDict["east"].PointAtNormalizedLength(0.5);
            centPts[3] = directionsDict["west"].PointAtNormalizedLength(0.5);
            Point3d[] endPts = new Point3d[4];
            endPts[0] = directionsDict["north"].PointAtEnd;
            endPts[1] = directionsDict["south"].PointAtEnd;
            endPts[2] = directionsDict["east"].PointAtEnd;
            endPts[3] = directionsDict["west"].PointAtEnd;
            // 创建一个字典，其中包含四个方向的部分的点的列表
            Dictionary<string, List<Point3d>> ptDic = new Dictionary<string, List<Point3d>>();
            ptDic.Add("north_part", new List<Point3d>());
            ptDic.Add("south_part", new List<Point3d>());
            ptDic.Add("east_part", new List<Point3d>());
            ptDic.Add("west_part", new List<Point3d>());

            // !可能需要改为GH 的plane
            Rhino.Geometry.Plane plane = Rhino.Geometry.Plane.WorldXY;
            // 确定每个端点属于哪个方向的部分，并将其添加到相应的列表中
            foreach (Point3d pt in endPts)
            {
                if ((directionsDict["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)

                    || (directionsDict["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["east_part"].Add(pt);
                }
                if ((directionsDict["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)
                    || (directionsDict["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["west_part"].Add(pt);
                }
                if ((directionsDict["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)
                    || (directionsDict["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["north_part"].Add(pt);
                }
                if ((directionsDict["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)
                    || (directionsDict["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsDict["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["south_part"].Add(pt);
                }
            }
            // 对每个部分的点进行排序，并使用这些点创建多段线
            // ! 排序功能存疑
            List<Point3d> northPartPts = ptDic["north_part"].Concat(new Point3d[] { centPts[2], centPts[3] }).ToList();
            northPartPts.Add(northPartPts[0]);

            List<Point3d> southPartPts = ptDic["south_part"].Concat(new Point3d[] { centPts[2], centPts[3] }).ToList();
            southPartPts.Add(southPartPts[0]);

            List<Point3d> eastPartPts = ptDic["east_part"].Concat(new Point3d[] { centPts[0], centPts[1] }).ToList();
            eastPartPts.Add(eastPartPts[0]);

            List<Point3d> westPartPts = ptDic["west_part"].Concat(new Point3d[] { centPts[0], centPts[1] }).ToList();
            westPartPts.Add(westPartPts[0]);

            Polyline northPartPolyline = new Polyline(northPartPts);
            Polyline southPartPolyline = new Polyline(southPartPts);
            Polyline eastPartPolyline = new Polyline(eastPartPts);
            Polyline westPartPolyline = new Polyline(westPartPts);

            // 根据part参数返回所选部分的多段线
            if (part == "north")
            {
                return northPartPolyline;
            }
            else if (part == "south")
            {
                return southPartPolyline;
            }
            else if (part == "east")
            {
                return eastPartPolyline;
            }
            else if (part == "west")
            {
                return westPartPolyline;
            }
            else
            {
                return null;
            }
        }
    }


}










