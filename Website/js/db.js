/**
 * Author: Jayden Wong
 * Date: 13 December 2025
 * Database operations module for PawPalAR. Handles all Firebase Realtime Database
 * interactions including leaderboard subscriptions, player profile fetching, and
 * admin functions for modifying scores and deleting users. Implements data safety
 * by removing sensitive fields and provides aggregated session data for analytics.
 */


import { db } from './config.js';
import { ref, get, child, update, remove, onValue } from "https://www.gstatic.com/firebasejs/10.7.1/firebase-database.js";

// --- Leaderboard ---
// Subscribe to real-time leaderboard updates, sorted by highest affection
export function subscribeToLeaderboard(callback) {
    const lbRef = ref(db, 'leaderboard');
    onValue(lbRef, (snapshot) => {
        const data = snapshot.val();
        if (!data) return callback([]);
        
        const sorted = Object.values(data).sort((a, b) => b.highestAffection - a.highestAffection);
        callback(sorted);
    });
}

// --- Player Profile ---
// Fetch complete user profile including all session history
export async function getFullPlayerProfile(uid) {
    const userRef = ref(db, `users/${uid}`);
    const snapshot = await get(userRef);
    
    if (!snapshot.exists()) return null;
    
    const data = snapshot.val();
    
    // Remove sensitive data that shouldn't be exposed to client
    if (data.profile && data.profile.password) {
        delete data.profile.password;
    }

    return {
        profile: data.profile,
        sessions: data.sessions ? Object.values(data.sessions).sort((a, b) => b.endTime - a.endTime) : []
    };
}

// Fetch all player data from leaderboard for admin panel
export async function getAllLeaderboardEntries() {
    const lbRef = ref(db, 'leaderboard');
    const snapshot = await get(lbRef);
    if (!snapshot.exists()) return [];
    
    // Convert object to array and sort by highestAffection for better display
    const data = snapshot.val();
    const sorted = Object.values(data).sort((a, b) => b.highestAffection - a.highestAffection);
    
    // Return the necessary fields
    return sorted.map(item => ({
        uid: item.userId,
        displayName: item.displayName,
        highestAffection: item.highestAffection
    }));
}

// Admin function: Update specific fields in leaderboard entry
export async function updateLeaderboardEntry(uid, updates) {
    return update(ref(db, `leaderboard/${uid}`), updates);
}

// Admin function: Permanently delete user from both leaderboard and user data
export async function deleteUserStats(uid) {

    // Use multi-path update to delete from both locations atomically
    const updates = {};
    updates[`leaderboard/${uid}`] = null;
    updates[`users/${uid}`] = null;
    return update(ref(db), updates);
}

// Fetch all sessions across all users for admin dashboard analytics
export async function fetchAllUserSessions() {
    const usersRef = ref(db, 'users');
    const snapshot = await get(usersRef);

    if (!snapshot.exists()) return [];

    const allSessions = [];
    const usersData = snapshot.val();
    
    // Iterate through each user's sessions and flatten into single array
    for (const uid in usersData) {
        // Fetch display name for the current user
        const displayName = usersData[uid].profile?.displayName || uid;
        
        if (usersData[uid].sessions) {
            const userSessions = usersData[uid].sessions;
            
            // Iterate through all session IDs for the current user
            for (const sessionId in userSessions) {
                const session = userSessions[sessionId];
                
                // Collect required fields and merge with displayName
                allSessions.push({
                    sessionId: sessionId,
                    uid: uid,
                    displayName: displayName,
                    startTime: session.startTime,
                    endTime: session.endTime,
                    finalAffection: session.finalAffection,
                });
            }
        }
    }
    
    return allSessions;
}