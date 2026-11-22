"use strict";
const { contextBridge } = require("electron");
contextBridge.exposeInMainWorld("RISE_API_BASE", "http://localhost:5000");