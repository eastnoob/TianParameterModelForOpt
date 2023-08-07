using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Collections;
using MoreLinq;
using Rhino.Input.Custom;
using System.Runtime.CompilerServices;
//using MoreLinq;


namespace TianParameterModelForOpt
{
    public static class JudgeGenerateBehaviour
    {


        /// <summary>
        /// 判断一个land应该生成什么形态的建筑
        /// </summary>
        /// <param name="boundageDirections">这个land所有存在boundage的边的方向</param>
        /// <param name="directionAndLengths">land四方向边缘各自的总长度</param>
        /// <param name="ew">属于东区还是西区，string</param>
        /// <param name="ns">属于南区还是北区，string</param>
        /// <param name="shortestLandDepth">生成block的最短宽度</param>
        /// <param name="shortestBLength"> 生成B的最短长</param>
        /// <param name="shortestLLength"> 生成L的最短长</param>
        /// <param name="shortestULength"> 生成U的最短长</param>
        /// <param name="shortestOLength"> 生成O的最短长</param>
        /// <returns>传回一个装满字符串的List，内部的字符串标记了建筑类型</returns>

        public static List<string> DetermineBuildingTypeOfTheLand(List<string> boundageDirections,
                                                        Dictionary<string, double> directionAndLengths,
                                                        Curve landCurve,
                                                        string ew, string ns,
                                                        double shortestLandDepth,
                                                        double shortestBrunchLength,
                                                        double shortestBLength,
                                                        double shortestLLength,
                                                        double shortestULength,
                                                        double shortestOLength,
                                                        double xLength,
                                                        double yLength)
        {

            string buildingType = null;


            List<string> initialCondition = new List<string>();


            // ------------------------------------------- 先判断形态 --------------------------------------------------------------


            // 如果最短边没过shortestLandDepth, 抛出信息：地块不能生成，并将”NO“添加到initialCondition中
            if (directionAndLengths.Min(x => x.Value) < shortestLandDepth)
            {
                Console.WriteLine("地块不能生成");
                initialCondition.Add("NO");
                return initialCondition;
            }
            // 如果最短边超过了shortestLandLength, 将继续判断
            else
            {

                // 如果最长边没过了shortestLandLength, 抛出信息：地块不能生成，并将”NO“添加到initialCondition中
                if (directionAndLengths.Max(x => x.Value) < shortestBLength)
                {
                    initialCondition.Add("NO");
                    return initialCondition;
                }


                else if (shortestBLength <= directionAndLengths.Max(x => x.Value)
                        && directionAndLengths.Max(x => x.Value) < shortestLLength)
                {
                    // TODO 记录日志：地块可以生成
                    if (directionAndLengths.Min(x => x.Value) <= shortestBrunchLength)
                        buildingType = "B";
                    else if (shortestBrunchLength < directionAndLengths.Min(x => x.Value)
                             && directionAndLengths.Min(x => x.Value) <= shortestBLength)
                        buildingType = "B";
                    else if (shortestBLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestLLength)
                        buildingType = "B";
                }

                else if((shortestLLength <= directionAndLengths.Max(x => x.Value)
                        && directionAndLengths.Max(x => x.Value) < shortestULength))
                {
                    if (directionAndLengths.Min(x => x.Value) <= shortestBrunchLength)
                        buildingType = "B";
                    else if (shortestBrunchLength < directionAndLengths.Min(x => x.Value)
                             && directionAndLengths.Min(x => x.Value) <= shortestBLength)
                        buildingType = "L";
                    else if (shortestBLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestLLength)
                        buildingType = "L";
                    else if (shortestLLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestULength)
                        buildingType = "L";
                }

                else if ((shortestULength <= directionAndLengths.Max(x => x.Value)
                         && directionAndLengths.Max(x => x.Value) < shortestOLength))
                {
                    if (directionAndLengths.Min(x => x.Value) <= shortestBrunchLength)
                        buildingType = "B";
                    else if (shortestBrunchLength < directionAndLengths.Min(x => x.Value)
                             && directionAndLengths.Min(x => x.Value) <= shortestBLength)
                        buildingType = "L";
                    else if (shortestBLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestLLength)
                        buildingType = "L";
                    else if (shortestLLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestULength)
                        buildingType = "U";
                    else if (shortestULength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestOLength)
                        buildingType = "U";
                }

                else if (shortestOLength <= directionAndLengths.Max(x => x.Value))
                {
                    if (directionAndLengths.Min(x => x.Value) <= shortestBrunchLength)
                        buildingType = "B";
                    else if (shortestBrunchLength < directionAndLengths.Min(x => x.Value)
                             && directionAndLengths.Min(x => x.Value) <= shortestBLength)
                        buildingType = "L";
                    else if (shortestBLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestLLength)
                        buildingType = "U";
                    else if (shortestLLength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestULength)
                        buildingType = "U";
                    else if (shortestULength < directionAndLengths.Min(x => x.Value)
                            && directionAndLengths.Min(x => x.Value) <= shortestOLength)
                        buildingType = "U";
                    else if (shortestOLength < directionAndLengths.Min(x => x.Value))
                        buildingType = "O";
                }
            }




            // --------------------------------------------- 再判断方向---------------------------------------------------


            // 如果boundageDirections是空的，就不用判断boundage了
            // 否则则将方向全部加入到initialCondition中

            Dictionary<string, int> directionWithScore = new Dictionary<string, int>{
                {"north", 0 },
                {"south", 0 },
                {"east", 0 },
                {"west", 0 }
            };


            // 先使用形状给一个基本分
            if (xLength > yLength)
            {
                if (directionAndLengths["north"] > directionAndLengths["south"] ||/* boundageDirections.Contains("north") || */ns == "north")
                {
                    directionWithScore["north"] += 2;
                }
                else if (directionAndLengths["north"] < directionAndLengths["south"] ||/* boundageDirections.Contains("south") || */ns == "south")
                {
                    directionWithScore["south"] += 2;
                }
            }
            else if (xLength < yLength)
            {
                  if (directionAndLengths["east"] > directionAndLengths["west"] ||/* boundageDirections.Contains("east") || */ew == "east")
                {
                    directionWithScore["east"] += 2;
                }
                else if (directionAndLengths["east"] < directionAndLengths["west"] ||/* boundageDirections.Contains("west") || */ew == "west")
                {
                    directionWithScore["west"] += 2;
                }
            }
            else
            {
                if (/*directionAndLengths["north"] > directionAndLengths["south"] || */boundageDirections.Contains("north") || ns == "north")
                {
                    directionWithScore["north"] += 1;
                }
                else if (/*directionAndLengths["north"] > directionAndLengths["south"] || */boundageDirections.Contains("south") || ns == "south")
                {
                    directionWithScore["south"] += 1;
                }
                else if (/*directionAndLengths["east"] > directionAndLengths["west"] || */boundageDirections.Contains("east") || ew == "east")
                {
                    directionWithScore["east"] += 1;
                }
                else if (/*directionAndLengths["east"] > directionAndLengths["west"] || */boundageDirections.Contains("west") || ew == "west")
                {
                    directionWithScore["west"] += 1;
                }
            }

            // 优先级A，判断boundageDirections是否为空，如果不为空，将其所有元素加入initialCondition
            if (boundageDirections.Count != 0)
            {
                foreach (var direction in boundageDirections)
                    directionWithScore[direction] += 2;
                // 将boundageDirections所有元素加入initialCondition
                //initialCondition.AddRange(boundageDirections);
            }
            else
            {
                // 如果其为空，则证明其不是一个boundage的land，不用添加什么
            }

            //List<string>temporaryDirectionContainer = new List<string>();

            // 继续判断方向

            // 优先级B，判断directionAndLengths哪一方向的边最长，其就带有这个方向
            // 找到directionAndLengths的最大值

            List<string> maxStrList = new List<string>();
            double maxVal = double.MinValue;

            foreach (KeyValuePair<string, double> entry in directionAndLengths)
            {
                if (entry.Value > maxVal)
                {
                    maxVal = entry.Value;
                    maxStrList.Clear();
                    //AddInToJudgeList(initialCondition, entry.Key);
                    
                    maxStrList.Add(entry.Key);
                }
                else if (entry.Value == maxVal)
                {
                    //AddInToJudgeList(initialCondition, entry.Key);
                    maxStrList.Add(entry.Key);
                }
            }
            //AddInToJudgeList(initialCondition, maxStrList[0]);
            directionWithScore[maxStrList[0]] += 2;


            // 优先级A：在哪个方向区域里
            //AddInToJudgeList(initialCondition, ew);
            directionWithScore[ew] += 2;

            //AddInToJudgeList(initialCondition, ns);
            directionWithScore[ns] += 2;


            // 增加长短避险
            //建立landcurve 的boundingbox
            var landCurveBoundingBox = landCurve.GetBoundingBox(Plane.WorldXY);
            // 如果boundbox的X比Y长
            if ((landCurveBoundingBox.Max.X - landCurveBoundingBox.Min.X) > 2 * (landCurveBoundingBox.Max.Y - landCurveBoundingBox.Min.Y))
            {
                //directionWithScore[maxStrList[0]] += 2;
                directionWithScore[ns] += 2;
            }
            else
            {
                directionWithScore[ew] += 2;
            }

            //// 增加对U的避险
            //if (buildingType == "U")
            //{

            //    //建立landcurve 的boundingbox
            //    var landCurveBoundingBox = landCurve.GetBoundingBox(Plane.WorldXY);
            //    // 如果boundbox的X比Y长
            //    if((landCurveBoundingBox.Max.X-landCurveBoundingBox.Min.X) > 2* (landCurveBoundingBox.Max.Y - landCurveBoundingBox.Min.Y))
            //    {
            //        //directionWithScore[maxStrList[0]] += 2;
            //        directionWithScore[ns] += 2;
            //    }
            //    else
            //    {
            //        directionWithScore[ew] += 2;
            //    }

            //}

            // 增加对B的避险
            if(buildingType == "B")
            {
                var nsList = new string[2] { "north", "south" };
                var ewList = new string[2] { "east", "west" };


                //if (directionWithScore[nsList[0]] == directionWithScore[nsList[1]])
                //{
                //    directionWithScore[nsList[0]] -= 1;
                //    directionWithScore[nsList[1]] -= 1;
                //}

                //if (directionWithScore[ewList[0]] == directionWithScore[ewList[1]])
                //{
                //    directionWithScore[ewList[0]] -= 1;
                //    directionWithScore[ewList[1]] -= 1;
                //}

                // 判断对边

                foreach (var nsString in nsList)
                {
                    if (directionAndLengths[ewList[0]] >= 2* directionAndLengths[nsString])
                    {
                        directionWithScore[nsString] -= 2;
                    }

                    else if (directionAndLengths[ewList[1]] >= 2 * directionAndLengths[nsString])
                    {
                        directionWithScore[nsString] -= 2;
                    }
                }

                foreach (var ewString in ewList)
                {
                    if (directionAndLengths[nsList[0]] >= 2 * directionAndLengths[ewString])
                    {
                        directionWithScore[ewString] -= 2;
                    }

                    else if (directionAndLengths[nsList[1]] >= 2 * directionAndLengths[ewString])
                    {
                        directionWithScore[ewString] -= 2;
                    }
                }


                if (maxStrList.Count != 1)// 两条边一样长
                {
                    //此时要取消其长度权重，改为方向权重
                    // 如果长边在n或者s里面，那么就不用管谁长，直接管在哪个区里面就行
                    
                    if (maxStrList.Contains(nsList[0]) || maxStrList.Contains(nsList[1]))
                    {
                        if (maxStrList.Contains(ns)) { directionWithScore[ns] += 2; }
                    }
                    else if (maxStrList.Contains(ewList[0]) || maxStrList.Contains(ewList[1]))
                    {
                        if (maxStrList.Contains(ew)) { directionWithScore[ew] += 2; }
                    }
                }
                else //证明有一条边最长，就按照最长的来就行
                {
                    directionWithScore[maxStrList[0]] += 2;
                }

            }

            // ---------------------------------------- 检查与纠错 -----------------------------------------------------

            Dictionary<string, int> typeWithItsNeedOffsetEdgeCount = new Dictionary<string, int>
            {
                {"B", 0 },
                {"L", 2 },
                {"U", 3 },
/*                {"C", 3 }*/
                {"O", 4 }
            };

            // 相邻边字典，四个方向是键，方向的两个临近方向组成的数组是值
            Dictionary<string, string[]> adjacentEdges = new Dictionary<string, string[]>
            {
                { "north", new string[] { "west", "east" } },
                { "east", new string[] { "north", "south" } },
                { "south", new string[] { "east", "west" } },
                { "west", new string[] { "south", "north" } }
            };

            // 对边组字典
            Dictionary<string, string> reverseDirection = new Dictionary<string, string>()
            {
                {"north", "south"},
                {"south", "north"},
                {"east", "west"},
                {"west", "east"}
            };


            // 以得分最高的那一些为基准边缘
            List<string> benchmarkCandidates = GetTheBiggestValueOneOfTheDictionary(directionWithScore);

            // benchmark要进行判断，如果不止一个的话，那么取在reverseDirection中不成对的那一个
            string benchmark = benchmarkCandidates[0];

            if (benchmarkCandidates.Count == 1 || benchmarkCandidates.Count == 2)
                benchmark = benchmarkCandidates[0];
            else
            {
                foreach (string benchmarkCandidate in benchmarkCandidates)
                {
                    if(! benchmarkCandidates.Contains(reverseDirection[benchmarkCandidate]));
                    {
                        benchmark = benchmarkCandidate;
                        break;
                    }
                }
            }


            if (! initialCondition.Contains(benchmark))
                initialCondition.Add(benchmark);

            // 利用形态判断
            if (buildingType == "B")
            {
                List<string> bCondition = new List<string> { benchmark };
                bCondition.Add(buildingType);
                return bCondition;

                // 对于B，其基准边的相邻边，对边都不为side
                foreach (string otherDirection in adjacentEdges[benchmark])
                {
                    if (initialCondition.Contains(otherDirection))
                    {
                        // 将这个otherdirectio移除
                        initialCondition.Remove(otherDirection);
                    }
                    // B类只需要偏移一条边
                    if(initialCondition.Count == 1)
                    {
                        initialCondition.Add(buildingType);
                        return initialCondition;
                    }
                }
            }
            else if(buildingType == "L")
            {
                // 对于L，另外有且只有一个side，而且必须是其相邻边
                directionWithScore.Remove(benchmark);
                List<string> secondScoreDirections = GetTheBiggestValueOneOfTheDictionary(directionWithScore);

                foreach (string direction in adjacentEdges[benchmark])
                {
                    if (secondScoreDirections.Contains(direction) || ! initialCondition.Contains(direction))
                    {
                        AddInToJudgeList(initialCondition, direction);
                    }
                    if (initialCondition.Count == 2)
                    {
                        initialCondition.Add(buildingType);
                        return initialCondition;
                    }
                        

                    else if (initialCondition.Count > 2)
                    {
                        // 如果超了2个，就剔除得分最低的那一个
                        Dictionary<string, int> findTheMin = new Dictionary<string, int>();
                        foreach(string directionOfSecond in initialCondition)
                        {
                            findTheMin.Add(directionOfSecond, directionWithScore[directionOfSecond]);
                        }
                        var minOne = GetTheBiggestValueOneOfTheDictionary(findTheMin, false)[0];
                        initialCondition.Remove(minOne);
                    }
                }
                initialCondition.Add(buildingType);
                return initialCondition;

            }
            else if(buildingType == "U")
            {
                // 对于u，其相邻两边必须是side
                foreach(string direction in adjacentEdges[benchmark])
                {
                    if (!initialCondition.Contains(direction))
                        //initialCondition.Add(direction);
                        AddInToJudgeList(initialCondition, direction, itsU: true);

                    if (initialCondition.Count == 3)
                    {
                        initialCondition.Add(buildingType);
                        return initialCondition;
                    }    
                }
                if (initialCondition.Count == 3)
                {
                    AddInToJudgeList(initialCondition, buildingType, itsU: true);
                    //initialCondition.Add(buildingType);
                    return initialCondition;
                }

            }
            else if (buildingType == "O")
            {
                // 对于o，无脑全加进去就行了
                if(initialCondition.Count != 4)
                {
                    foreach(var direction in adjacentEdges.Keys)
                    {
                        AddInToJudgeList(initialCondition, direction, true);
                    }

                    if(initialCondition.Count == 4)
                    {
                        initialCondition.Add(buildingType);
                        return initialCondition;
                    }
                }
                if (initialCondition.Count == 4)
                {
                    initialCondition.Add(buildingType);
                    return initialCondition;
                }
            }
            initialCondition.Add(buildingType);
            return initialCondition;
        }


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

