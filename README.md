# ParkeFaceAnimation
This Unity project implements a face animation system inspired by Frederic I. Parke's seminal work "Computer Generated Animation of Faces". It captures and replays vertex-level deformations of a mesh using raycasting and barycentric coordinates, allowing for dynamic facial animation directly within the Unity engine.

Key Features:
- <b>Recording & Playback</b>: Capture mesh deformation frames in real time and interpolate them for smooth animation playback.
- <b>Parke-style Interpolation</b>: Includes an implementation of the cosine-based interpolation method proposed by Parke for more natural motion between animation frames.
- <b>Collider-based Sampling</b>: Uses a screen-space quad and raycasting to identify mesh vertices to track.
- <b>Custom Mesh Generation</b>: Builds a simplified animated mesh using sampled surface points and their UV

https://github.com/user-attachments/assets/e66eed7c-5121-4dd4-9bd5-c62755804a35
