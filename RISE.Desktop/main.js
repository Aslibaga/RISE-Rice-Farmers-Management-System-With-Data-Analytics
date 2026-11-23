const { app, BrowserWindow, shell } = require("electron");
const path = require("path");
const { spawn } = require("child_process");
const http = require("http");
const fs = require("fs");

let apiProcess = null;

// Connection string is read from server-config.json in the app data folder so every
// installed copy can be pointed at the same shared SQL Server without rebuilding.
function getConnectionString() {
    if (process.env.RISE_DB_CONNECTION) return process.env.RISE_DB_CONNECTION;
    const configPath = path.join(app.getPath("userData"), "server-config.json");
    try {
        const config = JSON.parse(fs.readFileSync(configPath, "utf8"));
        if (config.connectionString) return config.connectionString;
    } catch {}
    const fallback = "Server=(localdb)\\MSSQLLocalDB;Database=RISE_DB;Trusted_Connection=True;TrustServerCertificate=True;";
    try {
        fs.mkdirSync(path.dirname(configPath), { recursive: true });
        fs.writeFileSync(configPath, JSON.stringify({ connectionString: fallback }, null, 2));
    } catch {}
    return fallback;
}

function getApiPath() {
    return app.isPackaged
        ? path.join(process.resourcesPath, "RISE.API", "RISE.API.exe")
        : path.join(__dirname, "..", "RISE.API", "bin", "Debug", "net11.0", "RISE.API.exe");
}

function startApi() {
    const apiPath = getApiPath();
    const env = Object.assign({}, process.env, {
        RISE_DB_CONNECTION: getConnectionString(),
        ASPNETCORE_URLS: "http://localhost:5000"
    });
    apiProcess = spawn(apiPath, [], { env, detached: false, stdio: "ignore" });
    apiProcess.on("exit", (code) => { console.log("API exited:", code); apiProcess = null; });
}

function waitForApi(cb) {
    const check = () => {
        http.get("http://localhost:5000/api/auth/admin-exists", (res) => {
            if (res.statusCode === 200) cb();
            else setTimeout(check, 500);
        }).on("error", () => setTimeout(check, 500));
    };
    check();
}

function createWindow() {
    const webPath = app.isPackaged
        ? path.join(process.resourcesPath, "RISE.Web", "index.html")
        : path.join(__dirname, "..", "RISE.Web", "index.html");

    const mainWindow = new BrowserWindow({
        width: 1440,
        height: 900,
        minWidth: 1100,
        minHeight: 700,
        title: "RISE Desktop",
        autoHideMenuBar: true,
        backgroundColor: "#082116",
        webPreferences: {
            preload: path.join(__dirname, "preload.js"),
            nodeIntegration: false,
            contextIsolation: true,
            sandbox: true
        }
    });

    waitForApi(() => mainWindow.loadFile(webPath));

    mainWindow.webContents.setWindowOpenHandler(({ url }) => {
        if (url.startsWith("https://")) shell.openExternal(url);
        return { action: "deny" };
    });

    mainWindow.webContents.on("will-navigate", (event, url) => {
        if (!url.startsWith("file://")) {
            event.preventDefault();
            if (url.startsWith("https://")) shell.openExternal(url);
        }
    });

    mainWindow.on("closed", () => {
        if (apiProcess) { apiProcess.kill(); apiProcess = null; }
    });
}

app.whenReady().then(() => {
    startApi();
    createWindow();
    app.on("activate", () => {
        if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
});

app.on("window-all-closed", () => {
    if (apiProcess) { apiProcess.kill(); apiProcess = null; }
    if (process.platform !== "darwin") app.quit();
});