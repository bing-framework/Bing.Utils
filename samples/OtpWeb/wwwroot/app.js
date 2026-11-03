let csrf = "";
let enrollment = null;
const byId = id => document.getElementById(id);
const status = text => { byId("status").textContent = text; };
async function api(path, body) {
  const response = await fetch(path, { method: body === undefined ? "GET" : "POST", credentials: "same-origin",
    headers: body === undefined ? {} : { "Content-Type": "application/json", "X-CSRF-TOKEN": csrf },
    body: body === undefined ? undefined : JSON.stringify(body), cache: "no-store" });
  const data = response.headers.get("content-type")?.includes("json") ? await response.json() : {};
  if (!response.ok) throw new Error(data.error || ("请求失败：" + response.status));
  return data;
}
function clearEnrollment() {
  enrollment = null; byId("secret").textContent = ""; byId("uri").value = "";
  byId("confirm-code").value = ""; byId("enrollment").hidden = true;
}
async function session() {
  const current = await api("/api/session");
  csrf = current.csrfToken;
  byId("session").textContent = current.authenticated ? (current.mfa ? "已完成二次认证" : "已完成主认证，等待动态验证码") : "尚未认证";
  byId("sign-in").hidden = !current.developmentLogin;
  byId("sign-out").hidden = !current.authenticated;
  if (current.authenticated) await devices();
  else byId("credentials").replaceChildren();
}
async function devices() {
  const items = await api("/api/otp/credentials");
  const select = byId("credentials"), previous = select.value;
  select.replaceChildren();
  for (const item of items.filter(x => x.status === 2)) {
    const option = document.createElement("option");
    option.value = item.credentialId; option.textContent = item.displayName + (item.parameters.type === 1 ? " · TOTP" : " · HOTP");
    select.append(option);
  }
  if ([...select.options].some(x => x.value === previous)) select.value = previous;
}
function credential() { const value = byId("credentials").value; if (!value) throw new Error("请先绑定并选择设备。"); return value; }
function on(id, event, action) {
  byId(id).addEventListener(event, async e => {
    e.preventDefault();
    try { await action(); } catch (error) { status(error.message); }
  });
}
on("sign-in", "click", async () => { clearEnrollment(); await api("/api/development/sign-in", {}); await session(); status("演示主认证已建立。"); });
on("sign-out", "click", async () => { await api("/api/sign-out", {}); clearEnrollment(); await session(); status("已退出。"); });
on("refresh", "click", devices);
on("enroll", "submit", async () => {
  clearEnrollment();
  enrollment = await api("/api/otp/enroll", { type: byId("type").value, displayName: byId("device-name").value });
  byId("secret").textContent = enrollment.secretBase32; byId("uri").value = enrollment.provisioningUri;
  byId("enrollment").hidden = false; status("请录入认证器并确认绑定。");
});
on("confirm", "submit", async () => {
  if (!enrollment) throw new Error("请先生成绑定信息。");
  const code = byId("confirm-code").value; byId("confirm-code").value = "";
  await api("/api/otp/confirm", { credentialId: enrollment.credentialId, transactionId: enrollment.transactionId, code });
  clearEnrollment(); await devices(); status("绑定完成，首次验证码已消费。");
});
on("verify", "submit", async () => {
  const credentialId = credential(), code = byId("verify-code").value; byId("verify-code").value = "";
  const tx = await api("/api/otp/transactions", {});
  await api("/api/otp/verify", { credentialId, transactionId: tx.transactionId, code });
  await session(); status("二次认证成功。");
});
on("revoke", "click", async () => { await api("/api/otp/revoke", { credentialId: credential() }); await devices(); status("设备已撤销。"); });
on("resync", "submit", async () => {
  const credentialId = credential(), firstCode = byId("first-code").value, secondCode = byId("second-code").value;
  byId("first-code").value = ""; byId("second-code").value = "";
  const tx = await api("/api/otp/resync/transactions", { credentialId });
  await api("/api/otp/resync", { credentialId, transactionId: tx.transactionId, firstCode, secondCode });
  status("重同步完成，请使用下一个验证码。");
});
on("protected", "click", async () => status((await api("/api/protected")).message));
session().catch(error => status(error.message));
