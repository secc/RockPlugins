module.exports = {
    testEnvironment: "node",
    roots: ["<rootDir>/tests"],
    testMatch: ["**/*.spec.ts"],
    transform: {
        "^.+\\.ts$": ["ts-jest", { tsconfig: "tests/tsconfig.json" }]
    }
};
