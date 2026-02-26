# Fog of War: A Real-World Exploration App

A location-based Android app that gamifies physical exploration using real-time GPS signal processing and a persistent Fog of War overlay.

## Inspiration

I love long walks and exploring my city. However, my inner "optimizer" always ends up defaulting to the exact same, most efficient route every time I leave the house. I built this app to break that habit.

By overlaying a classic video game "Fog of War" onto a real-world map, that dissipates only on explored areas, the goal is to gamify exploration. It provides a visual incentive to take the long way home, take a turn down an unfamiliar street and permanently uncover new parts of the city just for the sake of seeing what's there.

## Tech Stack

- Engine: Unity 6 (Universal Render Pipeline - URP)
- Map Engine: Mapbox Maps SDK for Unity
- Scripting Language: C#
- Platform: Android 

## Core Architecture

- Geospatial Translation: The app ingests continuous WGS84 coordinates (Latitude/Longitude) from the device's GPS and dynamically projects them into Unity's local flat Cartesian space (X, Y, Z) to account for Mapbox's floating origin.
- Hardware Noise Mitigation: The app uses a spatial low-pass filter to account for mobile GPS multipath jitter. It calculates the geodesic distance between pings and only registers a new breadcrumb if the user has physically moved a minimum distance, filtering out stationary drift.
- Decoupled Input Testing: To test without needing to walk outside, I made a custom WASD Location Provider. It injects mock keyboard-driven coordinates into the the Android GPS event stream.
- Custom URP Shader: The visual fog is a custom lightweight HLSL Unlit Shader that maps world-space distances to the player's tracked coordinates, carving smooth, transparent holes into the UI layer at 60fps.

## Current State & Future Features

This repository is currently in the foundational phase. The core tracking, coordinate conversion and real-time visualization loop are complete.

Next up on the roadmap:
- Persistent Serialization: Implementing a JSON save system utilizing "Slippy Map" Web Mercator math (Spatial Hashing) to chunk and store coordinate data efficiently without database bloat.
- Spatial Culling: Tying the active fog data directly to Mapbox events to dynamically load and unload coordinate lists based on the camera's active frustum.
- GPU Optimization: Upgrading the array-based shader to a 2D RenderTexture mask to achieve O(1) rendering cost regardless of how much the user has walked.

## Repository Contents

My personal core logic and systems can be found in Assets/Scripts/.
