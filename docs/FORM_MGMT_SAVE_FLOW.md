# Form Management — Save Flow & Mail-Merge Pipeline

> Tài liệu này tổng hợp lại bài toán, các thuật toán đang dùng, code flow, và những bug đã đạp + cách fix trong quá trình build feature Form Management (insurance template editor với mail-merge).
>
> Stack: React 18 + Vite + TypeScript (frontend-react) — OnlyOffice DocumentServer 9.3.1 (docker) — .NET 8 (BE module FormManagement) — PostgreSQL.

---

## 1. Bài toán

**Mô tả nghiệp vụ:** hệ thống insurance cần cho phép user (1) tạo / chỉnh sửa template hợp đồng dưới dạng DOCX, (2) chèn các field metadata (vd `BSO_HD`, `CTEN`, `CCHUC_VU`, `BNGAY_CAP`, ...) như placeholders, (3) sau đó với mỗi submission user nhập data vào form và BE mail-merge ra file output đã thay thế các field bằng giá trị thật.

**Pain point cụ thể đã được giải quyết:**

1. Khi user chèn field qua plugin / sidebar / `@mention` trong editor OnlyOffice, OnlyOffice chỉ paste plain text `«FIELDNAME»` (không phải real OOXML MERGEFIELD). Plain text «...» mail-merge được nhưng không có "field shading", không Alt+F9 toggle được — không đúng kỳ vọng người dùng nhập liệu.
2. `usedFields` (list các field code dùng trong template — dùng để generate form data-entry phía FE) bị stale: sau khi save bytes từ DocServer xuống BE, BE chỉ replace bytes mà không re-detect → field mới chèn không xuất hiện trong form submission → user không có chỗ điền → mail-merge để nguyên `«FIELDNAME»`.
3. Nút "Lưu template" trong header không fire DS save callback → user bấm Save + F5 thì field vừa chèn vẫn plain, không phải MERGEFIELD.
4. Bất kỳ approach unmount/remount DocumentEditor React component nào (set key prop, conditional render, destroyEditor + React unmount) đều gây `NotFoundError: Failed to execute 'removeChild' on 'Node'` → whitescreen toàn page.

**Mục tiêu cuối:** 

- (a) Cả Office Save toolbar lẫn "Lưu template" button đều phải fire DS callback → BE wrap + persist DB.
- (b) Sau save, editor tự reload bytes mới (đã wrap MERGEFIELD) **in-place** — không nav-away, không popup "Đã thay đổi phiên bản", không whitescreen, không yêu cầu user F5.
- (c) Submission page generate form đúng theo `usedFields` mới nhất, bao gồm cả field user vừa chèn.
- (d) Mail-merge produce DOCX đầy đủ — substitute mọi field user fill, vi-VN format đúng (currency `I*` → dấu chấm ngàn, date `*NGAY/THANG/NAM` → dd/MM/yyyy parts).

---

## 2. Kiến trúc tổng

