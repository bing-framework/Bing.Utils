import test from 'node:test';
import assert from 'node:assert/strict';
import { imagePoint, dragRegion, validRegion } from '../wwwroot/geometry.mjs';
test('CSS 缩放后的鼠标位置转换成完整图片像素',()=> {
  assert.deepEqual(imagePoint(110,70,{left:10,top:20,width:200,height:100},1000,500),{x:500,y:250});
});
test('反向拖动产生整数区域，边缘坐标被限制',()=> {
  const a=imagePoint(-10,0,{left:0,top:0,width:100,height:100},100,100);
  const region=dragRegion({x:50.2,y:60.8},a);
  assert.deepEqual(region,{x:0,y:0,width:51,height:61}); assert.ok(validRegion(region,100,100));
  assert.equal(validRegion({x:99,y:0,width:2,height:1},100,100),false);
  assert.equal(validRegion({x:0,y:0,width:0,height:1},100,100),false);
});
