using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


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
                                                        string ew, string ns,
                                                        double shortestLandDepth,
                                                        double shortestBLength,
                                                        double shortestLLength,
                                                        double shortestULength,
                                                        double shortestOLength)
        {
            List<string> initialCondition = new List<string>();

            // -------------------------------------------先判断方向------------------------------------------------------------------

            // 如果boundageDirections是空的，就不用判断boundage了
            // 否则则将方向全部加入到initialCondition中
            if (boundageDirections.Count != 0)
            {
                // 将boundageDirections所有元素加入initialCondition
                initialCondition.AddRange(boundageDirections);
            }

            // 继续判断方向
            // 优先级A，判断directionAndLengths哪一方向的边最长，其就带有这个方向

            // 将directionAndLengths按照value从大到小排序
            var sortedDirections = directionAndLengths.OrderByDescending(x => x.Value).Select(x => x.Key);

            // 将最长的边的方向加入到initialCondition中
            AddInToJudgeList(initialCondition, sortedDirections.First());
            //initialCondition.Add(sortedDirections.First());

            // 优先级b：在哪个方向区域里
            AddInToJudgeList(initialCondition, ew);
            AddInToJudgeList(initialCondition, ns);

            // -------------------------------------------再判断形态------------------------------------------------------------------
            // 如果最短边没过了shortestLandDepth, 抛出信息：地块不能生成，并将”NO“添加到initialCondition中
            if (directionAndLengths.Min(x => x.Value) < shortestLandDepth)
            {
                initialCondition.Add("NO");
                return initialCondition;
            }
            // 如果最短边超过了shortestLandLength, 将继续判断
            else if (directionAndLengths.Min(x => x.Value) > shortestBLength)
            {
                

                // 如果最长边没过了shortestLandLength, 抛出信息：地块不能生成，并将”NO“添加到initialCondition中
                if (directionAndLengths.Max(x => x.Value) < shortestBLength)
                {
                    initialCondition.Add("NO");
                    return initialCondition;
                }

                else
                {
                    // TODO 记录日志：地块可以生成
                    
                    // 如果此时最短边超过了shortestLandLength, 但是没超过shortestLLength, 
                    if (directionAndLengths.Min(x => x.Value) < shortestLLength)
                    {
                        // 如果最长边不足以构成L，则将"B"加入到initialCondition中
                        if (directionAndLengths.Max(x => x.Value) < shortestLLength)
                            AddInToJudgeList(initialCondition, "B");
                        // 如果最长边足以构成L
                        else if (directionAndLengths.Max(x => x.Value) >= shortestLLength)
                        {
                            // 且足以构成U，则将"U"加入到initialCondition中
                            if (directionAndLengths.Max(x => x.Value) >= shortestULength)
                                AddInToJudgeList(initialCondition, "U");

                            // 否则将"L"加入到initialCondition中
                            else
                                AddInToJudgeList(initialCondition, "L");
                        }
                    }

                    // 如果此时最短边超过了L,
                    else if (directionAndLengths.Min(x => x.Value) >= shortestLLength)
                    {
                        // 但是没超过U
                        if (directionAndLengths.Min(x => x.Value) < shortestULength)
                        {
                            // 如果最长边已经超过了L，但是没超过U，则将"L"加入到initialCondition中
                            if (directionAndLengths.Max(x => x.Value) < shortestULength)
                                AddInToJudgeList(initialCondition, "L");
                            // 否则为U
                            else
                                AddInToJudgeList(initialCondition, "U");
                        }

                        // 如果此时最短边超过了U是，
                        else if(directionAndLengths.Min(x => x.Value) >= shortestULength)
                        {
                            // 但还没到O
                            if (directionAndLengths.Min(x => x.Value) < shortestOLength)
                                AddInToJudgeList(initialCondition, "U");
                            else
                                AddInToJudgeList(initialCondition, "O");

                            //if (directionAndLengths.Max(x => x.Value) < shortestOLength)
                            //    AddInToJudgeList(initialCondition, "U");
                            //else if (directionAndLengths.Max(x => x.Value) >= shortestOLength)
                            //    AddInToJudgeList(initialCondition, "O");
                        }

                        else
                        {
                            AddInToJudgeList(initialCondition, "B");
                        }
                    }
                }
            
            }
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

        /// <summary>
        /// 只有当judgeList还没有这个方向或者条件的时候，才加进去
        /// </summary>
        /// <param name="judgeList"></param>
        /// <param name="judgeCondition"></param>
        public static void AddInToJudgeList(List<string> judgeList, string judgeCondition)
        {
            //相反方向字典，如north对south，east对west
            Dictionary<string, string> reverseDirection = new Dictionary<string, string>()
            {
                {"north", "south"},
                {"south", "north"},
                {"east", "west"},
                {"west", "east"}
            };

            if (judgeList.Contains(judgeCondition) == false)
            {
                judgeList.Add(judgeCondition);
                // 同样不允许相反的方向同时存在
                if (!judgeList.Contains(reverseDirection[judgeCondition]))
                {
                    //judgeList.Remove(judgeCondition);
                    judgeList.Add(judgeCondition);
                }

            }
        }

    }
}
