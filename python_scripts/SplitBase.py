# coding=utf-8
"""
                       _oo0oo_
                      o8888888o
                      88" . "88
                      (| -_- |)
                      0\  =  /0
                    ___/`---'\___
                  .' \\|     |// '.
                 / \\|||  :  |||// \
                / _||||| -:- |||||- \
               |   | \\\  - /// |   |
               | \_|  ''\---/''  |_/ |
               \  .-\__  '-'  ___/-. /
             ___'. .'  /--.--\  `. .'___
          ."" '<  `.___\_<|>_/___.' >' "".
         | | :  `- \`.;`\ _ /`;.`/ - ` : | |
         \  \ `_.   \_ __\ /__ _/   .-` /  /
     =====`-.____`.___ \_____/___.-`___.-'=====
                       `=---='


     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

           佛祖保佑       永不宕机     永无BUG
"""

import Rhino.Geometry as rh
from scriptcontext import doc
import rhinoscriptsyntax as rs

ds = _split_direction
ss = _split_degree
#curve = _project_base

tol = doc.ModelAbsoluteTolerance

## setback the base
base_polyline = rh.PolylineCurve.ToPolyline(rh.Curve.ToPolyline(_project_base, tol, tol, 1000, 1000))
centpt = rh.Polyline.CenterPoint(base_polyline)
XYplane = rh.Plane(centpt, rs.coerce3dvector([0, 0, 1]))

condition = rh.CurveOffsetCornerStyle.Sharp
array_setback = rh.Curve.Offset(_project_base, XYplane, _setback, tol, condition)

if len(array_setback) > 1:
    jointed_setback = rh.Curve.JoinCurves(array_setback)
else:
    jointed_setback = array_setback

curve = jointed_setback[0]

## function to split one curve by direction (d) and split (s) parameter

def rotate_bounding_box(base, bounding, angle):
    ## Not be used
    
    centpt = rh.AreaMassProperties.Compute(_project_base).Centroid
    scale = rh.Transform.Scale(rh.Plane.WorldXY, 5, 5, 1)
    rotate = rh.Transform.Rotation(angle, rh.Plane.WorldXY.ZAxis, centpt)
    
    bounding.Transform(scale)
    bounding.Transform(rotate)
    
    return bounding

def split_curve(curve, d, s):
    #    bb = rh.Curve.GetBoundingBox(
    
    bb = curve.GetBoundingBox(True)
#    bbo = curve.GetBoundingBox(True)
#    bb = rotate_bounding_box(curve, bbo, _rotate_angle)
    
    base_pt = rh.Point3d(bb.Min.X, bb.Min.Y, 0.0)
    
    x = bb.Max.X - bb.Min.X
    y = bb.Max.Y - bb.Min.Y
#    print(x, y)
    dims = [x, y]
    vecs = [rh.Vector3d(1,0,0), rh.Vector3d(0,1,0)]
    
    vec_1 = vecs[d] * dims[d] * s
    new_pt_1 = rh.Point3d(base_pt)
    new_pt_1.Transform(rh.Transform.Translation(vec_1))
    
    other_dir = abs(d - 1)
    
    vec_2 = vecs[other_dir] * dims[other_dir]
    new_pt_2 = rh.Point3d(new_pt_1)
    new_pt_2.Transform(rh.Transform.Translation(vec_2))
    
    
    split_line = rh.Line(new_pt_1, new_pt_2).ToNurbsCurve()
    
    inter = rh.Intersect.Intersection.CurveCurve(curve, split_line, tol, tol)
    p = [i.ParameterA for i in inter]
    
    if len(p) > 2:
        for i in range(len(p)):
            
            pt1 = curve.PointAt(p[i-1])
            pt2 = curve.PointAt(p[i])
            
            line = rh.Line(pt1, pt2).ToNurbsCurve()
            cp = line.PointAtNormalizedLength(0.5)
            
            if (curve.Contains(cp) == rh.PointContainment.Inside and 
                    len(rh.Intersect.Intersection.CurveCurve(curve, line, tol, tol)) <= 2):
                p = [p[i-1], p[i]]
                break
    
    pieces = curve.Split(p)
    
    curves = []
    
    for piece in pieces:
        line = rh.Line(piece.PointAtStart, piece.PointAtEnd)
        curve = rh.NurbsCurve.JoinCurves([piece, line.ToNurbsCurve()])
        curves += curve
    
    return curves


def split_recursively(curves, ds, ss):
    
    if len(ds) == 0 or len(ss) == 0:
        return curves
    
    curve = curves.pop(0)
    d = ds.pop(0)
    s = ss.pop(0)
    
    curves += split_curve(curve, d, s)
    
    print(len(curves))
    
    return split_recursively(curves, ds, ss)
print(curve)
print(ds)
print(ss)

splited_space_ = split_recursively([curve], ds, ss)
setbacked_base_ = curve