# 娴嬭瘯杩佺Щ鎶ュ憡绱㈠紩锛圡igration Reports Index锛?

## 1. 鐩殑
鏈洰褰曠敤浜庡綊妗ｆ瘡涓€杞祴璇曢噸褰掔被杩佺Щ鐨勯獙璇佹姤鍛婏紝渚夸簬锛?
- 璺熻釜杩佺Щ杩涘害锛堣瘯鐐?/ P0 / P1 / P2锛?
- 瀹¤姣忚疆鎵ц鑼冨洿涓庨獙璇佺粨鏋?
- 蹇€熷畾浣嶉仐鐣欓棶棰樹笌椋庨櫓椤?
- 鏀寔鍥炴粴涓庡悗缁紭鍖栧喅绛?

---

## 2. 鍏宠仈鏂囨。
- `AGENTS.md`
- `.agents/skills/test-project-boundary/SKILL.md`
- `.agents/skills/test-project-boundary/refs/test-project-boundary.md`
- `docs/quality/test-reclassification-plan.md`

> 娉細鑻ヨ鍒欐枃妗ｅ凡杩佺Щ鑷?Skill 鍐咃紝浠ヤ笂浠?Skill 鍐呯増鏈负鍑嗐€?

---

## 3. 鍛藉悕瑙勮寖锛堟姤鍛婃枃浠讹級
寤鸿姣忚疆鎶ュ憡浣跨敤浠ヤ笅鍛藉悕鏍煎紡锛?

`YYYY-MM-DD-<scope>-<batch>.md`

绀轰緥锛?
- `2026-02-25-idutils-pilot.md`
- `2026-02-26-p0-text-collections.md`
- `2026-02-27-p1-datetime-reflection.md`
- `2026-02-28-p2-http-drawing.md`

鍛藉悕寤鸿锛?
- `scope`锛氭湰杞富瑕佹ā鍧楁垨鑼冨洿锛堝 `idutils` / `text-collections`锛?
- `batch`锛氶樁娈垫爣璇嗭紙濡?`pilot` / `p0` / `p1` / `p2`锛?

---

## 4. 杩佺Щ闃舵鎬昏

### 4.1 闃舵瀹氫箟
- **Pilot锛堣瘯鐐癸級**锛氬厛澶勭悊涓€涓竟鐣屾竻鏅版ā鍧楋紝楠岃瘉娴佺▼涓庤鍒欐墽琛屾晥鏋?
- **P0**锛氳竟鐣屾竻鏅般€佹敹鐩婇珮銆侀闄╀綆妯″潡锛堜紭鍏堬級
- **P1**锛氫腑绛夊鏉傛ā鍧?+ 娣峰悎娴嬭瘯鎷嗗垎
- **P2**锛氬鏉傚崗浣滄ā鍧?/ 闆嗘垚娴嬭瘯鏁寸悊

### 4.2 褰撳墠杩涘害姒傝
| 闃舵 | 鐘舵€?| 宸插畬鎴愯疆娆?| 澶囨敞 |
|---|---|---:|---|
| Pilot | `杩涜涓璥 | `1` | 宸插畬鎴?`IdUtils` 璇曠偣棣栬疆涓?`IdTest` 鍘婚噸锛堥儴鍒嗘垚鍔燂級 |
| P0 | `杩涜涓璥 | `7` | 宸插畬鎴?`IdUtils` 娈嬩綑瀹¤銆佹贩鍚堟媶鍒嗘敹灏句笌 Legacy 鍘婚噸 Batch-1/2锛涘緟杞悜涓嬩竴妯″潡鎴栧叾浠栧幓閲嶉」 |
| P1 | `TODO` | `TODO` | `DateTime` / `Reflection` / 娣峰悎娴嬭瘯鎷嗗垎 |
| P2 | `TODO` | `TODO` | `Http` / `Drawing*` / Integration |

---

## 5. 鎶ュ憡绱㈠紩锛堟寜鏃堕棿鍊掑簭鎴栭樁娈靛垎缁勶級

> 寤鸿姣忔柊澧炰竴杞姤鍛婂悗锛屾洿鏂版湰鑺傘€?

