import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  NgZone,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import {
  DocumentEditorContainerComponent,
  DocumentEditorContainerModule,
  ToolbarService,
  SearchService
} from '@syncfusion/ej2-angular-documenteditor';
import { MetadataService } from './metadata.service';
import { MetadataDto } from './metadata.model';
import { MetadataSidebarComponent } from './metadata-sidebar.component';
import { MentionPopupComponent } from './mention-popup.component';
import { DetectedPlaceholder, TemplateService } from './template.service';

@Component({
  selector: 'app-template-editor-page',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    ButtonModule,
    DocumentEditorContainerModule,
    MetadataSidebarComponent,
    MentionPopupComponent
  ],
  providers: [ToolbarService, SearchService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-head">
      <h1>{{ 'form_mgmt.editor.title' | translate }}</h1>
    </div>

    <div class="toolbar">
      <input #fileInput type="file" accept=".sfdt,.docx,.doc,.xml,.rtf"
             (change)="onFileChosen($event)" hidden />
      <button pButton type="button" size="small"
              (click)="fileInput.click()"
              [label]="'form_mgmt.editor.btn_import' | translate"></button>
      <button pButton type="button" size="small" [outlined]="true"
              (click)="saveAsSfdt()"
              [label]="'form_mgmt.editor.btn_save_sfdt' | translate"></button>
      <button pButton type="button" size="small" [outlined]="true"
              (click)="saveAsDocx()"
              [label]="'form_mgmt.editor.btn_save_docx' | translate"></button>
      <span class="spacer"></span>
      <button pButton type="button" size="small" [text]="true"
              (click)="reloadMetadata()"
              [label]="'form_mgmt.editor.btn_reload_sidebar' | translate"></button>
    </div>

    <div class="workspace" [class.with-placeholders]="placeholders().length > 0"
         [style.--sidebar-width.px]="sidebarWidth()">
      <app-metadata-sidebar
        class="sidebar"
        [metadata]="metadata()"
        (insert)="onInsertMetadata($event)">
      </app-metadata-sidebar>

      <div class="resizer"
           (mousedown)="onResizerMouseDown($event)"
           [class.dragging]="isResizing()"
           [attr.aria-label]="'form_mgmt.editor.resize_handle' | translate"
           role="separator"
           aria-orientation="vertical">
      </div>

      <div #editorHost
           class="editor-host"
           [class.drop-target]="dropActive()"
           (dragenter)="onDragEnter($event)"
           (dragover)="onDragOver($event)"
           (dragleave)="onDragLeave($event)"
           (drop)="onDrop($event)">
        <ejs-documenteditorcontainer #editor
                                     [enableToolbar]="true"
                                     [enableTrackChanges]="false"
                                     [enableComment]="false"
                                     [restrictEditing]="false"
                                     [showPropertiesPane]="false"
                                     serviceUrl=""
                                     height="100%"
                                     (created)="onEditorCreated()">
        </ejs-documenteditorcontainer>

        @if (mentionActive()) {
          <app-mention-popup #mention
                             [metadata]="metadata()"
                             [anchorTop]="mentionTop()"
                             [anchorLeft]="mentionLeft()"
                             (pick)="confirmMention($event)">
          </app-mention-popup>
        }
      </div>

      @if (placeholders().length > 0) {
        <aside class="placeholders">
          <div class="ph-head">
            <strong>{{ 'form_mgmt.placeholders.title' | translate: { count: placeholders().length } }}</strong>
            <button pButton type="button" size="small" [text]="true"
                    (click)="closePlaceholdersPanel()"
                    [label]="'common.close' | translate"></button>
          </div>
          <p class="hint">{{ 'form_mgmt.placeholders.hint' | translate }}</p>
          <ul class="ph-list">
            @for (p of placeholders(); track $index) {
              <li>
                <button type="button" class="ph-item"
                        [class.active]="activePlaceholderIndex() === $index"
                        (click)="navigateToPlaceholder($index)">
                  <span class="ph-pattern">{{ ('form_mgmt.placeholders.pattern_' + p.pattern) | translate }}</span>
                  <span class="ph-text">{{ truncate(p.text) }}</span>
                </button>
              </li>
            }
          </ul>
        </aside>
      }
    </div>

    @if (status()) {
      <p class="hint subtle">{{ status() }}</p>
    }
  `,
  styles: [`
    :host { display: block; padding: 16px 20px; }
    .page-head h1 { margin: 0 0 4px; font-size: 18px; font-weight: 600; }
    .hint { color: var(--c-text-muted); font-size: 13px; margin: 0 0 12px; }
    .hint.subtle { font-size: 12px; margin-top: 8px; }
    .toolbar { display: flex; gap: 8px; margin-bottom: 12px; align-items: center; flex-wrap: wrap; }
    .toolbar .spacer { flex: 1; }
    .workspace {
      display: grid;
      grid-template-columns: var(--sidebar-width, 280px) 6px 1fr;
      gap: 0;
      height: calc(100vh - 220px);
      min-height: 480px;
    }
    .workspace.with-placeholders {
      grid-template-columns: var(--sidebar-width, 280px) 6px 1fr 300px;
    }
    .sidebar, .placeholders { border-radius: var(--radius); border: 1px solid var(--c-border); overflow: hidden; background: var(--c-surface); }
    .placeholders { margin-left: 12px; }

    /* Draggable resizer giữa sidebar và editor — drag horizontal để chỉnh sidebarWidth. */
    .resizer {
      cursor: col-resize;
      background: transparent;
      position: relative;
      transition: background 0.12s ease;
    }
    .resizer::before {
      content: '';
      position: absolute;
      top: 0; bottom: 0; left: 50%;
      width: 1px;
      background: var(--c-border);
      transform: translateX(-50%);
      transition: background 0.12s ease, width 0.12s ease;
    }
    .resizer:hover::before, .resizer.dragging::before {
      background: var(--c-text);
      width: 2px;
    }
    .resizer.dragging {
      background: rgba(0, 0, 0, 0.04);
    }
    .editor-host {
      border: 1px solid var(--c-border);
      border-radius: var(--radius);
      overflow: hidden;
      position: relative;
      transition: box-shadow 0.12s ease, border-color 0.12s ease;
    }
    .editor-host.drop-target {
      border-color: var(--c-text);
      box-shadow: inset 0 0 0 2px var(--c-text);
    }

    .placeholders { display: flex; flex-direction: column; }
    .ph-head { display: flex; align-items: center; justify-content: space-between; padding: 10px 12px; border-bottom: 1px solid var(--c-border); }
    .ph-head strong { font-size: 13px; font-weight: 600; }
    .placeholders .hint { padding: 6px 12px 0; font-size: 11px; }
    .ph-list { list-style: none; margin: 0; padding: 8px; overflow-y: auto; flex: 1; }
    .ph-list li { margin: 1px 0; }
    .ph-item {
      width: 100%; display: flex; flex-direction: column; gap: 2px;
      padding: 6px 8px; border: 1px solid transparent; background: transparent;
      cursor: pointer; text-align: left; border-radius: var(--radius);
    }
    .ph-item:hover, .ph-item.active { background: var(--c-surface-2); border-color: var(--c-border); }
    .ph-pattern { font-size: 10px; color: var(--c-text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
    .ph-text { font-family: ui-monospace, monospace; font-size: 12px; color: var(--c-text); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

    /* Force-hide Syncfusion property pane (right side TEXT/Paragraph format panel).
       LƯU Ý: KHÔNG hide .e-documenteditor-optionspane — đó là wrapper chứa LUÔN cả canvas editor.
       Đúng selector: .e-de-pane (sibling của .e-documenteditor trong flex parent
       .e-de-tool-ctnr-properties-pane). Cộng với toggle button. */
    :host ::ng-deep .e-de-pane,
    :host ::ng-deep .e-de-ctnr-properties-pane-btn {
      display: none !important;
    }

    /* Force-hide Syncfusion track-changes / review UI để user không vô tình toggle.
       Cấm Review tab, Track Changes pane, Restrict Editing button. */
    :host ::ng-deep .e-de-tc-button,
    :host ::ng-deep .e-de-track-changes,
    :host ::ng-deep .e-de-track-changes-pane,
    :host ::ng-deep .e-de-cmt-pane,
    :host ::ng-deep [aria-label="Track Changes"],
    :host ::ng-deep [aria-label*="Comment"],
    :host ::ng-deep [title="Track Changes"] {
      display: none !important;
    }

    @media (max-width: 1200px) {
      .workspace.with-placeholders { grid-template-columns: 240px 1fr; }
      .placeholders { grid-column: 1 / -1; height: 240px; }
    }
    @media (max-width: 900px) {
      .workspace, .workspace.with-placeholders { grid-template-columns: 1fr; height: auto; }
      .sidebar { height: 320px; }
      .editor-host { height: calc(100vh - 560px); min-height: 360px; }
      .placeholders { height: 240px; margin-left: 0; }
      /* Resizer chỉ hợp lý ở desktop grid 2-3 cột — hide ở mobile stack. */
      .resizer { display: none; }
    }
  `]
})
export class TemplateEditorPageComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('editor') private editorRef?: DocumentEditorContainerComponent;
  @ViewChild('editorHost') private editorHostRef?: ElementRef<HTMLDivElement>;
  @ViewChild('fileInput') private fileInputRef?: ElementRef<HTMLInputElement>;
  @ViewChild('mention') private mentionRef?: MentionPopupComponent;

  private domKeyDownListener?: (e: KeyboardEvent) => void;
  private iframeObserver?: MutationObserver;
  private attachedIframes = new WeakSet<HTMLIFrameElement>();

  private readonly metadataApi = inject(MetadataService);
  private readonly templateApi = inject(TemplateService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);

  readonly status = signal<string>('');
  readonly metadata = signal<MetadataDto[]>([]);
  readonly dropActive = signal(false);

  // @mention state
  readonly mentionActive = signal(false);
  readonly mentionTop = signal(48);
  readonly mentionLeft = signal(48);
  private mentionQuery = '';

  // Placeholders panel state
  readonly placeholders = signal<DetectedPlaceholder[]>([]);
  readonly activePlaceholderIndex = signal<number>(-1);

  // Resizer state — width sidebar có thể chỉnh giữa min 200px - max 600px.
  private static readonly SIDEBAR_MIN = 200;
  private static readonly SIDEBAR_MAX = 600;
  readonly sidebarWidth = signal<number>(280);
  readonly isResizing = signal<boolean>(false);
  private resizeStartX = 0;
  private resizeStartWidth = 0;
  private resizeMoveListener?: (e: MouseEvent) => void;
  private resizeUpListener?: () => void;

  private dropEnterCount = 0;

  private get container(): SfDocEditorContainer | undefined {
    return this.editorRef as unknown as SfDocEditorContainer | undefined;
  }

  ngOnInit(): void {
    this.reloadMetadata();
  }

  ngAfterViewInit(): void {
    // KHÔNG attach listener ở đây — chờ (created) event của Syncfusion (xem onEditorCreated).
    // documentEditor.openBlank() cũng dời sang onEditorCreated vì editor chưa init xong ở AfterViewInit.
  }

  ngOnDestroy(): void {
    if (this.domKeyDownListener) {
      document.removeEventListener('keydown', this.domKeyDownListener, true);
      // Cũng remove khỏi tất cả iframe contentDocument đã attach.
      const host = this.editorHostRef?.nativeElement;
      host?.querySelectorAll('iframe').forEach((ifr) => {
        try { ifr.contentDocument?.removeEventListener('keydown', this.domKeyDownListener!, true); } catch { /* cross-origin */ }
      });
    }
    this.iframeObserver?.disconnect();
    // Cleanup resize listeners nếu component destroy giữa drag.
    if (this.resizeMoveListener) document.removeEventListener('mousemove', this.resizeMoveListener);
    if (this.resizeUpListener) document.removeEventListener('mouseup', this.resizeUpListener);
    this.container?.destroy?.();
  }

  /**
   * Editor đã render xong. Init blank document + tắt track changes + attach DOM keydown listener
   * trên editor host (capture phase) để intercept @mention trước khi Syncfusion handle.
   */
  onEditorCreated(): void {
    const c = this.container;
    if (!c) return;

    c.documentEditor.openBlank();
    this.disableTrackChanges();
    this.attachDomKeyDownListener();
  }

  private disableTrackChanges(): void {
    const ed = this.container?.documentEditor as unknown as {
      enableTrackChanges?: boolean;
      revisions?: { acceptAll?(): void };
      commentReviewPane?: { showHidePane?(show: boolean, tab?: string): void };
    } | undefined;
    if (!ed) return;
    ed.enableTrackChanges = false;
    // Accept toàn bộ pending revisions có sẵn (DOCX import có track-changes ON từ Word gốc).
    try { ed.revisions?.acceptAll?.(); } catch { /* no revisions */ }
    // Force-close Review pane — pane mở/đóng độc lập với enableTrackChanges flag.
    try { ed.commentReviewPane?.showHidePane?.(false, 'Changes'); } catch { /* pane chưa init */ }
  }


  private attachDomKeyDownListener(): void {
    const host = this.editorHostRef?.nativeElement;
    if (!host) return;

    // ROOT CAUSE (v33): Syncfusion's text input là <iframe class="e-de-text-target"> —
    // keystrokes fire trong iframe.contentDocument, KHÔNG bubble ra parent document.
    // Phải attach vào TỪNG iframe contentDocument. Iframe được tạo async sau editor created
    // → dùng MutationObserver bắt iframe mới + scan iframe đã có sẵn.
    this.domKeyDownListener = (e: KeyboardEvent) => {
      this.zone.run(() => {
        this.handleKeyDown(e);
        this.cdr.markForCheck();
      });
    };

    // Backup: attach lên outer document (catch @ ngoài iframe — vd: editor chưa init iframe).
    document.addEventListener('keydown', this.domKeyDownListener, true);

    const attachToIframe = (ifr: HTMLIFrameElement) => {
      if (this.attachedIframes.has(ifr)) return;
      const tryAttach = () => {
        try {
          const doc = ifr.contentDocument;
          if (!doc) return false;
          doc.addEventListener('keydown', this.domKeyDownListener!, true);
          this.attachedIframes.add(ifr);
          return true;
        } catch { return false; }
      };
      // Iframe có thể chưa load contentDocument → retry sau load event.
      if (!tryAttach()) {
        ifr.addEventListener('load', () => tryAttach(), { once: true });
      }
    };

    // Scan iframe đã có sẵn.
    host.querySelectorAll('iframe').forEach((ifr) => attachToIframe(ifr as HTMLIFrameElement));

    // Observe DOM changes — Syncfusion có thể replace iframe khi reload document.
    this.iframeObserver = new MutationObserver((mutations) => {
      for (const m of mutations) {
        m.addedNodes.forEach((n) => {
          if (n instanceof HTMLIFrameElement) attachToIframe(n);
          else if (n instanceof HTMLElement) {
            n.querySelectorAll('iframe').forEach((ifr) => attachToIframe(ifr as HTMLIFrameElement));
          }
        });
      }
    });
    this.iframeObserver.observe(host, { childList: true, subtree: true });
  }

  reloadMetadata(): void {
    this.metadataApi.search().subscribe({
      next: (list) => {
        this.metadata.set(list);
        this.cdr.markForCheck();
      }
    });
  }

  /**
   * Import flow Phase 6:
   *   .sfdt → load JSON trực tiếp client-side
   *   .docx → Syncfusion editor.open(file) load client-side + parallel POST BE detect placeholder
   *   .xml  → tương tự .docx; BE phân biệt qua extension
   */
  onFileChosen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (ext === 'sfdt') {
      const reader = new FileReader();
      reader.onload = () => {
        const sfdt = reader.result as string;
        this.container?.documentEditor?.open(sfdt);
        this.disableTrackChanges();
        this.placeholders.set([]);
        this.activePlaceholderIndex.set(-1);
        this.status.set(`Loaded ${file.name}`);
        this.cdr.markForCheck();
      };
      reader.readAsText(file);
    } else if (ext === 'docx' || ext === 'doc' || ext === 'xml' || ext === 'rtf') {
      // BE convert DOCX/DOC/Word 2003 XML/RTF → SFDT bằng Syncfusion.EJ2.WordEditor.
      // FE Syncfusion DocumentEditor v33 không có client-side converter cho binary formats,
      // nên phải qua BE — open(sfdt string) thay vì open(file).
      this.status.set(`Đang import ${file.name}…`);
      this.templateApi.importFromWord(file).subscribe({
        next: (res) => {
          if (res.sfdtContent) {
            this.container?.documentEditor?.open(res.sfdtContent);
            // DOCX import có thể có track-changes flag từ Word gốc → tắt + accept revisions.
            this.disableTrackChanges();
          }
          this.placeholders.set(res.placeholders);
          this.activePlaceholderIndex.set(-1);
          this.status.set(`Loaded ${file.name} — ${res.placeholders.length} placeholders.`);
          this.cdr.markForCheck();
        }
      });
    } else {
      this.status.set(`File .${ext} không hỗ trợ — chỉ chấp nhận .sfdt, .docx, .xml.`);
    }

    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  saveAsSfdt(): void {
    this.container?.documentEditor?.save('template', 'Sfdt');
  }

  saveAsDocx(): void {
    this.container?.documentEditor?.save('template', 'Docx');
  }

  onInsertMetadata(m: MetadataDto): void {
    const c = this.container;
    if (!c) return;
    c.documentEditor.focusIn();
    c.documentEditor.editor.insertField(`MERGEFIELD ${m.value}`, `«${m.value}»`);
  }

  // ============== Placeholders panel ==============
  closePlaceholdersPanel(): void {
    this.placeholders.set([]);
    this.activePlaceholderIndex.set(-1);
  }

  navigateToPlaceholder(index: number): void {
    const list = this.placeholders();
    if (index < 0 || index >= list.length) return;
    this.activePlaceholderIndex.set(index);

    const c = this.container;
    if (!c) return;
    // Syncfusion search highlight chấp nhận chuỗi exact. Một số pattern «...» an toàn,
    // dấu chấm có thể match nhiều chỗ → vẫn highlight tất cả, user thấy được ngữ cảnh.
    c.documentEditor.searchModule?.findAll?.(list[index].text);
    c.documentEditor.focusIn();
  }

  truncate(text: string): string {
    return text.length > 28 ? text.slice(0, 28) + '…' : text;
  }

  // ============== Resizer (drag sidebar/editor divider) ==============
  onResizerMouseDown(event: MouseEvent): void {
    event.preventDefault();
    this.resizeStartX = event.clientX;
    this.resizeStartWidth = this.sidebarWidth();
    this.isResizing.set(true);

    // Listener attach lên document để vẫn bắt mousemove khi cursor rời resizer.
    this.resizeMoveListener = (e: MouseEvent) => this.onResizerMouseMove(e);
    this.resizeUpListener = () => this.onResizerMouseUp();
    document.addEventListener('mousemove', this.resizeMoveListener);
    document.addEventListener('mouseup', this.resizeUpListener);
    // Disable text selection trong khi drag.
    document.body.style.userSelect = 'none';
    document.body.style.cursor = 'col-resize';
  }

  private onResizerMouseMove(event: MouseEvent): void {
    const delta = event.clientX - this.resizeStartX;
    const next = this.resizeStartWidth + delta;
    const clamped = Math.max(
      TemplateEditorPageComponent.SIDEBAR_MIN,
      Math.min(TemplateEditorPageComponent.SIDEBAR_MAX, next)
    );
    this.sidebarWidth.set(clamped);
  }

  private onResizerMouseUp(): void {
    this.isResizing.set(false);
    if (this.resizeMoveListener) document.removeEventListener('mousemove', this.resizeMoveListener);
    if (this.resizeUpListener) document.removeEventListener('mouseup', this.resizeUpListener);
    this.resizeMoveListener = undefined;
    this.resizeUpListener = undefined;
    document.body.style.userSelect = '';
    document.body.style.cursor = '';
  }

  // ============== Drag-drop ==============
  onDragEnter(event: DragEvent): void {
    if (!this.hasMetadataPayload(event)) return;
    event.preventDefault();
    this.dropEnterCount++;
    this.dropActive.set(true);
  }
  onDragOver(event: DragEvent): void {
    if (!this.hasMetadataPayload(event)) return;
    event.preventDefault();
    event.dataTransfer!.dropEffect = 'copy';
  }
  onDragLeave(_event: DragEvent): void {
    this.dropEnterCount = Math.max(0, this.dropEnterCount - 1);
    if (this.dropEnterCount === 0) this.dropActive.set(false);
  }
  onDrop(event: DragEvent): void {
    if (!this.hasMetadataPayload(event)) return;
    event.preventDefault();
    this.dropEnterCount = 0;
    this.dropActive.set(false);

    const raw = event.dataTransfer?.getData('application/x-form-mgmt-metadata');
    if (!raw) return;
    try {
      const payload = JSON.parse(raw) as { value: string };
      const c = this.container;
      if (!c) return;
      c.documentEditor.focusIn();
      c.documentEditor.editor.insertField(`MERGEFIELD ${payload.value}`, `«${payload.value}»`);
    } catch {
      // ignore
    }
  }
  private hasMetadataPayload(event: DragEvent): boolean {
    return Array.from(event.dataTransfer?.types ?? []).includes('application/x-form-mgmt-metadata');
  }

  // ============== @mention ==============
  /**
   * DOM keydown listener (capture phase trên editor host) — intercept trước khi Syncfusion handle.
   * `@` mở popup; trong mention mode chặn ArrowUp/Down/Enter/Tab/Escape; chữ/số/_ append vào query.
   */
  private handleKeyDown(e: KeyboardEvent): void {
    if (!this.mentionActive()) {
      // Trigger: '@' (US qwerty), hoặc Shift+Digit2 (defensive cho layout khác).
      const isAtKey = e.key === '@' || (e.shiftKey && (e.code === 'Digit2' || e.key === '2'));
      if (isAtKey) this.openMention();
      return;
    }

    switch (e.key) {
      case 'ArrowDown':
        this.mentionRef?.moveActive(1);
        e.preventDefault();
        e.stopPropagation();
        break;
      case 'ArrowUp':
        this.mentionRef?.moveActive(-1);
        e.preventDefault();
        e.stopPropagation();
        break;
      case 'Enter':
      case 'Tab': {
        const picked = this.mentionRef?.pickActive();
        e.preventDefault();
        e.stopPropagation();
        if (picked) this.confirmMention(picked);
        else this.cancelMention();
        break;
      }
      case 'Escape':
        e.preventDefault();
        e.stopPropagation();
        this.cancelMention();
        break;
      case 'Backspace':
        if (this.mentionQuery.length > 0) {
          this.mentionQuery = this.mentionQuery.slice(0, -1);
          this.mentionRef?.setQuery(this.mentionQuery);
        } else {
          this.cancelMention();
        }
        break;
      case ' ':
      case 'Spacebar':
        this.cancelMention();
        break;
      default:
        if (e.key.length === 1 && /[A-Za-z0-9_]/.test(e.key)) {
          this.mentionQuery += e.key.toUpperCase();
          this.mentionRef?.setQuery(this.mentionQuery);
        } else if (e.key.length === 1) {
          this.cancelMention();
        }
        break;
    }
  }

  private openMention(): void {
    this.mentionQuery = '';
    this.mentionTop.set(48);
    this.mentionLeft.set(48);
    this.mentionActive.set(true);
    setTimeout(() => this.mentionRef?.setQuery(''), 0);
  }

  private cancelMention(): void {
    this.mentionActive.set(false);
    this.mentionQuery = '';
  }

  confirmMention(m: MetadataDto): void {
    const c = this.container;
    if (!c) return;

    const charsToDelete = this.mentionQuery.length + 1;
    const sel = (c.documentEditor as unknown as { selection?: { extendBackward(): void } }).selection;
    if (sel) {
      for (let i = 0; i < charsToDelete; i++) sel.extendBackward();
      (c.documentEditor.editor as unknown as { delete?(): void }).delete?.();
    }

    c.documentEditor.editor.insertField(`MERGEFIELD ${m.value}`, `«${m.value}»`);
    this.cancelMention();
  }
}

// ============== Types ==============
interface SfDocEditorContainer {
  documentEditor: {
    openBlank(): void;
    open(input: string | Blob | File): void;
    save(name: string, format: string): void;
    focusIn(): void;
    enableTrackChanges: boolean;
    revisions?: { acceptAll?(): void; rejectAll?(): void };
    editor: { insertField(code: string, result?: string): void };
    searchModule?: { findAll?(text: string): void };
  };
  destroy(): void;
}
