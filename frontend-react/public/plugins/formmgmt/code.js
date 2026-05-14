/* eslint-disable */
// FormMgmt OnlyOffice Bridge Plugin (DS 9.3.x).
//
// @mention design:
//   - DS 9.3.1 native InputHelper bị bug 'innerHTML null' khi setItems → popup KHÔNG render được
//   - Workaround: plugin chỉ DETECT trigger '@' qua onInputHelperInput, gửi 'mention-show' lên host
//   - Host (React) render popup tự, focus shift để Up/Down/Enter/Esc work
//   - User pick → host post 'replace-trigger' → plugin SearchAndReplace
//
// Insert command:
//   - 'insert' từ host → executeMethod('PasteText') tại cursor
(function () {
  'use strict';

  function postToHost(payload) {
    var msg = Object.assign({ src: 'formmgmt-plugin' }, payload);
    try { window.top.postMessage(msg, '*'); }
    catch (e) { try { window.parent.postMessage(msg, '*'); } catch (_) {} }
  }
  function log() { try { console.log.apply(console, ['[FormMgmt Plugin]'].concat([].slice.call(arguments))); } catch (_) {} }

  window.addEventListener('message', function (e) {
    var d = e.data;
    if (!d || d.target !== 'formmgmt-plugin') return;
    log('msg from host:', d.type);
    if (d.type === 'insert') {
      insertAtCursor('«' + String(d.value || '') + '»');
      return;
    }
    if (d.type === 'replace-trigger') {
      replaceTriggerWith(String(d.trigger || ''), String(d.value || ''));
      return;
    }
    if (d.type === 'focus-editor') {
      try { window.Asc.plugin.executeMethod && window.Asc.plugin.executeMethod('SetEditorFocus', []); } catch (_) {}
      return;
    }
  });

  function insertAtCursor(text) {
    if (!text) return;
    try {
      if (typeof window.Asc.plugin.executeMethod === 'function') {
        window.Asc.plugin.executeMethod('PasteText', [text], function (result) { log('PasteText:', result); });
        return;
      }
    } catch (e) {}
  }

  function replaceTriggerWith(trigger, value) {
    log('replaceTriggerWith trigger=', trigger, 'value=', value);
    if (!trigger) return;
    var replacement = '«' + value + '»';
    window.Asc.scope.trigger = trigger;
    // Strategy: Search('@') → find LAST occurrence → Select() → PasteText replace selection.
    // SearchAndReplace API bị bug "Yv" trên DS 9.3.1 với single-char search.
    window.Asc.plugin.callCommand(function () {
      try {
        var oDoc = Api.GetDocument();
        if (typeof oDoc.Search !== 'function') return 'no-search-api';
        var ranges = oDoc.Search(Asc.scope.trigger);
        if (!ranges || ranges.length === 0) return 'no-match';
        var lastRange = ranges[ranges.length - 1];
        if (typeof lastRange.Select !== 'function') return 'no-select';
        lastRange.Select();
        return 'selected:' + ranges.length;
      } catch (e) { return 'err:' + (e && e.message); }
    }, false, false, function (result) {
      log('Search+Select result:', result);
      if (typeof result === 'string' && result.indexOf('selected:') === 0) {
        // Selection đang ở '@' → PasteText sẽ thay '@' bằng '«VALUE»'.
        try {
          window.Asc.plugin.executeMethod('PasteText', [replacement], function (r) {
            log('Replace via PasteText:', r);
          });
        } catch (e) { log('PasteText err:', e && e.message); }
      } else {
        log('cannot select trigger; replace skipped');
      }
    });
  }

  window.Asc.plugin.init = function () {
    var p = window.Asc.plugin;
    log('init editorType=', p.info && p.info.editorType);
    postToHost({ type: 'ready' });
  };

  // OnlyOffice fire khi user gõ word char trong editor. DS 9.3 pass cả '@' trong data.text.
  // Plugin chỉ FORWARD lên host — không tự render popup vì InputHelper bị bug.
  window.Asc.plugin.event_onInputHelperInput = function (data) {
    log('onInputHelperInput', JSON.stringify(data));
    if (!data || data.add === false) {
      postToHost({ type: 'mention-hide' });
      return;
    }
    var text = String(data.text || '');
    if (text.charAt(0) !== '@' || text.indexOf('@', 1) !== -1) {
      // Không bắt đầu '@' hoặc có '@' thứ 2 → không phải trigger mention
      postToHost({ type: 'mention-hide' });
      return;
    }
    postToHost({ type: 'mention-show', trigger: text });
  };

  window.Asc.plugin.event_onInputHelperClear = function () {
    log('onInputHelperClear');
    postToHost({ type: 'mention-hide' });
  };

  // onInputHelperItemClick không fire vì popup native không render. Không cần handle.
})();