### 5.1 Pilot锛堣瘯鐐癸級
| 鏃ユ湡 | 鎶ュ憡鏂囦欢 | 妯″潡/鑼冨洿 | 绫诲瀷 | 缁撴灉 | 鎽樿 |
|---|---|---|---|---|---|
| `2026-02-25` | `2026-02-25-idutils-pilot.md` | `IdUtils锛圔ing.Utils.Tests -> Bing.Utils.IdUtils.Tests锛塦 | Pilot | `閮ㄥ垎鎴愬姛` | 宸茶縼绉?`IdGenerators/*` 3 涓枃浠跺苟閫氳繃缂栬瘧/鏈€灏忔祴璇曪紱宸插畬鎴?`Bing.Helpers.IdTest` 閲嶅瑕嗙洊姣斿涓庡幓閲嶅垹闄?|

---

### 5.2 P0锛堣竟鐣屾竻鏅版ā鍧楋級
| 鏃ユ湡 | 鎶ュ憡鏂囦欢 | 妯″潡/鑼冨洿 | 绫诲瀷 | 缁撴灉 | 鎽樿 |
|---|---|---|---|---|---|
| `2026-02-25` | `2026-02-25-batch-idutils.md` | `IdUtils锛圔ing.Utils.Tests 娈嬩綑瀹¤锛塦 | P0 | `鎴愬姛` | 瀹屾垚杩炵画杩愯鎵规锛氱‘璁ゆ棤鏂板鏂囦欢绾х洿杩侀」锛涘墿浣欓」鍧囦负娣峰悎娴嬭瘯/搴斾繚鐣欓」锛屽苟宸叉洿鏂拌鍒掍笌绱㈠紩 |
| `2026-02-25` | `2026-02-25-mixed-batch-idutils.md` | `IdUtils锛圡ixed Split / UnitTest1锛塦 | P0 | `鎴愬姛` | 瀹屾垚 `UnitTest1` 鏂规硶绾ф媶鍒嗛鎵癸細灏?`Test_Id` 杩佸叆 `Bing.Utils.IdUtils.Tests`锛屽苟閫氳繃缂栬瘧涓庢渶灏忔祴璇曢獙璇?|
| `2026-02-25` | `2026-02-25-mixed-batch-idutils-2.md` | `IdUtils锛圡ixed Split 鏀跺熬澶嶅锛塦 | P0 | `鎴愬姛` | 瀹屾垚 `UnitTest1` 鍓╀綑 IdUtils 鏂规硶澶嶅锛氭棤鏂板鍙媶鍒嗛」锛涘叧闂?`IdUtils` Mixed-Batch-2 |
| `2026-02-25` | `2026-02-25-batch-idutils-legacy-dedupe-1.md` | `IdUtils锛圠egacy 鍘婚噸 Batch-1锛塦 | P0 | `鎴愬姛` | 鍒犻櫎 `ObjectId/TimestampId` 涓や釜楂橀噸鍙?Legacy 娴嬭瘯鏂囦欢锛屽苟鐢?`IdTest` 6 涓浛浠ｈ鐩栫敤渚嬪畬鎴愰獙璇?|
| `2026-02-25` | `2026-02-25-batch-idutils-legacy-dedupe-2.md` | `IdUtils锛圠egacy 鍘婚噸 Batch-2锛塦 | P0 | `鎴愬姛` | 鍒犻櫎 `Snowflake` Legacy 娴嬭瘯鏂囦欢锛屽苟鐢?`IdSnowflakeTest + SnowflakeGeneratorConcurrencyContractTest` 5 涓浛浠ｈ鐩栫敤渚嬪畬鎴愰獙璇?|
| 2026-02-26 | 2026-02-26-batch-text-collections-datetime-reflection-drawing-http-b1.md | Multi-module Batch-1（Collections） | P0 | 成功 | 审计 8 个目标模块并自动拆分子批次；已完成 Collections/ArrayShortcutTests 文件级迁移与最小验证 |
| 2026-02-26 | 2026-02-26-batch-text-collections-datetime-reflection-drawing-http-b2.md | Multi-module Batch-2（Collections） | P0 | 成功 | 完成 CollUT 剩余 3 个文件（ArrayCopy/ArrayEmpty/ArrayTo）迁移与验证，BingUtilsUT/CollUT 目录清空 |
| `TODO` | `TODO` | `TODO` | P0 | `TODO` | `TODO` |

---

### 5.3 P1锛堜腑绛夊鏉?/ 娣峰悎娴嬭瘯鎷嗗垎锛?
| 鏃ユ湡 | 鎶ュ憡鏂囦欢 | 妯″潡/鑼冨洿 | 绫诲瀷 | 缁撴灉 | 鎽樿 |
|---|---|---|---|---|---|
| `TODO` | `TODO` | `DateTime + Reflection` | P1 | `TODO` | `TODO` |
| `TODO` | `TODO` | `Mixed test split` | P1 | `TODO` | `TODO` |

---

### 5.4 P2锛堝鏉傚崗浣?/ 闆嗘垚娴嬭瘯锛?
| 鏃ユ湡 | 鎶ュ憡鏂囦欢 | 妯″潡/鑼冨洿 | 绫诲瀷 | 缁撴灉 | 鎽樿 |
|---|---|---|---|---|---|
| `TODO` | `TODO` | `Http + Drawing` | P2 | `TODO` | `TODO` |

---

## 6. 鍏抽敭鎸囨爣锛堝彲閫夛紝鎸佺画鏇存柊锛?

> 鏈妭鐢ㄤ簬闀挎湡瑙傚療杩佺Щ鏁堟灉锛屽彲鎸夐渶缁存姢銆?

### 6.1 褰掔被杩佺Щ缁熻
| 鎸囨爣 | 鏁板€?| 璇存槑 |
|---|---:|---|
| 宸茶縼绉绘祴璇曟枃浠舵暟 | `TODO` | 绱 |
| 宸茶縼绉绘祴璇曠被鏁?| `TODO` | 绱 |
| 宸叉媶鍒嗘贩鍚堟祴璇曠被鏁?| `TODO` | 绱 |
| 寰呬汉宸ョ‘璁ら」鏁?| `TODO` | 褰撳墠鏈鐞?|
| 閬楃暀娣峰悎娴嬭瘯绫绘暟 | `TODO` | 褰撳墠鏈鐞?|

### 6.2 椋庨櫓鏀舵暃鎯呭喌
- [ ] `Bing.Utils.Tests` 涓ā鍧椾笓灞炴祴璇曟樉钁楀噺灏?
- [ ] 妯″潡涓撳睘娴嬭瘯椤圭洰杈圭晫鏇存竻鏅?
- [ ] 娣峰悎娴嬭瘯鎷嗗垎绛栫暐绋冲畾
- [ ] 鍏变韩娴嬭瘯杈呭姪绫绘娊鍙栨柟妗堝凡钀藉湴锛堝閫傜敤锛?
- [ ] CI 鍙寜妯″潡鍒嗗眰鎵ц娴嬭瘯锛堝宸查厤缃級

---

## 7. 甯歌鎵ц娴佺▼锛堜緵澶嶇敤锛?

### 7.1 姣忚疆杩佺Щ寤鸿娴佺▼
1. 闃呰瑙勫垯锛坄AGENTS.md` + Skill 瑙勫垯锛?
2. 璇诲彇 `docs/quality/test-reclassification-plan.md`
3. 杈撳嚭鏈疆杩佺Щ娓呭崟锛堝彲鐩存帴杩佺Щ / 娣峰悎 / 寰呯‘璁わ級
4. 鎵ц杩佺Щ涓庢渶灏忎慨澶?
5. 杩愯鏈€灏忛獙璇侊紙缂栬瘧 / 娴嬭瘯锛?
6. 鐢熸垚杩佺Щ楠岃瘉鎶ュ憡锛堟湰鐩綍锛?
7. 鏇存柊鏈储寮曪紙澧炲姞涓€鏉℃姤鍛婅褰曪級
8. 鏇存柊瑙勫垝鏂囨。鎵ц璁板綍绔犺妭

### 7.2 绂佹浜嬮」锛堟彁閱掞級
- 涓嶈涓€娆℃€у叏閲忚縼绉诲叏浠撳簱
- 涓嶈璺宠繃鈥滆縼绉绘竻鍗曗€濈洿鎺ユ敼浠ｇ爜
- 涓嶈寮鸿杩佺Щ娣峰悎娴嬭瘯绫?
- 涓嶈鍦ㄦ湭鎺堟潈鎯呭喌涓嬩慨鏀圭敓浜т唬鐮?

---

## 8. 鍚庣画浼樺寲浜嬮」锛堥暱鏈燂級
- [ ] 寤虹珛 `tests/Common` 鍏变韩娴嬭瘯鍩虹璁炬柦锛堝閫傜敤锛?
- [ ] 澧炲姞娴嬭瘯褰掑睘瀹¤鑴氭湰/妫€鏌ラ」锛堥槻姝㈠洖娴侊級
- [ ] 寤虹珛 defect-to-test 鍥炲綊鏄犲皠
- [ ] 涓烘ā鍧椾笓灞炴祴璇曢」鐩ˉ鍏?README锛堟祴璇曡寖鍥淬€佽繍琛屾柟寮忋€佺害鏉燂級
- [ ] 灏嗚縼绉荤粨鏋滃悓姝ュ洖 `docs/quality/test-reclassification-plan.md`

---

## 9. 缁存姢璇存槑
- 鏈枃浠朵负杩佺Щ鎶ュ憡绱㈠紩锛岃褰曗€滄瘡杞姤鍛婂叆鍙ｄ笌缁撴灉鎽樿鈥?
- 璇︾粏鎵ц鍐呭璇锋煡鐪嬪悇杞姤鍛婃枃浠?
- 鑻ヨ鍒欐枃妗ｄ綅缃彂鐢熷彉鍖栵紝璇蜂紭鍏堟洿鏂?`AGENTS.md` 涓?Skill 鍐呭紩鐢ㄨ矾寰?


