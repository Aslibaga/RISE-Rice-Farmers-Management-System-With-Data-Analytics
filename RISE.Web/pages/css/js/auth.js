/*
 * RISE – Authentication state management
 * Handles login, registration, logout, and session persistence.
 * Works with the RISE.API JWT authentication endpoints.
 */
(function () {
    const API_BASE_URL = window.RISE_API_BASE ? window.RISE_API_BASE + "/api" : "http://localhost:5000/api";
    const TOKEN_KEY = "rise_auth_token";
    const USER_KEY = "rise_auth_user";

    /* --------------------------------------------------------------
       Auth state
       -------------------------------------------------------------- */
    let authToken = localStorage.getItem(TOKEN_KEY) || null;
    let authUser = null;
    try {
        const stored = localStorage.getItem(USER_KEY);
        if (stored) authUser = JSON.parse(stored);
    } catch {}
    let authListeners = [];

    /* --------------------------------------------------------------
       Helpers
       -------------------------------------------------------------- */
    function saveSession(token, user) {
        authToken = token;
        authUser = user;
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        notifyListeners();
    }

    function clearSession() {
        authToken = null;
        authUser = null;
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        notifyListeners();
    }

    function notifyListeners() {
        authListeners.forEach(fn => { try { fn(authUser); } catch {} });
    }

    function onAuthChange(callback) {
        authListeners.push(callback);
        return () => { authListeners = authListeners.filter(f => f !== callback); };
    }

    function getAuthHeaders() {
        const headers = {};
        if (authToken) {
            headers["Authorization"] = `Bearer ${authToken}`;
        }
        return headers;
    }

    async function request(endpoint, options = {}) {
        const url = `${API_BASE_URL}${endpoint}`;
        const method = options.method || "GET";
        const headers = { ...options.headers, ...getAuthHeaders() };
        if (!headers["Accept"]) headers["Accept"] = "application/json";
        if (options.body != null && !headers["Content-Type"]) headers["Content-Type"] = "application/json";

        let response;
        try {
            response = await fetch(url, { ...options, headers });
        } catch (error) {
            throw new Error(`Could not connect to the RISE API at ${API_BASE_URL}. Make sure the API is running.`);
        }

        if (response.status === 401) {
            const text = await response.text();
            let problem = null;
            try { problem = text ? JSON.parse(text) : null; } catch {}
            const message = problem?.detail || problem?.title || text || "Session expired. Please log in again.";
            clearSession();
            throw new Error(message || "Invalid username or password.");
        }

        if (!response.ok) {
            const text = await response.text();
            let problem = null;
            try { problem = text ? JSON.parse(text) : null; } catch {}
            const message = problem?.errors
                ? Object.values(problem.errors).flat().join(" ")
                : problem?.detail || problem?.title || text || `Request failed (${response.status}).`;
            throw new Error(message);
        }

        if (response.status === 204 || response.status === 205) return null;
        const text = await response.text();
        return text.trim() ? JSON.parse(text) : null;
    }

    /* --------------------------------------------------------------
       Public API
       -------------------------------------------------------------- */
    const auth = {
        isAuthenticated() { return authToken !== null && authUser !== null; },
        getToken() { return authToken; },
        getUser() { return authUser; },
        getRole() { return authUser?.role || null; },
        getUserName() { return authUser?.userName || null; },
        getDisplayName() { return authUser?.displayName || null; },
        onAuthChange,

        async login(usernameOrEmail, password) {
            const data = await request("/auth/login", {
                method: "POST",
                body: JSON.stringify({ UsernameOrEmail: usernameOrEmail, Password: password })
            });
            saveSession(data.token, data.user);
            return data;
        },

        async register(email, username, officeDepartment, password, confirmPassword) {
            const data = await request("/auth/register", {
                method: "POST",
                body: JSON.stringify({
                    Email: email,
                    Username: username,
                    OfficeDepartment: officeDepartment,
                    Password: password,
                    ConfirmPassword: confirmPassword
                })
            });
            return data;
        },

        async logout() {
            try {
                await request("/auth/logout", { method: "POST" });
            } catch {}
            clearSession();
        },

        async me() {
            const data = await request("/auth/me");
            saveSession(authToken, data);
            return data;
        },

        async forgotPassword(email) {
            return await request("/auth/forgot-password", {
                method: "POST",
                body: JSON.stringify({ Email: email })
            });
        },

        async resetPassword(token, newPassword, confirmNewPassword) {
            return await request("/auth/reset-password", {
                method: "POST",
                body: JSON.stringify({ Token: token, NewPassword: newPassword, ConfirmNewPassword: confirmNewPassword })
            });
        }
    };

    // Expose globally
    window.RISEAuth = auth;

    // Initialize: try to validate existing session
    if (auth.isAuthenticated()) {
        auth.me().catch(() => {
            // Session invalid, clear it
            clearSession();
        });
    }

})();