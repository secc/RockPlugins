declare module "*.obs" {
    import { DefineComponent } from "vue";

    const Component: DefineComponent<{}, {}, any>;
    export default Component;
}

// The @rockrms/obsidian-framework npm package ships no types for Libs/*, but
// the import maps to RockWeb's bundled Chart.js at runtime. chart.js is a
// devDependency for TYPES ONLY - nothing is bundled from it.
declare module "@Obsidian/Libs/chart" {
    export * from "chart.js";
    export { Chart } from "chart.js";
}
