using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Showcase
{
    public class ShowCase : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ShowCase class.
        /// </summary>
        public ShowCase()
          : base("ShowCase", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddCurveParameter("Curve", "Curve", "test curve", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Output", "Output", "test out put", GH_ParamAccess.list)
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 在一个列表中建立Z坐标从1-10共19个点
            List<Point3d> pts1 = new List<Point3d>();
            for (int i = 0; i < 10; i++)
            {
                pts1.Add(new Point3d(0, 0, i));
            }

            List<Point3d> pts2 = new List<Point3d>();
            for (int i = 0; i < 10; i++)
            {
                pts2.Add(new Point3d(i, 0, 0));
            }

            List<List<Point3d>> output = new List<List<Point3d>> { pts1, pts2 };

            DA.SetDataList("Output", output);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F6EE9E1F-806C-4574-8BB7-859021A3ECE2"); }
        }
    }
}