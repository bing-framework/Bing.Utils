// 坐标始终使用 EXIF 校正后的预览画布像素。
export function imagePoint(clientX, clientY, bounds, width, height) {
  return { x: Math.max(0, Math.min(width, (clientX - bounds.left) * width / bounds.width)),
    y: Math.max(0, Math.min(height, (clientY - bounds.top) * height / bounds.height)) };
}
export function dragRegion(a, b) {
  const x = Math.floor(Math.min(a.x, b.x)), y = Math.floor(Math.min(a.y, b.y));
  return { x, y, width: Math.ceil(Math.max(a.x, b.x)) - x, height: Math.ceil(Math.max(a.y, b.y)) - y };
}
export function validRegion(region, width, height) {
  return [region.x,region.y,region.width,region.height].every(Number.isInteger) &&
    region.x >= 0 && region.y >= 0 && region.width > 0 && region.height > 0 &&
    region.x + region.width <= width && region.y + region.height <= height;
}