```
┌─────────────────────────────────────────────────────────────────────┐
│ Browser                                                              │
│ ┌──────────────────┐        ┌──────────────────────────────────────┐ │
│ │ React app        │        │ OnlyOffice DocEditor iframe          │ │
│ │ TemplateEditor   │ ◀────► │ (DS-served, cross-origin)            │ │
│ │  - saveTemplate  │ postMsg│  ┌──────────────────────────────────┐ │ │
│ │  - poller(3s)    │        │  │ FormMgmt plugin (same-origin     │ │ │
│ │  - refreshFile() │        │  │  iframe → host postMessage)      │ │ │
│ │  - sidebar       │        │  │  - PasteText «NAME»              │ │ │
│ └────────┬─────────┘        │  │  - @mention detect               │ │ │
│          │                  │  └──────────────────────────────────┘ │ │
│          │ HTTP             └──────────────────────────────────────┘ │
└──────────┼─────────────────────────────────────┬──────────────────────┘
           │                                     │
           ▼ (Authorization Bearer)              ▼ (cross-container HTTP)
┌──────────────────────────────────────┐   ┌────────────────────────────┐
│ BE (.NET 8 - jira-clone-api)         │   │ OnlyOffice DocServer 9.3.1 │
│                                       │   │ (jira-clone-onlyoffice)    │
│ TemplatesController                   │   │                            │
│  ├─ GET    /{id}/file (anonymous)    ◀───┤  DocsAPI.DocEditor mount   │
│  ├─ POST   /{id}/callback (anonymous)│───►  flush cache→ POST callback │
│  ├─ POST   /{id}/trigger-save        │   │                            │
│  │   └─→ proxy CommandService.ashx ──┼──►  c=forcesave                │
│  ├─ POST   /{id}/normalize-fields    │   │                            │
│  ├─ POST   /submissions/             │   │                            │
│  │   submit-and-export               │   │                            │
│  └─ /import, /content, /metadata     │   │                            │
│                                       │   │                            │
│ FormManagement.Application.Services   │   └────────────────────────────┘
│  ├─ TemplateService                   │
│  │   - CRUD                           │
│  │   - ReplaceDocxBytesAsync          │ ◀── callback path
│  │       └─ ExtractUsedFields refresh │
│  ├─ SubmissionService                 │
│  │   - mail-merge orchestration       │
│  └─ IDocumentConversionService        │
│                                       │
│ FormManagement.Infrastructure         │
│  └─ OpenXmlDocumentConversionService  │
│      ├─ WrapGuillemetsAsMergeFields   │ (per-Run + fldChar depth tracking)
│      ├─ ExtractUsedFields             │ (instrText ∪ guillemet text)
│      └─ MailMergeAsync                │ (Strategy 1 fldChar walk + Strategy 2 regex)
└──────────────────────────────────────┘
                  │
                  ▼
        PostgreSQL form_mgmt schema
        ├─ templates (docx_bytes BLOB, used_fields_json text, version int)
        ├─ submissions
        └─ metadata
```

---

## 3. Thuật toán chính

### 3.1. WrapGuillemetsAsMergeFields — per-Run wrap với fldChar depth tracking

**Đầu vào:** DOCX bytes có thể chứa `«FIELDNAME»` plain text rải rác (có thể trong cùng paragraph với MERGEFIELD đã wrapped từ trước).

**Đầu ra:** DOCX bytes đã wrap mọi plain `«FIELDNAME»` (matching `[A-Za-z][A-Za-z0-9_]*`) thành real OOXML MERGEFIELD structure:

```xml
<w:r>            <!-- begin -->
  <w:rPr>…clone từ run gốc</w:rPr>
  <w:fldChar w:fldCharType="begin"/>
</w:r>
<w:r>            <!-- instrText -->
  <w:rPr>…</w:rPr>
  <w:instrText xml:space="preserve"> MERGEFIELD FIELDNAME \* MERGEFORMAT </w:instrText>
</w:r>
<w:r>            <!-- separate -->
  <w:rPr>…</w:rPr>
  <w:fldChar w:fldCharType="separate"/>
</w:r>
<w:r>            <!-- display -->
  <w:rPr>…</w:rPr>
  <w:t xml:space="preserve">«FIELDNAME»</w:t>
</w:r>
<w:r>            <!-- end -->
  <w:rPr>…</w:rPr>
  <w:fldChar w:fldCharType="end"/>
</w:r>
```

**Idempotency yêu cầu:** đã wrap rồi gọi lại không được tạo wrapper chồng wrapper.

**Algorithm:**

```
for each paragraph P in body + headers + footers:
    runs = P.Elements<Run>()
    depth = 0
    startsInField[i] = (depth > 0 khi vào run i)
    foreach run in runs:
        startsInField[i] = (depth > 0)
        foreach fldChar in run.Descendants<FieldChar>():
            if Begin → depth++
            else if End → depth = max(0, depth-1)
    
    for each run i in runs:
        if startsInField[i] → SKIP        # nằm trong MERGEFIELD display range
        if run có FieldChar hoặc FieldCode → SKIP   # là scaffolding run
        concat = run.Descendants<Text>() joined
        matches = pattern.Matches(concat)
        if matches empty → continue
        
        # rebuild run thành sequence [plain-before, MF-runs, plain-between, MF-runs, …, plain-after]
        rPrTemplate = run.RunProperties (clone)
        newElements = []
        cursor = 0
        foreach match in matches:
            if match.Index > cursor → newElements.add(BuildPlainRun(concat[cursor..match.Index], rPrTemplate))
            newElements.addAll(BuildMergeFieldRuns(match.Groups[1].Value, rPrTemplate))
            cursor = match.Index + match.Length
        if cursor < concat.Length → newElements.add(BuildPlainRun(concat[cursor..], rPrTemplate))
        
        # insert sau run hiện tại theo thứ tự, rồi remove run cũ
        prev = run
        foreach el in newElements: parent.InsertAfter(el, prev); prev = el
        run.Remove()
```

