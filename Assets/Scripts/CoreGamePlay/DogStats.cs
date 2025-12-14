/// <summary>
/// Author: Jayden Wong
/// Date: 11 December 2025
/// Stores the core state variables for the virtual dog character.
/// Tracks energy (stamina for actions), hunger (feeding requirement),
/// and affection (player's score). All values are designed to be
/// modified by DogController and read by UI systems. Energy and hunger
/// use 0-100 range with Range attribute for inspector visualization.
/// Affection is unbounded and serves as the primary game score.
/// </summary>

using UnityEngine;

public class DogStats : MonoBehaviour
{
    // Dog's energy level: depletes with Play actions, restores with Rest
    // 0 = exhausted (can't play), 100 = fully energized
    [Range(0, 100)] public float energy = 80f;

    // Dog's hunger level: depletes over time, restores with Feed
    // 0 = starving (affects play success), 100 = completely full
    [Range(0, 100)] public float hunger = 80f;

    // Player's affection score: increases with successful actions
    // This is the primary game metric and determines leaderboard ranking
    // Can grow indefinitely - higher is better
    public int affection = 10;
}