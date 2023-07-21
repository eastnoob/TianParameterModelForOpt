using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace TianParameterModelForOpt
{
    internal static class Draw
    {

        /// <summary>
        /// 方法1：生成单个草图
        /// </summary>
        /// <param name="pts"> 一条边偏移并相交之后获得的所有点</param>
        /// <param name="distance"> </param>
        /// <param name="land"></param>
        /// <returns></returns>
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





        /// <summary>
        /// 根据输入的condition，处理单个的曲线edge，返回偏移后的边缘
        /// </summary>
        /// <param name="edge"> 要被处理的线</param>
        /// <param name="landCurve">地块曲线（封闭）</param>
        /// <param name="buildingDepth">建筑深度</param>
        /// <param name="buildingSpacing">建筑间距(不用除以2)</param>
        /// <param name="buildingLandSpacing">建筑</param>
        /// <param name="condition">边的处理方式，默认为edge<br/>
        /// · edge：建筑实体边缘<br/>
        /// · edge-boundage：boundage位的建筑实体边缘<br/>
        /// · end：建筑末端边缘<br/>
        /// · end-boundage：boundage位的建筑末端边缘<br/>
        /// </param>
        /// <returns>一个list，装有处理好了的边 <br/>
        /// 1. edge类：两条<br/>
        /// 2. boundage类：一条<br/>
        /// </returns>
        public static List<Curve> EdgeProcessor(Curve edge, Curve landCurve, double buildingDepth, double buildingSpacing, string condition = "edge")
        {
            // double buildingLandSpacing, 
            // 储存偏移后的边缘
            double realBuildingSpacingPara = buildingSpacing/2;
            List<Curve> spliters = new List<Curve>();

            /*  ------------------------------------------------ EDGE --------------------------------------------------------*/

            if (condition == "edge") // edge，普通非boundage边缘，偏移两次，形成体块的主体
            {

                spliters.AddRange(Offset.offsetSideCurve(edge, landCurve, buildingSpacing, buildingDepth));

            }
            /*--------------------------------------------- Boundage ------------------------------------------------------*/

            else if (condition == "edge-boundage") // boundage，boundage边缘，偏移一次，第一次与边缘重合，第二次便宜建筑深度，形成体块的主体
            {

                spliters.AddRange(Offset.offsetSideCurve(edge, landCurve, 0, buildingDepth));

            }

            /*----------------------------------------------END BOUNDAGE--------------------------------------------------*/

            else if (condition == "end_boundage") // boundage，boundage边缘，不偏移，与边缘重合
            {
                // 不偏移
                spliters.Add(edge);
            }

            /*----------------------------------------------END BOUNDING-------------------------------------------------*/

            // ? 想不起来了，暂时搁置

            //else if (condition == "end_bounding")
            //{
            //    // 处理边缘在边界末端且边界将被用作末端
            //    if (land != null)
            //    {
            //        List<Curve> boundingLinesDirections = CreateBoundingLinesDirections(land);
            //        Curve outsideSpliters = boundingLinesDirections[direction] as Curve;
            //        Curve[] insideSpliters = outsideSpliters.Offset(directionPt[0], spacing, RhinoMath.ZeroTolerance, CurveOffsetCornerStyle.Sharp);

            //        spliters.Add(insideSpliters[0]);
            //    }
            //}

            /*----------------------------------------------END SPACING-------------------------------------------------*/

            else if (condition == "end") // end_spacing，endb边缘，但是end不在boundage上，偏移一次
            {
                spliters.AddRange(Offset.offsetEndCurve(edge, landCurve, buildingSpacing));
            }

            /*----------------------------------------------END SELF SPACING-------------------------------------------------*/

            else if (condition == "end_selfspacing")// end_self_spacing，偏移2次但是只偏移一个spacing的距离，功能忘记，先定义出来
            {

                spliters.AddRange(Offset.offsetSideCurve(edge, landCurve, 0, buildingDepth));
            }

            return spliters;
        }
    
    
        // ****** 核心

        public static Curve DrawSketchOfABuilding(Land land/*, Dictionary<string, string> edgeProcessCondition, Dictionary<string, List<Curve>> directionWithEdges*/)
        {
            /*--------------------------------------------材料--------------------------------------------*/
            bool boundageOrNot = land.boundageOrNot;


            // 在bondage和不在boundage的原始边
            Dictionary<string, List<Curve>> onBoundage  = land.onBoundage;
            Dictionary<string, List<Curve>> notOnBoundage = land.notOnBoundage;

            // 被分好方向的原始边缘
            Dictionary<string, List<Curve>> dispatchedEdges = land.dispatchedEdges;

            // land本身
            Curve landCurve = land.landCurve;

            // 数值
            double buildingDepth = land.GetBuildingDepth();
            double buildingSpacing = land.buildingSpacing;
            //double shortestEndDepth = land.GetShortestEndDepth();


            //东西南北
            string ew = land.isWestOrEast;
            string ns = land.isNorthOrSouth;

            //List<Curve>JudgeGenerateBehaviour.JudgeTheLandCondition(buildingSpacing, buildingDepth, landCurve, edgeProcessCondition, directionWithEdges);
            List<Curve> sketchOfSingleEdge = new List<Curve>();

            /// *****最后要传出的这个
            Curve sketchOfABuilding = null;

            /*----------------------------------------以下是方法----------------------------------------------*/



            /*-------------------------------------1. 先处理Edge的偏移 -----------------------------------------*/

            List<string> buildingTypeOfThisLandCurve = land.buildingTypeOfThisLandCurve;

            // ***** 建立原有的的边与偏移结果的联系
            Dictionary<Curve, List<Curve>> curveWithOffsetedResults = new Dictionary<Curve, List<Curve>>();

            // 获得四边的condition
            Dictionary<string, string> offsetBehavioursOfLandcurves = JudgeGenerateBehaviour.DetermineLandcurvesOffsetBehaviours(buildingTypeOfThisLandCurve, land.boundageDirections);


            // 遍历每一个方向
            foreach (string direction in offsetBehavioursOfLandcurves.Keys)
            {
                // 遍历每一个方向的所有原始边
                foreach (Curve originalEdge in dispatchedEdges[direction])
                {
                    // 对于原始边进行处理，得到单个的原始边的偏移后的边
                    List<Curve> offsetedEdge = EdgeProcessor(originalEdge, landCurve, buildingDepth, buildingSpacing, offsetBehavioursOfLandcurves[direction]);
                    curveWithOffsetedResults[originalEdge] = offsetedEdge;
                }


                // 开始处理intersection
            }

            /*-------------------------------------2. 再处理intersection -----------------------------------------*/
            // ******* 用来装Curve和它的intersection
            Dictionary<Curve, List<Point3d>> curveWithIntersections = new Dictionary<Curve, List<Point3d>>();

            foreach (Curve curve in curveWithOffsetedResults.Keys)
            {
                curveWithIntersections[curve] = new List<Point3d>();
            }


            // 用这个排除重复出现的组合，正确的组合应该是12，13，14，23，24，34这样
            //List< HashSet<Curve> > usedCurvePairs = new List<HashSet<Curve>>();

            // ## 这里可以用for循环，通过监督index的方式确定是否被重复使用了

            // 运算每一对边的intersection

            // 尝试不要使用foreach，使用for循环，通过监督index的方式确定是否被重复使用了
            //foreach (KeyValuePair<Curve, List<Curve>> pair1 in curveWithOffsetedResults)
            //{
            //    foreach(KeyValuePair<Curve, List<Curve>> pair2 in curveWithOffsetedResults)
            //    {

            List<int[]> usedCurveIndexPairs = new List<int[]>();

            for (int i = 0; i < curveWithOffsetedResults.Count; i++)
            {
                for (int j = 0; j < curveWithOffsetedResults.Count; j++)
                {
                    KeyValuePair<Curve, List<Curve>>  pair1 = curveWithOffsetedResults.ElementAt(i);
                    KeyValuePair<Curve, List<Curve>>  pair2 = curveWithOffsetedResults.ElementAt(j);

                    // 用于判断是否被使用过
                    bool isBeUsed = false;

                    int[] usedCurveIndexPair = { i, j };

                    // 不要自交
                    if (i != j || pair1.Equals(pair2) == false || pair1.Key != pair2.Key)

                    {
                        //// 有可能不需要转换成Dictionary
                        //// 配合参数数据类型
                        //Dictionary<Curve, List<Curve>>temporaryPair1 = new Dictionary<Curve, List<Curve>>();
                        //temporaryPair1.Add(pair1.Key, pair1.Value);
                        //Dictionary<Curve, List<Curve>>temporaryPair2 = new Dictionary<Curve, List<Curve>>();
                        //temporaryPair2.Add(pair2.Key, pair2.Value);

                        // 用来装intersection的点的临时的参数
                        Dictionary<Curve, List<Point3d>> curveWithIntersection1 = new Dictionary<Curve, List<Point3d>>();
                        Dictionary<Curve, List<Point3d>> curveWithIntersection2 = new Dictionary<Curve, List<Point3d>>();


                        // 1 先确定是不是计算过了，计算过的对久不用计算了

                        foreach (var arrayOfPair in usedCurveIndexPairs)
                        {
                            // 两个都出现过，证明已经运算过了，这样就没必要再算一次
                            if (arrayOfPair.Contains(i) && arrayOfPair.Contains(j))
                            {
                                Console.WriteLine("出现过一次了，不要重复");
                                isBeUsed = true;
                                break;
                            }
                            else
                            {
                                // 证明这一段没有运算过，可以通过
                                // 不做任何事情，继续循环
                            }
                        }

                        // 经过以上步骤，证明这一对没有被运算过，那么就可以运算了
                        if (isBeUsed == false)
                        {
                        // 2. 进行相交运算

                            bool isIntersection = false;

                            // curveWithIntersection1 是 i 线段的交点结果
                            // curveWithIntersection2 是 j 线段的交点结果
                            // 注意此时间curveWithIntersection1 中应该只有一个键
                            isIntersection = Intersect.GetIntersections(pair1, pair2, landCurve, 0.001,
                                                                    out curveWithIntersection1,
                                                                    out curveWithIntersection2);

                            // 3. 如果有交点，那么就要进行处理
                            if (isIntersection == true)
                            {
                                // 加入到对应的边缘当中
                                curveWithIntersections[curveWithIntersection1.Keys.First()].AddRange(curveWithIntersection1[curveWithIntersection1.Keys.First()]);
                                curveWithIntersections[curveWithIntersection2.Keys.First()].AddRange(curveWithIntersection2[curveWithIntersection2.Keys.First()]);

                                usedCurveIndexPairs.Add(usedCurveIndexPair);
                            }


                        }

                        else
                            usedCurveIndexPairs.Add(usedCurveIndexPair);
                    }
                    else 
                    {
                        usedCurveIndexPairs.Add(usedCurveIndexPair);
                        // 自交了，所以不做任何事情，不添加新的intersection到curveWithIntersections中，以显示二者没有交点
                    }
                }
            }

            /*-------------------------------------3. 最后再生成底面图形 -----------------------------------------*/
        // 使用intersection们来生成图形

        // 用来装单个边缘的对应矩形
            List<Curve> singleBlocks = new List<Curve>();

            foreach (Curve curve in curveWithIntersections.Keys)
            {
                //dispatchedEdges

                bool offsetConditionOfTheLand = curveWithOffsetedResults[curve].Count != 2;
                // true是不生成，false是生成
                // 处于end或者没有参与的边缘，不要参与矩形的生成


                // 单个边缘对应的矩形
                Curve singleBlock = GenerateSketch.DrawSingleBlockSketch(curveWithIntersections[curve], offsetConditionOfTheLand);
                if (singleBlock != null)
                {
                    singleBlocks.Add(singleBlock);
                }

            }
            
            // 布尔并集
            sketchOfABuilding = GenerateSketch.BoolSingleBlockSketchsUnion(singleBlocks);
            

            return sketchOfABuilding;
        }

        //----------------------------------------------------debug用----------------------------------------------------
        public static List<Curve> ReturnSingleBlocks(Land land/*, Dictionary<string, string> edgeProcessCondition, Dictionary<string, List<Curve>> directionWithEdges*/)
        {
            /*--------------------------------------------材料--------------------------------------------*/
            bool boundageOrNot = land.boundageOrNot;


            // 在bondage和不在boundage的原始边
            Dictionary<string, List<Curve>> onBoundage = land.onBoundage;
            Dictionary<string, List<Curve>> notOnBoundage = land.notOnBoundage;

            // 被分好方向的原始边缘
            Dictionary<string, List<Curve>> dispatchedEdges = land.dispatchedEdges;

            // land本身
            Curve landCurve = land.landCurve;

            // 数值
            double buildingDepth = land.GetBuildingDepth();
            double buildingSpacing = land.buildingSpacing;
            //double shortestEndDepth = land.GetShortestEndDepth();


            //东西南北
            string ew = land.isWestOrEast;
            string ns = land.isNorthOrSouth;

            //List<Curve>JudgeGenerateBehaviour.JudgeTheLandCondition(buildingSpacing, buildingDepth, landCurve, edgeProcessCondition, directionWithEdges);
            List<Curve> sketchOfSingleEdge = new List<Curve>();

            /// *****最后要传出的这个
            Curve sketchOfABuilding = null;

            /*----------------------------------------以下是方法----------------------------------------------*/



            /*-------------------------------------1. 先处理Edge的偏移 -----------------------------------------*/

            List<string> buildingTypeOfThisLandCurve = land.buildingTypeOfThisLandCurve;

            // ***** 建立原有的的边与偏移结果的联系
            Dictionary<Curve, List<Curve>> curveWithOffsetedResults = new Dictionary<Curve, List<Curve>>();

            // 获得四边的condition
            Dictionary<string, string> offsetBehavioursOfLandcurves = JudgeGenerateBehaviour.DetermineLandcurvesOffsetBehaviours(buildingTypeOfThisLandCurve, land.boundageDirections);


            // 遍历每一个方向
            foreach (string direction in offsetBehavioursOfLandcurves.Keys)
            {
                // 遍历每一个方向的所有原始边
                foreach (Curve originalEdge in dispatchedEdges[direction])
                {
                    // 对于原始边进行处理，得到单个的原始边的偏移后的边
                    List<Curve> offsetedEdge = EdgeProcessor(originalEdge, landCurve, buildingDepth, buildingSpacing, offsetBehavioursOfLandcurves[direction]);
                    curveWithOffsetedResults[originalEdge] = offsetedEdge;
                }


                // 开始处理intersection
            }

            /*-------------------------------------2. 再处理intersection -----------------------------------------*/
            // ******* 用来装Curve和它的intersection
            Dictionary<Curve, List<Point3d>> curveWithIntersections = new Dictionary<Curve, List<Point3d>>();

            foreach (Curve curve in curveWithOffsetedResults.Keys)
            {
                curveWithIntersections[curve] = new List<Point3d>();
            }


            // 用这个排除重复出现的组合，正确的组合应该是12，13，14，23，24，34这样
            //List< HashSet<Curve> > usedCurvePairs = new List<HashSet<Curve>>();

            // ## 这里可以用for循环，通过监督index的方式确定是否被重复使用了

            // 运算每一对边的intersection

            // 尝试不要使用foreach，使用for循环，通过监督index的方式确定是否被重复使用了
            //foreach (KeyValuePair<Curve, List<Curve>> pair1 in curveWithOffsetedResults)
            //{
            //    foreach(KeyValuePair<Curve, List<Curve>> pair2 in curveWithOffsetedResults)
            //    {

            List<int[]> usedCurveIndexPairs = new List<int[]>();

            for (int i = 0; i < curveWithOffsetedResults.Count; i++)
            {
                for (int j = 0; j < curveWithOffsetedResults.Count; j++)
                {
                    KeyValuePair<Curve, List<Curve>> pair1 = curveWithOffsetedResults.ElementAt(i);
                    KeyValuePair<Curve, List<Curve>> pair2 = curveWithOffsetedResults.ElementAt(j);

                    // 用于判断是否被使用过
                    bool isBeUsed = false;

                    int[] usedCurveIndexPair = { i, j };

                    // 不要自交
                    if (i != j || pair1.Equals(pair2) == false || pair1.Key != pair2.Key)

                    {
                        //// 有可能不需要转换成Dictionary
                        //// 配合参数数据类型
                        //Dictionary<Curve, List<Curve>>temporaryPair1 = new Dictionary<Curve, List<Curve>>();
                        //temporaryPair1.Add(pair1.Key, pair1.Value);
                        //Dictionary<Curve, List<Curve>>temporaryPair2 = new Dictionary<Curve, List<Curve>>();
                        //temporaryPair2.Add(pair2.Key, pair2.Value);

                        // 用来装intersection的点的临时的参数
                        Dictionary<Curve, List<Point3d>> curveWithIntersection1 = new Dictionary<Curve, List<Point3d>>();
                        Dictionary<Curve, List<Point3d>> curveWithIntersection2 = new Dictionary<Curve, List<Point3d>>();


                        // 1 先确定是不是计算过了，计算过的对久不用计算了

                        foreach (var arrayOfPair in usedCurveIndexPairs)
                        {
                            // 两个都出现过，证明已经运算过了，这样就没必要再算一次
                            if (arrayOfPair.Contains(i) && arrayOfPair.Contains(j))
                            {
                                Console.WriteLine("出现过一次了，不要重复");
                                isBeUsed = true;
                                break;
                            }
                            else
                            {
                                // 证明这一段没有运算过，可以通过
                                // 不做任何事情，继续循环
                            }
                        }

                        // 经过以上步骤，证明这一对没有被运算过，那么就可以运算了
                        if (isBeUsed == false)
                        {
                            // 2. 进行相交运算

                            bool isIntersection = false;

                            // curveWithIntersection1 是 i 线段的交点结果
                            // curveWithIntersection2 是 j 线段的交点结果
                            // 注意此时间curveWithIntersection1 中应该只有一个键
                            isIntersection = Intersect.GetIntersections(pair1, pair2, landCurve, 0.001,
                                                                    out curveWithIntersection1,
                                                                    out curveWithIntersection2);

                            // 3. 如果有交点，那么就要进行处理
                            if (isIntersection == true)
                            {
                                // 加入到对应的边缘当中
                                curveWithIntersections[curveWithIntersection1.Keys.First()].AddRange(curveWithIntersection1[curveWithIntersection1.Keys.First()]);
                                curveWithIntersections[curveWithIntersection2.Keys.First()].AddRange(curveWithIntersection2[curveWithIntersection2.Keys.First()]);

                                usedCurveIndexPairs.Add(usedCurveIndexPair);
                            }


                        }

                        else
                            usedCurveIndexPairs.Add(usedCurveIndexPair);
                    }
                    else
                    {
                        usedCurveIndexPairs.Add(usedCurveIndexPair);
                        // 自交了，所以不做任何事情，不添加新的intersection到curveWithIntersections中，以显示二者没有交点
                    }
                }
            }

            /*-------------------------------------3. 最后再生成底面图形 -----------------------------------------*/
            // 使用intersection们来生成图形

            // 用来装单个边缘的对应矩形
            List<Curve> singleBlocks = new List<Curve>();

            foreach (Curve curve in curveWithIntersections.Keys)
            {
                //dispatchedEdges

                bool offsetConditionOfTheLand = curveWithOffsetedResults[curve].Count != 2;
                // true是不生成，false是生成
                // 处于end或者没有参与的边缘，不要参与矩形的生成


                // 单个边缘对应的矩形
                Curve singleBlock = GenerateSketch.DrawSingleBlockSketch(curveWithIntersections[curve], offsetConditionOfTheLand);
                if (singleBlock != null)
                {
                    singleBlocks.Add(singleBlock);
                }

            }

            //// 布尔并集
            //sketchOfABuilding = GenerateSketch.BoolSingleBlockSketchsUnion(singleBlocks);


            return singleBlocks;
        }

    }
}
