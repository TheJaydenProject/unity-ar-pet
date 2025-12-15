/**
 * Author: Jayden Wong
 * Date: 13 December 2025
 * UI utility module for PawPalAR. Handles dynamic navigation header rendering with
 * auth-based content (login/logout, admin links), toast notification system for
 * user feedback, and helper functions for visual badges. Detects admin privileges
 * and adapts navigation accordingly.
 */


import { auth, ADMIN_UID } from './config.js';
import { signOut } from "https://www.gstatic.com/firebasejs/10.7.1/firebase-auth.js";

// Render navigation header with dynamic content based on auth state and active page
export function renderHeader(activePage = '') {
    const user = auth.currentUser;
    const isAdmin = user && user.uid === ADMIN_UID;
    
    const navHTML = `
    <nav class="fixed top-0 left-0 right-0 z-50 bg-surfaceContainer/90 backdrop-blur-md shadow-sm border-b border-outline/20 px-4 py-3">
        <div class="max-w-5xl mx-auto flex items-center justify-between">
            <a href="leaderboard.html" class="text-2xl font-bold text-primary tracking-tight hover:scale-105 transition-transform">
                🐾 PawPal<span class="text-textSecondary">AR</span>
            </a>
            
            <div class="hidden md:flex items-center gap-2">
                <a href="leaderboard.html" class="px-4 py-2 rounded-pill text-sm transition-colors 
                    ${activePage === 'leaderboard' 
                        ? 'bg-primary text-onPrimary shadow-md font-bold'
                        : 'text-textPrimary hover:bg-primary/10 font-bold'}">Leaderboard</a>
                
                <a href="player.html" class="px-4 py-2 rounded-pill text-sm transition-colors 
                    ${activePage === 'player' 
                        ? 'bg-primary text-onPrimary shadow-md font-bold' 
                        : 'text-textPrimary hover:bg-primary/10 font-bold'}">Player Lookup</a>
                
                ${isAdmin ? 
                    `<a href="admin.html" class="px-4 py-2 rounded-pill text-sm transition-colors 
                        ${activePage === 'admin' 
                            ? 'bg-accent text-white shadow-md font-bold' 
                            : 'text-accent hover:bg-accent/10 font-bold'}">Admin Board</a>` 
                    : ''}
            </div>

            <div class="flex items-center gap-3">
                ${user ? `
                    <div class="hidden md:block text-sm font-medium text-textSecondary bg-surfaceLow/50 px-3 py-1 rounded-pill border border-outline/30">
                        ${user.displayName || user.email}
                    </div>
                    <button id="logoutBtn" class="text-sm font-bold text-primary hover:underline">Logout</button>
                ` : `
                    <a href="index.html" class="bg-primary text-onPrimary px-5 py-2 rounded-full text-sm font-bold shadow-sm hover:shadow-md hover:-translate-y-0.5 active:scale-95 transition-all">Login</a>
                `}
            </div>
        </div>
    </nav>
    <div class="h-20"></div> `;

    document.body.insertAdjacentHTML('afterbegin', navHTML);

    if (document.getElementById('logoutBtn')) {
        document.getElementById('logoutBtn').addEventListener('click', () => {
            signOut(auth).then(() => window.location.href = 'index.html');
        });
    }
}

// Display temporary toast notification
export function showToast(message, type = 'info') {
    const colors = type === 'error' ? 'bg-red-100 text-red-800 border-red-200' : 'bg-surfaceContainer text-primary border-outline';
    const el = document.createElement('div');
    el.className = `fixed bottom-6 right-6 px-6 py-3 rounded-card shadow-lg border ${colors} z-50 animate-bounce-in`;
    el.innerText = message;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 3000);
}

export function getActionBadge(action, result) {
    const isSuccess = result === 'Success';
    const colorClass = isSuccess ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800';
    return `<span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${colorClass}">${action}</span>`;
}