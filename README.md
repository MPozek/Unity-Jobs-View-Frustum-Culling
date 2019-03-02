# Unity-Jobs-View-Frustum-Culling

Unity version - 2018.3.6f1

## Used packages

- com.unity.burst - v0.2.4
- com.unity.collections - v0.0.9
- com.unity.jobs - v0.0.7
- com.unity.mathematics - v1.0.0

## Usage

Call the ViewFrustumCulling static class to setup and schedule culling jobs.
For example:

  1. call `ViewFrustumCulling.SetFrustumPlanes` when you move the camera or change FOV passing the camera's culling matrix
  2. call `ViewFrustumCulling.ScheduleCullingJob(positions, extents, outIndices)` passing positions, extents and an empty list where indices into the positions array that pass the cull will be stored
  
A convinience overload that takes the WP matrix is also available if you don't want to worry about reseting the stored frustum planes whenever you move the camera.
