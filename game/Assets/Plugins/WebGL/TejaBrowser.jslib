mergeInto(LibraryManager.library, {
  TejaRegisterBrowser: function (objectName) {
    var receiver = UTF8ToString(objectName);
    if (!Module.tejaStorageQueue) {
      // Unity also calls FS.syncfs directly. Serialize at that shared boundary,
      // retaining populate requests and every caller's completion callback.
      var state = Module.tejaStorageQueue = { original: FS.syncfs, pending: [], running: false, timer: null };
      var pump = function () {
        if (state.running || !state.pending.length) return;
        // Startup may have begun an IDBFS populate before this wrapper existed.
        // Emscripten decrements this counter before invoking its callback.
        if (FS.syncFSRequests > 0) {
          if (state.timer === null) state.timer = setTimeout(function () { state.timer = null; pump(); }, 25);
          return;
        }
        state.running = true;
        var request = state.pending.shift(), completed = false;
        var finish = function (error) {
          if (completed) return;
          completed = true;
          try { request.callback(error); }
          finally { state.running = false; pump(); }
        };
        try { state.original.call(FS, request.populate, finish); }
        catch (error) {
          if (completed) throw error;
          finish(error);
        }
      };
      FS.syncfs = function (populate, callback) {
        if (typeof populate === 'function') { callback = populate; populate = false; }
        state.pending.push({ populate: populate, callback: callback });
        pump();
      };
    }
    if (Module.tejaVisibilityHandler) document.removeEventListener('visibilitychange', Module.tejaVisibilityHandler);
    Module.tejaVisibilityHandler = function () {
      if (document.visibilityState === 'hidden') SendMessage(receiver, 'OnTejaBrowserHidden');
    };
    document.addEventListener('visibilitychange', Module.tejaVisibilityHandler);
  },
  TejaSyncStorage: function (objectName) {
    var receiver = UTF8ToString(objectName);
    Module.tejaSyncPending = true;
    if (Module.tejaSyncRunning) return;
    var flush = function () {
      Module.tejaSyncPending = false;
      Module.tejaSyncRunning = true;
      FS.syncfs(false, function (error) {
        Module.tejaSyncRunning = false;
        if (error) {
          SendMessage(receiver, 'OnTejaBrowserStorageError', 'unavailable');
        }
        // A newer save requested during a failed flush must still get its turn.
        // With no newer request we report the error and allow the next save to retry.
        if (Module.tejaSyncPending) flush();
      });
    };
    flush();
  }
});
