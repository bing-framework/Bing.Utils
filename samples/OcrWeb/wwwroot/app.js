import { imagePoint, dragRegion, validRegion } from './geometry.mjs';
const byId = id => document.getElementById(id);
const canvas = byId('canvas'), ctx = canvas.getContext('2d');
let bitmap = null, file = null, result = null, selection = null, anchor = null;
let revision = 0, requestId = 0, controller = null, csrf = null;
const status = (text, error = false) => { byId('status').textContent = text; byId('status').classList.toggle('error', error); };
function busy(value) { byId('recognize').disabled = value || !file; byId('cancel').disabled = !value; }
function clearResult() {
  result = null; byId('edited').value = ''; byId('original').textContent = '';
  byId('lines').replaceChildren(); byId('count').textContent = '';
  for (const id of ['copy','txt','json']) byId(id).disabled = true;
}
function syncRegion() {
  const r = selection ?? { x:0,y:0,width:bitmap?.width ?? '',height:bitmap?.height ?? '' };
  for (const key of ['x','y','width','height']) byId(key).value = r[key];
}
function draw() {
  if (!bitmap) return;
  ctx.fillStyle = '#fff'; ctx.fillRect(0,0,canvas.width,canvas.height); ctx.drawImage(bitmap,0,0);
  ctx.lineWidth = Math.max(1, canvas.width / 500);
  for (const line of result?.lines ?? []) {
    ctx.strokeStyle = line.isLowConfidence ? '#c78105' : '#2676dc';
    ctx.beginPath(); line.polygon.forEach((p,i) => i ? ctx.lineTo(p.x,p.y) : ctx.moveTo(p.x,p.y)); ctx.closePath(); ctx.stroke();
  }
  if (selection) {
    ctx.strokeStyle = '#0a9b86'; ctx.setLineDash([8,5]); ctx.strokeRect(selection.x,selection.y,selection.width,selection.height); ctx.setLineDash([]);
  }
}
async function selectFile(next) {
  controller?.abort(); requestId++; const current = ++revision;
  bitmap?.close(); bitmap = null; file = null; selection = null; clearResult(); busy(false);
  canvas.hidden = true; byId('empty').hidden = false; syncRegion();
  if (!next) return;
  if (next.size > 10 * 1024 * 1024 || !/\.(png|jpe?g|bmp)$/i.test(next.name)) { status('请选择不超过 10 MiB 的 PNG、JPEG 或 BMP 图片。', true); return; }
  try {
    const decoded = await createImageBitmap(next, { imageOrientation:'from-image' });
    if (current !== revision) { decoded.close(); return; }
    if (decoded.width > 8192 || decoded.height > 8192 || decoded.width * decoded.height > 20000000) {
      decoded.close(); throw new Error('图片尺寸超过限制，请先裁切图片。');
    }
    bitmap = decoded; file = next; canvas.width = bitmap.width; canvas.height = bitmap.height;
    canvas.hidden = false; byId('empty').hidden = true; syncRegion(); draw(); busy(false);
    status('图片已准备好，可直接识别或拖动选择区域。');
  } catch (error) { if (current === revision) status(error.message || '无法打开图片。', true); }
}
byId('file').addEventListener('change', event => selectFile(event.target.files[0]));
byId('drop').addEventListener('dragover', event => event.preventDefault());
byId('drop').addEventListener('drop', event => { event.preventDefault(); selectFile(event.dataTransfer.files[0]); });
byId('whole').addEventListener('click', () => { selection = null; syncRegion(); draw(); });
const point = event => imagePoint(event.clientX,event.clientY,canvas.getBoundingClientRect(),canvas.width,canvas.height);
canvas.addEventListener('pointerdown', event => { if (!bitmap) return; anchor = point(event); canvas.setPointerCapture(event.pointerId); });
canvas.addEventListener('pointermove', event => { if (!anchor) return; selection = dragRegion(anchor,point(event)); syncRegion(); draw(); });
canvas.addEventListener('pointerup', event => { if (!anchor) return; selection = dragRegion(anchor,point(event)); anchor = null; if (!validRegion(selection,canvas.width,canvas.height)) selection = null; syncRegion(); draw(); });
canvas.addEventListener('pointercancel', () => { anchor = null; selection = null; syncRegion(); draw(); });
for (const key of ['x','y','width','height']) byId(key).addEventListener('change', () => {
  const r = Object.fromEntries(['x','y','width','height'].map(k => [k,Number(byId(k).value)]));
  if (!bitmap || !validRegion(r,canvas.width,canvas.height)) { status('区域需位于图片内，坐标和尺寸需为整数。',true); return; }
  selection = r; draw(); status('识别区域已更新。');
});
byId('cancel').addEventListener('click', () => controller?.abort());
byId('recognize').addEventListener('click', async () => {
  if (!file) return;
  const region = Object.fromEntries(['x','y','width','height'].map(k => [k,Number(byId(k).value)]));
  if (!validRegion(region,canvas.width,canvas.height)) { status('请修正识别区域。',true); return; }
  const id = ++requestId; const currentRevision = revision;
  const abort = new AbortController(); controller = abort; busy(true); clearResult(); draw(); status('正在识别…');
  try {
    if (!csrf) {
      const session = await fetch('/api/session',{signal:abort.signal});
      if (!session.ok) throw new Error('无法初始化识别会话。');
      csrf = (await session.json()).token;
    }
    const form = new FormData(); form.append('image',file);
    form.append('options',JSON.stringify({ languageHint:'zh-Hans+en',region,rotationDegrees:Number(byId('rotation').value),preprocessing:byId('preset').value }));
    const response = await fetch('/api/ocr',{method:'POST',headers:{'X-CSRF-TOKEN':csrf},body:form,signal:abort.signal});
    const data = await response.json();
    if (!response.ok) { if (response.status === 400) csrf = null; throw new Error(data.message || '识别失败，请稍后重试。'); }
    if (id !== requestId || currentRevision !== revision || abort.signal.aborted) return;
    if (data.imageWidth !== canvas.width || data.imageHeight !== canvas.height) throw new Error('预览与识别图尺寸不一致，已停止叠加文字框。');
    result = data; byId('edited').value = data.text; byId('original').textContent = data.text;
    byId('count').textContent = data.lines.length + ' 行';
    byId('lines').replaceChildren(...data.lines.map(line => {
      const li = document.createElement('li'); li.classList.toggle('low',line.isLowConfidence);
      li.textContent = line.text + (line.confidence == null ? '（未提供分数）' : '（分数 ' + line.confidence.toFixed(3) + '）'); return li;
    }));
    for (const key of ['copy','txt','json']) byId(key).disabled = false;
    draw(); status(data.lines.length ? '识别完成，请核对并编辑文字。' : '识别完成，未检测到文字。');
  } catch (error) {
    if (id === requestId) status(error.name === 'AbortError' ? '已取消等待。' : error.message,true);
  } finally { if (id === requestId) { controller = null; busy(false); } }
});
byId('copy').addEventListener('click', async () => {
  try { await navigator.clipboard.writeText(byId('edited').value); status('文字已复制。'); }
  catch { status('无法访问剪贴板，请在文本框中选择并复制。',true); }
});
function download(name, contents, type) {
  const url = URL.createObjectURL(new Blob([contents],{type}));
  const link = document.createElement('a'); link.href = url; link.download = name; link.click(); setTimeout(()=>URL.revokeObjectURL(url),1000);
}
byId('txt').addEventListener('click',()=>download('ocr.txt',byId('edited').value,'text/plain;charset=utf-8'));
byId('json').addEventListener('click',()=>download('ocr.json',JSON.stringify({recognition:result,editedText:byId('edited').value},null,2),'application/json;charset=utf-8'));