**Key insight:** thay vì xử lý **whole paragraph** (như approach cũ — concat all Text, rebuild all Runs → vỡ existing MERGEFIELD scaffolding), xử lý **per-Run**:
- Run nào nằm trong display range của MERGEFIELD cũ (`depth > 0` khi vào) → SKIP, giữ nguyên display text «FIELDNAME».
- Run nào là fldChar/instrText scaffolding → SKIP, không động vào.
- Run nào là plain text outside fldChar range → wrap các match «...» trong text của nó.

**Trade-off:** nếu OnlyOffice split `«FIELDNAME»` thành **nhiều Run** liên tiếp (Run1=`«FIELD`, Run2=`NAME»`), per-Run match sẽ miss → giữ plain. Mail-merge regex Strategy 2 vẫn substitute đúng (concat per-paragraph) nên output cuối không hỏng.

### 3.2. ExtractUsedFields — detect tất cả field codes

**Đầu vào:** DOCX bytes.

**Đầu ra:** `IReadOnlyList<string>` — list unique field code, giữ thứ tự xuất hiện đầu tiên, filter validid `^[A-Za-z][A-Za-z0-9_]*$`.

**Strategy:** union 2 nguồn:
1. **MERGEFIELD instrText scan**: walk `Descendants<FieldCode>()`, regex `\s*MERGEFIELD\s+(\S+)` → field code.
2. **Plain `«NAME»` scan**: walk paragraphs + headers + footers, concat all `Text.Text` per paragraph với `\n` separator, regex `«([A-Za-z]\w*)»`.

**Regex chọn `«([A-Za-z]\w*)»` thay vì `«([^»]+)»`:**

```
Khi OnlyOffice split MERGEFIELD display run và user paste «CEMAIL» VÀO GIỮA, concat thành:
  "«CCHUC_VU«CEMAIL»"

Greedy regex `«([^»]+)»` match nguyên cụm "«CCHUC_VU«CEMAIL»" → "CCHUC_VU«CEMAIL" fail identifier filter → CEMAIL MẤT.

Strict regex `«([A-Za-z]\w*)»` match đúng "«CEMAIL»" vì giữa « và » phải toàn identifier chars, không cho phép « lồng.
```

### 3.3. Mail-merge — dual-strategy substitution

**Đầu vào:** DOCX template + `Dictionary<string, object?>` data.

**Đầu ra:** DOCX với mọi field code được thay bằng formatted value.

**Pre-format theo prefix/suffix tên field (vi-VN):**
- `I*` (vd `ITONG_PHI`) → currency: `123456789` → `123.456.789` (dấu chấm ngàn, không trailing decimals).
- `*NGAY` (vd `BNGAY_CAP`) → `dd` (2 chữ số ngày).
- `*THANG` (vd `BTHANG_HD`) → `MM` (2 chữ số tháng).
- `*NAM` (vd `BNAM_HD`) → `yyyy` (4 chữ số năm).

**Strategy 1 — walk real MERGEFIELDs (fldChar structure):**

```
fieldNameRegex = /\s*MERGEFIELD\s+(?<name>\S+)/i
for each body/header/footer root:
    allRuns = root.Descendants<Run>()
    for i in 0..allRuns.Count:
        beginChar = allRuns[i].fldChar Begin? continue if not
        fieldCode = null, separateIdx = -1, endIdx = -1
        for j from i:
            instrText = allRuns[j].FirstFieldCode → assign fieldCode
            fc = allRuns[j].FirstFieldChar
            if Separate → separateIdx = j
            else if End → endIdx = j; break
        if all 3 found:
            fieldName = match(fieldCode)
            value = dataFormatted[fieldName]
            # gom các Text giữa separateIdx+1 và endIdx-1, set value vào Text đầu tiên, xoá rest
            first = true
            for j in separateIdx+1 .. endIdx-1:
                foreach t in allRuns[j].Texts:
                    if first → t.Text = value; first = false
                    else → t.Text = ""
```

**Strategy 2 — regex replace plain `«FIELDNAME»` (fallback):**

