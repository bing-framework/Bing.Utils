const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

// 执行实际页面脚本，用可控响应复现网络时序；不复制页面实现。
const html = fs.readFileSync(path.join(__dirname, '../wwwroot/index.html'), 'utf8');
const script = html.match(/<script>([\s\S]*?)<\/script>/)[1];
const tick = () => new Promise(setImmediate);
function page() {
    const elements = Object.fromEntries(['form', 'picture', 'refresh', 'submit', 'status', 'answer', 'expiry'].map(id =>
        [id, { hidden: true, disabled: false, value: '', textContent: '', focused: false, focus() { this.focused = true; } }]));
    const requests = [];
    vm.runInNewContext(script, {
        crypto: { randomUUID: () => 'form-1' },
        document: { getElementById: id => elements[id] },
        fetch: (url, options) => new Promise(resolve => requests.push({ url, options, respond(status, data) {
            resolve({ ok: status >= 200 && status < 300, status, json: async () => data });
        } }))
    });
    return { elements, requests };
}
const challenge = id => ({ captchaId: id, imageBase64: 'png', expiresAt: '2030-01-01T00:00:00Z' });
async function initialize(p) {
    p.requests[0].respond(200, { token: 'csrf' });
    await tick();
    p.requests[1].respond(200, challenge('first'));
    await tick();
}

test('slow session plus manual refresh sends one challenge only after the token arrives', async () => {
    const p = page();
    assert.equal(p.elements.refresh.disabled, true);
    await p.elements.refresh.onclick();
    assert.equal(p.requests.length, 1);
    p.requests[0].respond(200, { token: 'csrf' });
    await tick();
    assert.equal(p.requests.length, 2);
    assert.equal(p.requests[1].options.headers['X-CSRF-TOKEN'], 'csrf');
    p.requests[1].respond(200, challenge('first'));
    await tick();
    assert.equal(p.elements.picture.hidden, false);
    assert.equal(p.elements.submit.disabled, false);
    assert.equal(p.elements.answer.focused, true);
});

test('failed session can be retried by refreshing without sending an unauthenticated POST', async () => {
    const p = page();
    p.requests[0].respond(503);
    await tick();
    assert.equal(p.requests.length, 1);
    assert.equal(p.elements.refresh.disabled, false);
    assert.equal(p.elements.submit.disabled, true);
    const retry = p.elements.refresh.onclick();
    assert.equal(p.requests[1].url, '/api/session');
    p.requests[1].respond(200, { token: 'retry-csrf' });
    await tick();
    assert.equal(p.requests[2].options.headers['X-CSRF-TOKEN'], 'retry-csrf');
    p.requests[2].respond(200, challenge('retry'));
    await retry;
    assert.equal(p.elements.picture.hidden, false);
});

test('refreshes are serialized and clear the old answer and expiry', async () => {
    const p = page();
    await initialize(p);
    p.elements.answer.value = 'old-answer';
    const refresh = p.elements.refresh.onclick();
    await tick();
    await p.elements.refresh.onclick();
    assert.equal(p.requests.length, 3);
    assert.equal(p.elements.answer.value, '');
    assert.equal(p.elements.expiry.textContent, '');
    assert.equal(p.elements.picture.hidden, true);
    p.requests[2].respond(200, challenge('latest'));
    await refresh;
    const submit = p.elements.form.onsubmit({ preventDefault() {} });
    assert.equal(JSON.parse(p.requests[3].options.body).captchaId, 'latest');
    await p.elements.refresh.onclick();
    assert.equal(p.requests.length, 4);
    p.requests[3].respond(400);
    await submit;
    assert.equal(p.elements.submit.disabled, true);
    assert.equal(p.elements.refresh.disabled, false);
    const next = p.elements.refresh.onclick();
    await tick();
    p.requests[4].respond(200, challenge('after-error'));
    await next;
    assert.equal(p.elements.submit.disabled, false);
});
