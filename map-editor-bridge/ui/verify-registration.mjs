import assert from "node:assert/strict";
import React from "react";

globalThis.window = {
  React,
  "cs2/api": { bindValue: (_group, _name, fallback) => ({ value: fallback }) }
};
const { default: register, hasCSS } = await import("./dist/MapEditorPlus.mjs");
assert.equal(hasCSS, true);

const stockGrid = "game-ui/game/components/item-grid/item-grid.tsx";
const assetPath = "game-ui/game/components/asset-menu/asset-menu.tsx";
const movedGrid = "new-ui/item-grid.tsx";
const stockComponent = () => null;

function checkRegistration(gridPaths, expectedOverride, rejectOverride = false) {
  const modules = new Map([[assetPath, { AssetMenu: stockComponent }]]);
  for (const path of gridPaths) modules.set(path, { ItemGrid: stockComponent });
  const overrides = [];
  const hooks = [];
  register({
    get(path, name) {
      const value = modules.get(path)?.[name];
      if (!value) throw new Error("Missing module " + path);
      return value;
    },
    find(query) {
      return [...modules].filter(([path]) => query.test(path))
        .map(([path, exports]) => [path, ...Object.keys(exports)]);
    },
    override(path, name, component) {
      overrides.push(path);
      assert.equal(name, "ItemGrid");
      assert.equal(typeof component, "function");
      if (rejectOverride || !modules.has(path)) throw new Error("Override rejected");
    },
    append(target, component) {
      assert.equal(target, "Editor");
      hooks.push(component);
    }
  });
  assert.deepEqual(overrides, expectedOverride ? [expectedOverride] : []);
  assert.equal(hooks.length, 3);
  assert.equal(hooks[0]().props.runtime.AssetMenu, stockComponent);
}

checkRegistration([stockGrid], stockGrid);
checkRegistration([movedGrid], movedGrid);
checkRegistration([movedGrid, "other/item-grid.tsx"], null);
checkRegistration([], null);
checkRegistration([stockGrid], stockGrid, true);
console.log("Passed stock, moved, ambiguous, missing, and rejected ItemGrid registration.");