```
fieldPattern = /«([A-Za-z]\w*)»/  ← strict identifier, đồng bộ với ExtractUsedFields
for each paragraph in body/header/footer:
    texts = paragraph.Descendants<Text>()
    concat = string.Concat(texts.map(t => t.Text))
    if not contains '«' → continue
    replaced = fieldPattern.Replace(concat, m => dataFormatted[m.Groups[1].Value] ?? m.Value)
    if replaced == concat → continue
    texts[0].Text = replaced
    texts[0].Space = Preserve
    for i in 1..: texts[i].Text = ""
```

**Tại sao 2 strategy?** Real MERGEFIELD (import từ Word .docx) đi qua Strategy 1, plain text «...» (user paste qua plugin) đi qua Strategy 2. WrapGuillemetsAsMergeFields có thể đã convert plain → wrapped khi callback save, nhưng phòng trường hợp wrap miss (split-run edge case), Strategy 2 vẫn quét sạch.

### 3.4. Save flow — DocServer CommandService proxy

**Trước fix (broken):** FE gọi `docEditor.serviceCommand('force-save', {})` trên DS 9.3.1 → silent fail (không throw, callback không fire, DB version không bump).

**Sau fix:**

```
FE saveTemplate() {
    1. Build docKey = `${template.id}-v${template.version}-${reloadKey}`
    2. POST /api/v1/form-management/templates/{id}/trigger-save
         body: { docKey }
    3. BE proxy POST http://onlyoffice/coauthoring/CommandService.ashx
         body: { c: "forcesave", key: docKey }
    4. DS response: { error: 0 } accepted (async — callback sẽ fire sau ~500-2000ms)
                   { error: 4 } no changes
                   { error: 1 } key not found (session chưa mở)
    5. FE poll GET /templates/{id} mỗi 200ms (max 6s) chờ version > startVersion
    6. Khi version bump → docEditor.refreshFile({
            document: { fileType, key: newDocKey, title, url: fileUrl }
       })
    7. OnlyOffice reload bytes mới vào iframe in-place → NO unmount React → NO crash
}
```

**Server-side flow song song (DS → BE):**

```
DocServer nhận c=forcesave:
    flush cached doc → fire POST /api/v1/form-management/templates/{id}/callback
    payload { status: 6, url: "http://localhost:8080/cache/files/.../output.docx", ... }

BE OnlyOfficeCallback handler:
    if status in {2, 6} and url not empty:
        rewrite url: http://localhost:8080 → http://onlyoffice (docker DNS)
        docxBytes = await http.GetByteArrayAsync(fetchUrl)
        docxBytes = WrapGuillemetsAsMergeFields(docxBytes)   ← per-Run wrap
        await _service.ReplaceDocxBytesAsync(id, docxBytes)
              └─ extractedFields = ExtractUsedFields(docxBytes)
              └─ template.UpdateContent(bytes, JSON.Serialize(extractedFields))
              └─ version++
    return { error: 0 }   ← required ACK
```

### 3.5. Background poller — auto-reload sau Office Save

Office Save (built-in toolbar OnlyOffice) không đi qua FE saveTemplate — DS tự fire callback. FE không có signal trực tiếp → poll detect:

```
useEffect mỗi 3 giây:
    if saveBusy → skip (saveTemplate handler đang chạy, tự refresh)
    fresh = await templateApi.getById(tplId)
    if fresh.version > lastSeenVersion:
        lastSeenVersion = fresh.version
        queryClient.setQueryData(['template', tplId], fresh)
        inst.refreshFile({
            document: { key: `${tplId}-v${fresh.version}-${reloadKey}`, url: fileUrl, ... }
        })
```

---

## 4. Code flow chi tiết

### 4.1. User click "Lưu template" button

