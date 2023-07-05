using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    public static class JudgeGenerateBehaviour
    {
        public static List<string> JudgeTheLandCondition(Dictionary<string, List<Curve>> relationshipDict, List<string> boundageDirections, Dictionary<string, double> directionAndLengths, string ew, string ns)
        {
            List<string> initialCondition = new List<string>;

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
            AddInToJudgeList(initialCondition, sortedDirections.First())
            //initialCondition.Add(sortedDirections.First());

            // 优先级b：在哪个方向区域里
            AddInToJudgeList(initialCondition, ew);
            AddInToJudgeList(initialCondition, ns);

            // -------------------------------------------再判断形态------------------------------------------------------------------
            // 判断directionAndLengths中，东西、南北两组中的最长者
            // 判断directionAndLengths中，东西、南北两组中的最长者
            var ewLength = directionAndLengths["EAST"] + directionAndLengths["WEST"];
            var nsLength = directionAndLengths["NORTH"] + directionAndLengths["SOUTH"];

            if (ewLength > nsLength)
            {
                AddInToJudgeList(initialCondition, "EAST");
                AddInToJudgeList(initialCondition, "WEST");
            }
            else if (ewLength < nsLength)
            {
                AddInToJudgeList(initialCondition, "NORTH");
                AddInToJudgeList(initialCondition, "SOUTH");
            }
            else
            {
                // 长度相等按照ns方向为准
                AddInToJudgeList(initialCondition, "NORTH");
                AddInToJudgeList(initialCondition, "SOUTH");
            }



        }

        /// <summary>
        /// 只有当judgeList还没有这个方向或者条件的时候，才加进去
        /// </summary>
        /// <param name="judgeList"></param>
        /// <param name="judgeCondition"></param>
        public static void AddInToJudgeList(List<string> judgeList, string judgeCondition)
        {
            if (judgeList.Contains(judgeCondition) == false)
            {
                judgeList.Add(judgeCondition);
            }
        }

    }
}
