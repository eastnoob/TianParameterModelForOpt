import Rhino.Geometry as rg

def generate_rectangles(base, offset=10.0):
    # 找到多边形的纵向分割线
    bbox = base.GetBoundingBox(True)
    center = bbox.Center
    start = rg.Point3d(center.X, bbox.Min.Y, 0)
    end = rg.Point3d(center.X, bbox.Max.Y, 0)

    # 将分割线延长到可包裹多边形的纵向长度
    diagonal = bbox.Diagonal
    length = diagonal.Length
#    extension_factor = length
    extension_factor = (length / 2.0)
    start_extended = rg.Point3d.Add(start, rg.Vector3d(start - center) * extension_factor)
    end_extended = rg.Point3d.Add(end, rg.Vector3d(end - center) * extension_factor)

    # 复制分割线并向左右两侧都复制一份
    left_start = rg.Point3d(start_extended.X - diagonal.X/2.0 - offset, start_extended.Y, 0)
    left_end = rg.Point3d(end_extended.X - diagonal.X/2.0 - offset, end_extended.Y, 0)
    
    right_start = rg.Point3d(start_extended.X + diagonal.X/2.0 + offset, start_extended.Y, 0)
    right_end = rg.Point3d(end_extended.X + diagonal.X/2.0 + offset, end_extended.Y, 0)

    # 将分割线的起点终点与复制结果的起点终点对应连线，组合成为两个矩形
    left_up_cup = rg.LineCurve(start_extended, left_start)
    left_down_cup = rg.LineCurve(end_extended, left_end)
    left_start_cup = rg.LineCurve(start_extended, end_extended)
    left_end_cup = rg.LineCurve(left_start, left_end)
    
    right_up_cup = rg.LineCurve(start_extended, right_start)
    right_down_cup = rg.LineCurve(end_extended, right_end)
    right_start_cup = rg.LineCurve(start_extended, end_extended)
    right_end_cup = rg.LineCurve(right_start, right_end)
    
    # 连线为两个Zones
    
    left_zone = rg.Curve.JoinCurves([left_up_cup, left_down_cup, left_start_cup, left_end_cup])
    right_zone = rg.Curve.JoinCurves([right_up_cup, right_down_cup, right_start_cup, right_end_cup])
    
    # 传回结果
    return [left_zone, right_zone]

a = generate_rectangles(base)[0]