```
TemplateEditorPage.saveTemplate()
  ├─ setSaveBusy(true)                              ← UI overlay spinner
  ├─ docKey = `${id}-v${version}-${reloadKey}`
  ├─ resp = await templateApi.triggerSave(id, docKey)
  │     POST /api/v1/form-management/templates/{id}/trigger-save
  │      ├─ TemplatesController.TriggerSave
  │      │    └─ HttpClient POST http://onlyoffice/coauthoring/CommandService.ashx
  │      │       body: { c: "forcesave", key: docKey }
  │      │       ─ DS responds { error: 0|1|4 }
  │      └─ return { dsStatus, dsBody }
  │
  ├─ ASYNC (DS background):
  │     DS flush cache → POST /api/v1/form-management/templates/{id}/callback
  │       status: 6 (forcesave), url: cache_url
  │       ├─ TemplatesController.OnlyOfficeCallback
  │       │    ├─ rewrite localhost:8080 → onlyoffice (docker DNS)
  │       │    ├─ fetch bytes
  │       │    ├─ WrapGuillemetsAsMergeFields(bytes)
  │       │    │    └─ per-Run + fldChar depth → wrap «...» thành MERGEFIELD
  │       │    └─ TemplateService.ReplaceDocxBytesAsync
  │       │         ├─ ExtractUsedFields(bytes)
  │       │         │    └─ MERGEFIELD instrText scan + «([A-Za-z]\w*)» scan
  │       │         └─ template.UpdateContent → version++, persist DB
  │       └─ return { error: 0 }
  │
  ├─ FE POLL LOOP (parallel với DS callback):
  │     for i in 0..30:
  │         await sleep(200ms)
  │         fresh = await templateApi.getById(id)
  │         if fresh.version > startVersion → break
  │
  ├─ bumpedTemplate = fresh    ← version mới (đã wrap)
  ├─ queryClient.setQueryData(['template', id], fresh)
  ├─ newDocKey = `${id}-v${fresh.version}-${reloadKey}`
  ├─ inst.refreshFile({
  │     document: { fileType: 'docx', key: newDocKey, title, url: fileUrl },
  │     editorConfig: { callbackUrl }
  │   })   ← OnlyOffice load bytes mới in-place, React không unmount
  │
  └─ setSaveBusy(false)
```

### 4.2. User click Office Save trong toolbar OnlyOffice (Ctrl+S / floppy disk icon)

```
DocServer toolbar Save:
  ├─ DS flush cache → POST /callback {status: 6, url: ...}
  │   (đi qua flow BE giống 4.1 phần ASYNC)
  └─ DB version bump

Background poller (mỗi 3s):
  ├─ fresh = await templateApi.getById(id)
  ├─ if fresh.version > lastSeenVersion:
  │     ├─ lastSeenVersion = fresh.version
  │     ├─ queryClient.setQueryData(['template', id], fresh)
  │     └─ inst.refreshFile({ document: { key: newDocKey, url: fileUrl } })
  │            ← editor reload in-place, user thấy MERGEFIELD ngay
  └─ (else loop tiếp)
```

### 4.3. User submit data → mail-merge → download

```
SubmissionPage:
  ├─ Load template via /templates/{id} → template.usedFields list
  ├─ For each field in usedFields:
  │     Map sang metadata → render input (text/date/number/textarea theo type)
  ├─ User fill data → values: Record<string, string>
  ├─ Click "Mail-merge + tải DOCX"
  │   POST /api/v1/form-management/submissions/submit-and-export
  │     body: { templateId, exportFormat: 2 (Docx), data: values }
  │     ├─ SubmissionService.SubmitAndExportAsync
  │     │    ├─ load template (DocxBytes)
  │     │    └─ _conversion.MailMergeAsync(bytes, data, Docx)
  │     │         ├─ FormatFieldValue per field theo prefix/suffix
  │     │         │   (I* → currency, *NGAY → dd, *THANG → MM, *NAM → yyyy)
  │     │         ├─ Strategy 1: walk fldChar structure, replace display text
  │     │         ├─ Strategy 2: regex «([A-Za-z]\w*)» per-paragraph replace
  │     │         └─ Save modified bytes
  │     └─ return DOCX FileContentResult
  │
  └─ Browser auto-download `{code}-merged.docx`
```

---

## 5. Bug catalog (đã đạp + fix trong session)

### Bug #1 — Idempotency guard skipping whole paragraph

**Triệu chứng:** user chèn «CEMAIL» vào paragraph đã có MERGEFIELD «CCHUC_VU» → save → reload → «CEMAIL» vẫn plain text, không thành MERGEFIELD.

**Root cause:** version cũ của `WrapInParagraph` kiểm tra `if (para.Descendants<FieldChar>().Any()) return;` → skip toàn paragraph nếu có bất kỳ MERGEFIELD nào. Plain «CEMAIL» chèn vào đó → bị bỏ qua mãi mãi.

