using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt._5_output
{
    internal static class Output
    {
        public static DataTree<Brep> ConvertListToDataTree(List<List<Brep>> brepLists)
        {
            DataTree<Brep> dataTree = new DataTree<Brep>();

            for (int i = 0; i < brepLists.Count; i++)
            {
                GH_Path path = new GH_Path(i);

                if (brepLists[i] == null || brepLists[i].Contains(null))
                {
                    dataTree.Add(null, path);
                    continue;
                }

                for (int j = 0; j < brepLists[i].Count; j++)
                {
                    dataTree.Add(brepLists[i][j], path);
                }
            }

            return dataTree;
        }
    }
}
