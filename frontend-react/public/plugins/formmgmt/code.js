/* eslint-disable */
// FormMgmt OnlyOffice Bridge Plugin (DS 9.3.x).
//
// Vai trò:
//   1. 'insert' command từ host → executeMethod('PasteText') native cursor-aware paste.
//   2. @mention native: dùng OnlyOffice InputHelper API:
//      - onInputHelperInput fires khi user gõ word → check '@', show popup AT CURSOR
//      - User Up/Down/Enter điều hướng & chọn — OnlyOffice tự xử lý native
//      - onInputHelperItemClick fires khi pick → plugin replace '@trigger' với '«VALUE»'
(function () {
  'use strict';

  /** @type {{value:string,label:string}[]} */
  var metadataList = [];
  var inputHelper = null;
  var currentTriggerQuery = ''; // text user gõ sau '@', đang được filter

  function postToHost(payload) {
    var msg = Object.assign({ src: 'formmgmt-plugin' }, payload);
    try { window.top.postMessage(msg, '*'); }
    catch (e) { try { window.parent.postMessage(msg, '*'); } catch (_) {} }
  }
  function log() { try { console.log.apply(console, ['[FormMgmt Plugin]'].concat([].slice.call(arguments))); } catch (_) {} }

  window.addEventListener('message', function (e) {
    var d = e.data;
    if (!d || d.target !== 'formmgmt-plugin') return;
    if (d.type === 'metadata-list') {
      metadataList = Array.isArray(d.items) ? d.items : [];
      log('metadata loaded:', metadataList.length);
      return;
    }
    if (d.type === 'insert') {
      insertAtCursor('«' + String(d.value || '') + '»');
      return;
    }
  });

  /** Insert text tại cursor native qua executeMethod('PasteText'). */
  function insertAtCursor(text) {
    if (!text) return;
    try {
      if (typeof window.Asc.plugin.executeMethod === 'function') {
        window.Asc.plugin.executeMethod('PasteText', [text], function (result) {
          log('PasteText:', result);
        });
        return;
      }
    } catch (e) {}
    // Fallback callCommand insert.
    window.Asc.scope.text = text;
    window.Asc.plugin.callCommand(function () {
      try {
        var oDoc = Api.GetDocument();
        if (typeof Api.CreateRun === 'function' && typeof oDoc.InsertContent === 'function') {
          var oRun = Api.CreateRun();
          oRun.AddText(Asc.scope.text);
          oDoc.InsertContent([oRun], true);
        }
      } catch (_) {}
    }, false);
  }

  /** Backspace n times via PasteText '' — workaround. Thực tế dùng undo hoặc selection delete.
   *  OnlyOffice executeMethod có 'PasteText' nhận string; pass '' không xoá. Thay vì xoá rồi paste,
   *  ta dùng `oDocument.SearchAndReplace` để thay '@query' bằng '«VALUE»' — atomic.
   */
  function replaceTriggerWith(trigger, value) {
    var replacement = '«' + value + '»';
    window.Asc.scope.trigger = trigger;
    window.Asc.scope.replacement = replacement;
    window.Asc.plugin.callCommand(function () {
      try {
        var oDoc = Api.GetDocument();
        if (typeof oDoc.SearchAndReplace === 'function') {
          oDoc.SearchAndReplace({ SearchString: Asc.scope.trigger, ReplaceString: Asc.scope.replacement, MatchCase: true });
        }
      } catch (_) {}
    }, false);
  }

  // ===== InputHelper native popup =====
  window.Asc.plugin.init = function () {
    var p = window.Asc.plugin;
    log('init editorType=', p.info && p.info.editorType);
    postToHost({ type: 'ready' });
    postToHost({ type: 'request-metadata' });

    try {
      inputHelper = p.createInputHelper();
      log('createInputHelper ok =', !!inputHelper);
    } catch (e) { log('createInputHelper err:', e && e.message); }
  };

  // Fires khi user gõ một word trong doc. data.text = word đang gõ (chưa hoàn chỉnh).
  // data.add = true (gõ thêm chars), false (xoá / cancel).
  window.Asc.plugin.event_onInputHelperInput = function (data) {
    log('onInputHelperInput', JSON.stringify(data));
    if (!inputHelper) return;
    if (!data || data.add === false) { inputHelper.unShow && inputHelper.unShow(); currentTriggerQuery = ''; return; }
    var text = String(data.text || '');
    // Trigger chỉ khi word bắt đầu '@'.
    if (text.charAt(0) !== '@') { inputHelper.unShow && inputHelper.unShow(); currentTriggerQuery = ''; return; }
    var query = text.substring(1).toUpperCase();
    currentTriggerQuery = text; // full trigger '@B' để replace sau
    var items = metadataList
      .filter(function (it) {
        return (it.value || '').toUpperCase().indexOf(query) !== -1 ||
               (it.label || '').toUpperCase().indexOf(query) !== -1;
      })
      .slice(0, 8)
      .map(function (it) { return { text: it.value + ' — ' + (it.label || ''), id: it.value }; });
    if (items.length === 0) { inputHelper.unShow && inputHelper.unShow(); return; }
    inputHelper.setItems(items);
    // show(width, isKeyboardUsed). Width 260 đủ cho code + label.
    inputHelper.show(260, false);
  };

  window.Asc.plugin.event_onInputHelperClear = function () {
    log('onInputHelperClear');
    if (inputHelper && inputHelper.unShow) inputHelper.unShow();
    currentTriggerQuery = '';
  };

  // Fires khi user nhấn Enter hoặc click chọn item trong InputHelper popup.
  window.Asc.plugin.event_onInputHelperItemClick = function (data) {
    log('onInputHelperItemClick', JSON.stringify(data));
    if (!data || !data.id) return;
    var trigger = currentTriggerQuery || '@';
    replaceTriggerWith(trigger, data.id);
    if (inputHelper && inputHelper.unShow) inputHelper.unShow();
    currentTriggerQuery = '';
  };
})();