**Fix:** rewrite per-Run với fldChar depth tracking (xem §3.1). Mỗi Run được phân loại độc lập: chỉ skip Run nằm trong display range của MERGEFIELD cũ, không skip whole paragraph.

### Bug #2 — Greedy regex `«([^»]+)»` ăn inner «

**Triệu chứng:** sau bug #1 fix, save vẫn không pick up «CEMAIL». ExtractUsedFields chạy xong trả 11 fields (không có CEMAIL) dù XML rõ ràng có CEMAIL.

**Root cause:** OnlyOffice split display run của MERGEFIELD «CCHUC_VU» thành 2 `w:t` ("«CCHUC_VU" và "»") rồi user dùng plugin PasteText chèn «CEMAIL» VÀO GIỮA. Concat per-paragraph thành `«CCHUC_VU«CEMAIL»` — regex greedy `«([^»]+)»` bắt nguyên cụm thành 1 match name `CCHUC_VU«CEMAIL` → fail identifier filter → CEMAIL bị "nuốt".

**Fix:** đổi regex sang strict identifier `«([A-Za-z]\w*)»` ở 2 chỗ:
- `ExtractUsedFields.guillemetRegex`
- `ReplaceGuillemetsText.fieldPattern` (Strategy 2 mail-merge)

`[A-Za-z]\w*` không cho phép « giữa name → match đúng `«CEMAIL»`.

### Bug #3 — Stale `used_fields_json` sau callback save

**Triệu chứng:** ngay cả khi wrap đúng, SubmissionPage không hiển thị input cho field mới chèn.

**Root cause:** `TemplateService.ReplaceDocxBytesAsync` chỉ replace `DocxBytes`, giữ nguyên `UsedFieldsJson` cũ → list field stale.

**Fix:** trong `ReplaceDocxBytesAsync` gọi `_conversion.ExtractUsedFields(docxBytes)` trước, serialize JSON, pass vào `template.UpdateContent(bytes, usedFieldsJson)`. Mỗi save tự refresh list.

### Bug #4 — `serviceCommand('force-save')` silent fail trên DS 9.3.1

**Triệu chứng:** click "Lưu template" trong header, console log "save triggered via serviceCommand: true", nhưng API log không nhận callback nào, DB version đứng yên.

**Root cause:** trên DocServer 9.3.1, `docEditor.serviceCommand('force-save', {})` từ host (cross-origin iframe) không thực sự trigger force-save. Method tồn tại, không throw, nhưng silent ignore.

**Fix:** dùng DocServer HTTP CommandService API thay vì serviceCommand. BE proxy: `POST http://onlyoffice/coauthoring/CommandService.ashx { c: "forcesave", key }`. DS này chính là cơ chế nội bộ DS dùng khi user bấm Save trong toolbar → reliable.

### Bug #5 — `removeChild` crash khi unmount DocumentEditor

**Triệu chứng:** thử nhiều approach để remount editor (set `key` prop, conditional render `{!saveBusy && <DocumentEditor/>}`, gọi `destroyEditor()` thủ công) — đều dẫn đến `NotFoundError: Failed to execute 'removeChild' on 'Node'` → React crash → whitescreen toàn page.

**Root cause:** `@onlyoffice/document-editor-react` có race condition trong useEffect cleanup: library tự gọi `destroyEditor()` (xoá iframe DOM) trước khi React commit phase chạy `commitDeletion` để remove các DOM nodes. React thấy iframe đã bị xoá → `parent.removeChild(iframe)` throw `NotFoundError`.

**Fix:** không unmount DocumentEditor. Dùng `docEditor.refreshFile(config)` để reload bytes vào instance hiện tại in-place. React component giữ mounted, OnlyOffice library tự handle iframe lifecycle bên trong.

### Bug #6 — Popup "Đã thay đổi phiên bản" sau remount

**Triệu chứng:** approach nav-away + nav-back để force unmount → OnlyOffice phát hiện docKey reuse với content khác → popup "Phiên bản tệp này đã được thay đổi. Trang này sẽ được tải lại."

**Root cause:** docKey không thay đổi khi version bump trong DS cache (DS dựa trên `editorConfig.document.key`). Nếu mount lại với docKey cũ mà BE bytes đã đổi (do callback save) → DS phát hiện inconsistency → popup.

