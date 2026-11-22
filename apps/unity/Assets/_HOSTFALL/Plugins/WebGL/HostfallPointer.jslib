mergeInto(LibraryManager.library, {
  $HostfallPointer: {
    canvas: null,
    wanted: false,
    pending: false,

    release: function () {
      if (document.pointerLockElement === HostfallPointer.canvas)
        document.exitPointerLock();
    },

    request: function () {
      var state = HostfallPointer;
      var canvas = state.canvas;
      if (!state.wanted || state.pending || document.pointerLockElement ||
          !canvas || !canvas.requestPointerLock) return;

      state.pending = true;
      function settled() {
        state.pending = false;
        if (!state.wanted) state.release();
      }

      try {
        var result = canvas.requestPointerLock();
        if (result && typeof result.then === 'function') result.then(settled, settled);
        else settled();
      } catch (error) {
        settled();
      }
    },

    initialize: function (canvas) {
      var state = HostfallPointer;
      if (state.canvas === canvas || !canvas) return;
      state.canvas = canvas;
      canvas.addEventListener('pointerdown', function (event) {
        if (event.button === 0) state.request();
      });
      canvas.addEventListener('contextmenu', function (event) {
        if (state.wanted) event.preventDefault();
      });
      document.addEventListener('pointerlockchange', function () {
        if (!state.wanted) state.release();
      });
    }
  },

  Hostfall_SetPointerCapture__deps: ['$HostfallPointer'],
  Hostfall_SetPointerCapture: function (wanted) {
    var state = HostfallPointer;
    state.initialize(Module.canvas);
    state.wanted = wanted !== 0;
    if (!state.wanted) state.release();
    else if (navigator.userActivation && navigator.userActivation.isActive) state.request();
  },

  Hostfall_HasPointerCapture__deps: ['$HostfallPointer'],
  Hostfall_HasPointerCapture: function () {
    return HostfallPointer.canvas && document.pointerLockElement === HostfallPointer.canvas ? 1 : 0;
  }
});
