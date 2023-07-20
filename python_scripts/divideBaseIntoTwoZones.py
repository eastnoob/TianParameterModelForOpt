import Rhino.Geometry as rg

def generate_rectangles(base, direction='vertical', offset=10.0):
    # Find the split line of the polygon
    bbox = base.GetBoundingBox(True)
    center = bbox.Center
    start = rg.Point3d(bbox.Min.X, center.Y, 0)
    end = rg.Point3d(bbox.Max.X, center.Y, 0)

    # Extend the line to the length of the polygon's vertical side
    diagonal = bbox.Diagonal
    length = diagonal.Length
    extension_factor = length / 2.0
    start_extended = rg.Point3d.Add(start, rg.Vector3d(start - center) * extension_factor)
    end_extended = rg.Point3d.Add(end, rg.Vector3d(end - center) * extension_factor)
    
    # Copy the split line to both left and right sides
    if direction == 'vertical':
        up_start = rg.Point3d(start_extended.X, start_extended.Y - diagonal.Y/2.0 - offset, 0)
        up_end = rg.Point3d(end_extended.X, end_extended.Y - diagonal.Y/2.0 - offset, 0)
        down_start = rg.Point3d(start_extended.X, start_extended.Y + diagonal.Y/2.0 + offset, 0)
        down_end = rg.Point3d(end_extended.X, end_extended.Y + diagonal.Y/2.0 + offset, 0)
    else:
        left_start = rg.Point3d(start_extended.X - diagonal.X/2.0 - offset, start_extended.Y, 0)
        left_end = rg.Point3d(end_extended.X - diagonal.X/2.0 - offset, end_extended.Y, 0)
        right_start = rg.Point3d(start_extended.X + diagonal.X/2.0 + offset, start_extended.Y, 0)
        right_end = rg.Point3d(end_extended.X + diagonal.X/2.0 + offset, end_extended.Y, 0)

    # Join points to form two rectangles
    if direction == 'vertical':
        up_up_cup = rg.LineCurve(start_extended, up_start)
        up_down_cup = rg.LineCurve(end_extended, up_end)
        up_start_cup = rg.LineCurve(start_extended, end_extended)
        up_end_cup = rg.LineCurve(up_start, up_end)
        down_up_cup = rg.LineCurve(start_extended, down_start)
        down_down_cup = rg.LineCurve(end_extended, down_end)
        down_start_cup = rg.LineCurve(start_extended, end_extended)
        down_end_cup = rg.LineCurve(down_start, down_end)
        up_zone = rg.Curve.JoinCurves([up_up_cup, up_down_cup, up_start_cup, up_end_cup])[0]
        down_zone = rg.Curve.JoinCurves([down_up_cup, down_down_cup, down_start_cup, down_end_cup])[0]
        return [up_zone, down_zone]
    else:
        left_left_cur = rg.LineCurve(start_extended, left_start)
        left_right_cur = rg.LineCurve(end_extended, left_end)
        left_start_cur = rg.LineCurve(start_extended, end_extended)
        left_end_cur = rg.LineCurve(left_start, left_end)
        right_left_cur = rg.LineCurve(start_extended, right_start)
        right_right_cur = rg.LineCurve(end_extended, right_end)
        right_start_cur = rg.LineCurve(start_extended, end_extended)
        right_end_cur = rg.LineCurve(right_start, right_end)
        left_zone = rg.Curve.JoinCurves([left_left_cur, left_right_cur, left_start_cur, left_end_cur])[0]
        right_zone = rg.Curve.JoinCurves([right_left_cur, right_right_cur, right_start_cur, right_end_cur])[0]
        return [left_zone, right_zone]