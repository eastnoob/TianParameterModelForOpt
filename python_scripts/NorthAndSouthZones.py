import Rhino.Geometry as rg

def generate_rectangles(base, offset=10.0):
    # 找到多边形的纵向分割线
    bbox = base.GetBoundingBox(True)
    center = bbox.Center
    start = rg.Point3d(bbox.Min.X, center.Y, 0)
    end = rg.Point3d(bbox.Max.X, center.Y, 0)

    # 将分割线延长到可包裹多边形的纵向长度
    diagonal = bbox.Diagonal
    length = diagonal.Length
#    extension_factor = length
    extension_factor = (length / 2.0)
    start_extended = rg.Point3d.Add(start, rg.Vector3d(start - center) * extension_factor)
    end_extended = rg.Point3d.Add(end, rg.Vector3d(end - center) * extension_factor)
    
#    return start_extended
    
    # 复制分割线并向左右两侧都复制一份
    up_start = rg.Point3d(start_extended.X, start_extended.Y - diagonal.Y/2.0 - offset, 0)
    up_end = rg.Point3d(end_extended.X, end_extended.Y - diagonal.Y/2.0 - offset, 0)
    
    down_start = rg.Point3d(start_extended.X, start_extended.Y + diagonal.Y/2.0 + offset, 0)
    down_end = rg.Point3d(end_extended.X, end_extended.Y + diagonal.Y/2.0 + offset, 0)

#    return [up_start, up_end, down_start, down_end]

    # 将分割线的起点终点与复制结果的起点终点对应连线，组合成为两个矩形
    up_up_cup = rg.LineCurve(start_extended, up_start)
    up_down_cup = rg.LineCurve(end_extended, up_end)
    up_start_cup = rg.LineCurve(start_extended, end_extended)
    up_end_cup = rg.LineCurve(up_start, up_end)
    
    down_up_cup = rg.LineCurve(start_extended, down_start)
    down_down_cup = rg.LineCurve(end_extended, down_end)
    down_start_cup = rg.LineCurve(start_extended, end_extended)
    down_end_cup = rg.LineCurve(down_start, down_end)
    
    # 连线为两个Zones
    
    up_zone = rg.Curve.JoinCurves([up_up_cup, up_down_cup, up_start_cup, up_end_cup])[0]
    down_zone = rg.Curve.JoinCurves([down_up_cup, down_down_cup, down_start_cup, down_end_cup])[0]
    
    # 传回结果
    return [up_zone, down_zone]

a = generate_rectangles(base)