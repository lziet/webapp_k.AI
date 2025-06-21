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
