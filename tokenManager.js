let refreshTimer = null;
let refreshInterval = null;

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(
            c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
        ).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        console.error("Invalid token", e);
        return null;
    }
}

function logTokenExpiry() {
    const token = sessionStorage.getItem("jwt");
    if (!token) {
        console.warn("No token found in sessionStorage.");
        return;
    }

    const decoded = parseJwt(token);
    if (!decoded || !decoded.exp) {
        console.warn("Token is invalid or missing expiration.");
        return;
    }

    const expiry = decoded.exp * 1000;
    const expiryDate = new Date(expiry);
    const remaining = Math.floor((expiry - Date.now()) / 1000);

    console.log("🔐 Token expires at:", expiryDate.toUTCString());
    console.log("⏳ Time remaining (seconds):", remaining);
}

async function loginAndRefresh() {
    const username = sessionStorage.getItem("username");
    const password = sessionStorage.getItem("password");
    if (!username || !password) return;

    try {
        const response = await fetch('/api/Users/Login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();
        if (data && data.data) {
            const token = data.data;
            sessionStorage.setItem("jwt", token);
            scheduleRefresh(token);
        }
    } catch (err) {
        console.error("Token refresh failed", err);
    }
}

function scheduleRefresh(token) {
    const decoded = parseJwt(token);
    if (!decoded || !decoded.exp) return;

    const expiry = decoded.exp * 1000;
    const now = Date.now();
    const refreshTime = expiry - (5 * 60 * 1000); // 5 minutes before expiration

    const msUntilRefresh = refreshTime - now;
    if (msUntilRefresh <= 0) {
        console.warn("Token already near expiry or expired");
        return;
    }

    if (refreshTimer) clearTimeout(refreshTimer);

    refreshTimer = setTimeout(() => {
        if (document.visibilityState === 'visible') {
            loginAndRefresh();
        }
    }, msUntilRefresh);
}

function checkAndRefreshToken() {
    const token = sessionStorage.getItem("jwt");
    if (!token) {
        console.warn("No token found for scheduled check.");
        return;
    }

    const decoded = parseJwt(token);
    if (!decoded || !decoded.exp) {
        console.warn("Token is invalid or missing exp.");
        return;
    }

    const expiry = decoded.exp * 1000;
    const timeLeft = expiry - Date.now();

    if (timeLeft <= 60 * 1000) { // 1 minute left
        console.log("⚠️ Token about to expire — refreshing...");
        loginAndRefresh();
    }
}

function initializeTokenManager() {
    loginAndRefresh(); // Initial login or refresh
    logTokenExpiry();  // Log immediately on page load

    if (refreshInterval) clearInterval(refreshInterval);

    // Check every 30s
    refreshInterval = setInterval(() => {
        if (document.visibilityState === 'visible') {
            checkAndRefreshToken();
            logTokenExpiry();
        }
    }, 30000);
}

// Auto-run on page load
document.addEventListener("DOMContentLoaded", () => {
    initializeTokenManager();
});
