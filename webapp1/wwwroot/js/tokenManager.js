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
    if (!token) return;

    const decoded = parseJwt(token);
    if (!decoded?.exp) return;

    const expiry = decoded.exp * 1000;
    console.log("🔐 Token expires at:", new Date(expiry).toUTCString());
    console.log("⏳ Time remaining (seconds):", Math.floor((expiry - Date.now()) / 1000));
}

function renewToken() {
    const refreshToken = sessionStorage.getItem("refreshToken");
    if (!refreshToken) return;

    fetch('/api/users/renew-token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken })
    })
        .then(res => res.json())
        .then(data => {
            if (data?.data?.token) {
                sessionStorage.setItem("jwt", data.data.token);
                scheduleRefresh(data.data.token);
                console.log("🔄 Token renewed.");
            }
        })
        .catch(err => {
            console.error("Token renewal error", err);
        });
}

function scheduleRefresh(token) {
    const decoded = parseJwt(token);
    if (!decoded?.exp) return;

    const msUntilRefresh = (decoded.exp * 1000) - Date.now() - (5 * 60 * 1000);
    if (msUntilRefresh <= 0) return;

    if (refreshTimer) clearTimeout(refreshTimer);
    refreshTimer = setTimeout(() => {
        if (document.visibilityState === 'visible') {
            renewToken();
        }
    }, msUntilRefresh);
}

function checkAndRefreshToken() {
    const token = sessionStorage.getItem("jwt");
    if (!token) return;

    const decoded = parseJwt(token);
    if (!decoded?.exp) return;

    const expiry = decoded.exp * 1000;
    const timeLeft = expiry - Date.now();

    if (timeLeft <= 60 * 1000) {
        console.log("⚠️ Token about to expire — refreshing...");
        renewToken();
    }
}

export function initializeTokenManager() {
    logTokenExpiry();
    if (refreshInterval) clearInterval(refreshInterval);

    refreshInterval = setInterval(() => {
        if (document.visibilityState === 'visible') {
            checkAndRefreshToken();
            logTokenExpiry();
        }
    }, 30000);
}

document.addEventListener("DOMContentLoaded", () => {
    initializeTokenManager();
});