**Fix:** không reuse docKey. Khi gọi `refreshFile`, build `newDocKey = `${id}-v${freshVersion}-${reloadKey}`` — version mới → docKey mới → DS treat as new session → no popup.

### Bug #7 — Mail-merge bỏ sót CEMAIL trong split-run paragraph

**Triệu chứng:** sau khi ExtractUsedFields fix (Bug #2), submission page có input CEMAIL → user fill → submit. Output DOCX hiển thị giá trị CCHUC_VU thay đúng, CEMAIL vẫn `«CEMAIL»` không substitute.

**Root cause:** mail-merge Strategy 2 dùng cùng regex cũ `«([^»]+)»` → cùng bug greedy như #2 nhưng ở vị trí khác trong code.

**Fix:** đồng bộ regex `«([A-Za-z]\w*)»` ở cả `ExtractUsedFields` lẫn `ReplaceGuillemetsText`.

### Bug #8 — DS callback URL "localhost:8080" không reach được từ container BE

**Triệu chứng:** trong DS callback, `payload.url` dạng `http://localhost:8080/cache/files/.../output.docx`. BE container fetch URL này → ECONNREFUSED.

**Root cause:** "localhost" trong perspective của DS container ám chỉ DS container itself. BE container không reach được localhost của DS.

**Fix:** trong `OnlyOfficeCallback` handler, rewrite URL: `http://localhost:8080` → `http://onlyoffice` (docker service name resolve qua docker DNS).

---

## 6. Files thay đổi chính

### Backend (`.NET 8`)

- `src/Modules/FormManagement/FormManagement.Api/TemplatesController.cs`:
  - `TriggerSave(id, payload)` — POST `/trigger-save`, proxy DS CommandService.
  - `OnlyOfficeCallback(id, payload)` — POST `/callback`, fetch bytes + WrapGuillemetsAsMergeFields + ReplaceDocxBytesAsync. URL rewrite `localhost:8080` → `onlyoffice`.
  - `NormalizeFields(id)` — admin endpoint chạy wrap thủ công.
- `src/Modules/FormManagement/FormManagement.Application/Services/TemplateService.cs`:
  - `ReplaceDocxBytesAsync` — gọi `ExtractUsedFields` để refresh `UsedFieldsJson` mỗi callback save.
- `src/Modules/FormManagement/FormManagement.Application/Services/IDocumentConversionService.cs`:
  - Thêm interface method `IReadOnlyList<string> ExtractUsedFields(byte[] docxBytes)`.
- `src/Modules/FormManagement/FormManagement.Infrastructure/OpenXmlDocumentConversionService.cs`:
  - `WrapGuillemetsAsMergeFields` — body + headers + footers, per-Run + fldChar depth tracking.
  - `WrapInParagraph` (private) — pre-compute `startsInField` flags, wrap qualified Run, skip in-field/scaffolding Runs.
  - `ExtractUsedFields` — union scan instrText + plain «...» strict identifier regex.
  - `ReplaceGuillemetsText` (mail-merge Strategy 2) — strict identifier regex.

### Frontend (`React + Vite`)

- `frontend-react/src/api/template.ts`:
  - `templateApi.triggerSave(id, docKey)` — POST `/trigger-save`.
- `frontend-react/src/pages/TemplateEditorPage.tsx`:
  - `saveTemplate()` — async, gọi triggerSave + poll version bump + `inst.refreshFile()`.
  - `useEffect` background poller mỗi 3s — detect Office Save / autosave version bump → auto `refreshFile`.
  - DocumentEditor render: KHÔNG dùng `key` prop, chỉ `id` prop (để library tự handle iframe lifecycle).
  - Overlay "Đang lưu template..." khi `saveBusy=true`.

### Tests

- `tests/UnitTests/FormManagement.UnitTests/FormManagement.UnitTests.csproj` — xUnit project.
- `tests/UnitTests/FormManagement.UnitTests/WrapGuillemetsTests.cs` — 11 test cases:
  1. Fresh wrap (plain «...» → MERGEFIELD).
  2. Idempotency (3 passes — counts không đổi).
  3. Mixed doc (MERGEFIELD cũ + plain mới ở para khác → cả 2 wrap đúng).
  4. Invalid pattern «Mã KH» (space) → giữ plain text.
  5. No «...» → no-op.
  6. ExtractUsedFields union MERGEFIELD + plain.
  7. Same field nhiều lần → wrap hết, dedup ở extract list.
  8. Split display run + plain insert (Bug #2 regression) — ExtractUsedFields strict regex pick up CEMAIL.
  9. Mail-merge same scenario — substitute CEMAIL đúng (Bug #7).
  10. Mixed paragraph với MERGEFIELD + plain mới (Bug #1) — per-Run wrap mới.
  11. Idempotency post per-Run refactor (3 passes — identical).

---

## 7. Cấu hình & deployment notes

### Docker compose services

```yaml
api:           # .NET 8 BE, build từ docker/Dockerfile.api
  ports: 5000:8080
  env:
    OnlyOffice__ApiBaseUrl: http://api:8080         # DS gọi BE qua docker DNS
    OnlyOffice__DocumentServerUrl: http://localhost:8080/  # FE config

onlyoffice:    # DocServer 9.3.1
  ports: 8080:80
  env:
    JWT_ENABLED: "false"                            # POC
    USE_UNAUTHORIZED_STORAGE: "true"

postgres:      # PostgreSQL 16

web:           # Angular FE (build static, không relevant ở luồng này)
  ports: 4200:80

minio:         # Object storage (chưa dùng)
```

**Frontend dev:** chạy ngoài docker bằng `cd frontend-react && npm run dev` → Vite serve port 3000, proxy `/api` → `http://localhost:5000`.

### DocServer config phải có (đã patch trong container):

```json
// /etc/onlyoffice/documentserver/local.json
{
  "services": {
    "CoAuthoring": {
      "request-filtering-agent": {
        "allowPrivateIPAddress": true,    // cho phép DS fetch từ api:8080 (private IP)
        "allowMetaIPAddress": true
      }
    }
  }
}
```

Sau khi patch: `docker exec jira-clone-onlyoffice supervisorctl restart all`.

### Test thực tế đã chạy qua

- **Unit tests:** 11/11 PASS (xUnit + FluentAssertions, OpenXml SDK ground-truth assertions).
- **End-to-end MCP:** browser MCP test toàn flow login → editor → sidebar insert → Lưu template → in-place refresh → submission page render đủ inputs → mail-merge → output XML check.
- **DB inspection:** verify mỗi step bằng `docker exec jira-clone-postgres psql ... SELECT version, used_fields_json FROM form_mgmt.templates WHERE id=...`.
- **XML inspection:** PowerShell + `Expand-Archive` unzip docx, regex check `fldCharType="begin"` count, `MERGEFIELD\s+\S+` count, balanced begin=sep=end.

---

## 8. Known limitations & next steps

1. **Split-run «...» không được wrap:** nếu OnlyOffice tách `«FIELD»` ra 2+ Run (Run1=`«FIE`, Run2=`LD»`), per-Run wrap miss. Mail-merge Strategy 2 vẫn substitute đúng vì concat per-paragraph. Future: merge consecutive plain Runs trước khi match.
2. **DS JWT chưa bật:** POC. Production phải enable + share secret giữa BE và DS để verify callback authenticity.
3. **Background poller 3s interval:** acceptable cho POC; production nên dùng SSE/WebSocket push từ BE khi version bump để giảm load.
4. **DocServer `error: 4` (no changes) khi trigger-save:** acceptable — không có gì để save, version không bump, FE saveTemplate timeout sau 6s với warning log. Không gây hỏng UX (overlay tắt, editor giữ nguyên).
5. **Mail-merge PDF output:** chưa support (chỉ DOCX). Cần OnlyOffice DocServer convert API.
6. **Auth callback endpoint:** `/callback` đang `[AllowAnonymous]` vì DS không gửi JWT user. POC chấp nhận; production cần IP whitelist hoặc OnlyOffice JWT.

---

## 9. Tham khảo

- OnlyOffice DocServer Command API: <https://api.onlyoffice.com/docs/docs-api/additional-api/command-service/>
- OnlyOffice callback spec: <https://api.onlyoffice.com/docs/docs-api/usage-api/callback-handler/>
- OnlyOffice `refreshFile` method: <https://api.onlyoffice.com/docs/docs-api/usage-api/methods/refresh-file/>
- OOXML MERGEFIELD spec: ECMA-376 §17.16 (Simple Fields + Complex Fields).
- DocumentFormat.OpenXml SDK: <https://github.com/dotnet/Open-XML-SDK>.
