# Unity-MGizmos
A runtime debug rendering API for drawing shapes, lines, arrows, and physics visualizations directly in the game view.

# Overview
Unity's built-in Gizmos only work in the Scene view and must be redrawn every frame. MGizmos solves both problems. You call a static method from anywhere in your code, optionally set a duration, and the shape renders in the game view for as long as you need without any per-frame calls.

It is useful any time you want to visualize positions, directions, rays, collisions, or velocities while the game is running without opening the Scene view or attaching components to objects.

#### Render anywhere you want in your code

```csharp
private void Update()
{
  MGizmos.RenderSphere(transform.position, 1.0f);
}
```

#### Show render for a specific amount of time, there is no need to render each frame

```csharp
private void ShootWeapon()
{
  //Render a line to the target for 1 second
  MGizmos.RenderLine(shootPoint.position, target.position).SetDuration(1.0f);
  //shoot logic
}
```

#### Easy to customize

```csharp
var dc = MGizmos.RenderArrow(ray.origin, rayEndPosition, RaycastStemWidth, RaycastArrowHeadSize);
dc.SetMaterial(myMaterial);
dc.SetColor(myColor);
dc.MaterialPropertyBlock.SetTexture("customTexture", texture);
```

#### Build in API to debug raycast operations

```csharp
Ray ray = new Ray(fromRaycast.position, (toRaycast.position - fromRaycast.position).normalized);
MPhysics.Raycast(ray);
```

![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/raycastExample.png?raw=true)

#### Render using game cameras and in builds too if you want

![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/cameraRendering.png?raw=true)

# How to Install?

Import [This](https://github.com/platinio/Unity-MGizmos/releases/latest/download/MGizmos.unitypackage) Unity package into your project.

# Getting Started

```csharp
//Create your draw call using MGizmos
var drawCall = MGizmos.RenderArrow(from, to);

//modify to your needs
drawCall.SetMaterial(myMaterial).SetColor(myColor);

//add your new draw call to MGizmos drawing queue, once you
//have added the DrawCall into MGizmos it cant longer be edited
MGizmos.AddMeshDrawCall(drawCall);
```
# Render Cylinder

```csharp
MGizmos.RenderCylinder(position, rotation, scale);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/cylinderExample.png?raw=true)

# Render Line

```csharp
MGizmos.RenderLine(from, to, lineWidth);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/lineExample.png?raw=true)

# Render Cube

```csharp
MGizmos.RenderCube(position, rotation, scale);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/cubeExample.png?raw=true)

# Render Quad

```csharp
MGizmos.RenderQuad(position, rotation, scale);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/quadExample.png?raw=true)

# Render Circle

```csharp
MGizmos.RenderCircle(center, sides, radius, lineWidth,upwards);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/circleExample.png?raw=true)

# Render Arrow

```csharp
MGizmos.RenderArrow(from, to, stemWidth, arrowHeadSize);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/arrowExample.png?raw=true)

# Render Disc

A filled disc, facing along `upwards` (default up). Double-sided, so a marker laid on the ground stays
visible from below. Scale the alpha of its colour down and overlap several to paint a heatmap.

```csharp
MGizmos.RenderDisc(center, radius, upwards);
```

# Render Cross

An X of two crossed lines in the plane perpendicular to `upwards` - the universal "ruled out" marker,
kept visually distinct from a sphere so exclusion never reads as just another sample.

```csharp
MGizmos.RenderCross(center, size, lineWidth, upwards);
```

# Render Bar

A square column standing on `basePosition`, growing along `direction` (default up). Base-anchored on
purpose - the built-in cube is centre-anchored, and offsetting a centre by half a height is the
arithmetic everyone visualizing a value field gets wrong once. A negative height grows the other way.

```csharp
MGizmos.RenderBar(basePosition, height, width);
```

# Retained pictures: MGizmoGroup

Every `Render*` call above is timed: it shows for `SetDuration` seconds (or one frame) and expires.
That answers "show this event", but a tool that owns a *picture* of something - a path, a sensor range,
a scored field of candidates - would have to re-issue every call each frame and fight the frame cadence
with durations.

`MGizmoGroup` states the intent directly: everything added to the group draws on every gizmo camera,
every frame, until the group is cleared, rebuilt or disposed.

```csharp
private readonly MGizmoGroup picture = new();

void OnStateChanged()
{
    picture.Clear();
    foreach (var point in points)
    {
        picture.Add(MGizmos.RenderSphere(point.Position, 0.25f).SetColor(point.Color));
    }
}

void OnDestroy() => picture.Dispose();
```

Rules of the road:

- `Add` transfers ownership. Configure a call before or after adding it, but never touch it after the
  group is cleared - released calls are recycled into the draw-call pools.
- Durations are ignored; retained calls do not age.
- Scene loads and play-mode transitions clear every group's *contents* (those positions belong to the
  world that is going away) but keep the group registered, so an `[ExecuteAlways]` owner just rebuilds
  into the same group.
- Dispose the group when its owner goes away for good; an undisposed group keeps drawing forever.
- Rebuilding is cheap: released originals return to the pools, so a steady rebuild loop settles at zero
  allocations.

# Render Mesh

```csharp
MGizmos.RenderMesh(mesh, position, rotation, scale);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/meshExample.png?raw=true)

# Performance and GPU Instancing

Draw calls that share a mesh and a material with **Enable GPU Instancing** turned on are batched into a
handful of `Graphics.DrawMeshInstanced` calls per frame, so drawing hundreds of spheres or lines stays
cheap. The bundled `DefaultMaterial` (shader `ArcaneOnyx/MGizmos/Instanced Unlit`) is set up this way out
of the box, including per-gizmo colors via `SetColor`.

If you pass your own material with `SetMaterial`:

- **Instancing disabled** — the gizmo renders through the classic `Graphics.DrawMesh` path, one draw call
  per gizmo, and `MaterialPropertyBlock` customizations apply as usual.
- **Instancing enabled** — the gizmo joins an instanced batch. Per-gizmo colors then require the shader to
  declare `_Color` as an instanced property (see `MGizmosInstancedUnlit.shader`); shaders without it render
  every instance with the material's own color, and per-draw-call `MaterialPropertyBlock` values are ignored.

# Enable MGizmos in Builds

Add a new [Scripting Define Symbol](https://docs.unity3d.com/6000.1/Documentation/Manual/custom-scripting-symbols.html) **SHOW_MESH_GIZMOS_IN_BUILD** 

# How to Enable MGizmos in Game View

Add component `MGizmoCamera` to your camera, there is also a `MGizmoCameraExecuteAlways` component which will try to also render while the game is not playing.