            foreach (string direction in new string[] {"north", "south", "east", "west"})
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

            // 判断如果是B，那么基准边的对边不偏移
            if (condition.Contains("B"))
            {
                // 如果是B，那么基准边的对边不偏移
                if (condition.Contains("north"))
                    edgeProcessCondition["south"] = "end-boundage";
                else if (condition.Contains("south"))
                    edgeProcessCondition["north"] = "end-boundage";
                else if (condition.Contains("east"))
                    edgeProcessCondition["west"] = "end-boundage";
                else if (condition.Contains("west"))
                    edgeProcessCondition["east"] = "end-boundage";
            }

            return edgeProcessCondition;

        }

        /// <summary>
        /// 只有当judgeList还没有这个方向或者条件的时候，才加进去
        /// </summary>
        /// <param name="judgeList"></param>
        /// <param name="judgeCondition"></param>
        public static void AddInToJudgeList(List<string> judgeList, string judgeCondition, bool itsO = false, bool itsU = false)
        {

            //相反方向字典，如north对south，east对west
            Dictionary<string, string> reverseDirection = new Dictionary<string, string>()
            {
                {"north", "south"},
                {"south", "north"},
                {"east", "west"},
                {"west", "east"}
            };

            // 如果传入的是O形，直接无脑加进去
            if (itsO == true)
            {
                foreach (string direction in reverseDirection.Keys) 
                { 
                    if (!judgeList.Contains(direction))
                        judgeList.Add(direction);
                }
            }

            // 如果是U，则无视反向规则，但是不能超过三个
            else if (itsU == true)
            {
                judgeList.Add(judgeCondition);
                if (judgeList.Count > 3)
                {
                    judgeList.RemoveAt(judgeList.Count-1);
                    //return;
                }
            }   

            else if (judgeList.Contains(judgeCondition) == false)
            {
                //judgeList.Add(judgeCondition);
                // 同样不允许相反的方向同时存在
                // 以及，遇到U或者O形的时候，不允许存在其他的形态判断
                try
                {
                    if (!judgeList.Contains(reverseDirection[judgeCondition]))
                    {
                        //judgeList.Remove(judgeCondition);
                        judgeList.Add(judgeCondition);
                    }
                }
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    List<string> conditionList = new List<string>() { "L", "U", "O", "B" };

                    if (!judgeList.Contains(judgeCondition) && conditionList.Where(x => x != judgeCondition).All(x => !judgeList.Contains(x)))
                    {
                        //if (conditionList.Contains(judgeCondition) && conditionList.Where(x => x != judgeCondition).All(x => !judgeList.Contains(x)))
                            judgeList.Add(judgeCondition);
                    }
                    //throw;
                }
            }
        }

        public static List<string> GetTheBiggestValueOneOfTheDictionary(Dictionary<string, int> directionAndLengths, bool isUsedForMax = true)
        {
            if (isUsedForMax == true)
            {
                List<string> maxStrList = new List<string>();
                double maxVal = double.MinValue;

                foreach (KeyValuePair<string, int> entry in directionAndLengths)
                {
                    if (entry.Value > maxVal)
                    {
                        maxVal = entry.Value;
                        maxStrList.Clear();
                        //AddInToJudgeList(initialCondition, entry.Key);

                        maxStrList.Add(entry.Key);
                    }
                    else if (entry.Value == maxVal)
                    {
                        //AddInToJudgeList(initialCondition, entry.Key);
                        maxStrList.Add(entry.Key);
                    }
                }
                return maxStrList;
            }
            else
            {
                List<string> minStrList = new List<string>();
                double minVal = double.MaxValue;

                foreach (KeyValuePair<string, int> entry in directionAndLengths)
                {
                    if (entry.Value < minVal)
                    {
                        minVal = entry.Value;
                        minStrList.Clear();
                        //AddInToJudgeList(initialCondition, entry.Key);

                        minStrList.Add(entry.Key);
                    }
                    else if (entry.Value == minVal)
                    {
                        //AddInToJudgeList(initialCondition, entry.Key);
                        minStrList.Add(entry.Key);
                    }
                }
                return minStrList;
            }
        }

    }
}
