# Unity-DebugMeshRenderer
Debugging render API for Unity.

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

..* Render using game cameras and in builds too if you want

![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/cameraRendering.png?raw=true)

# How to Install?

Import [This](https://github.com/platinio/Unity-ScriptableObjectDatabase/releases/download/1.1/SOD_1.1.unitypackage) Unity package into your project.

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

# Render Mesh

```csharp
MGizmos.RenderMesh(mesh, position, rotation, scale);
```
![alt text](https://github.com/platinio/Unity-MGizmos/blob/main/ReadmeResources/meshExample.png?raw=true)

# Enable MGizmos in Builds

Add a new [Scripting Define Symbol](https://docs.unity3d.com/6000.1/Documentation/Manual/custom-scripting-symbols.html) **SHOW_MESH_GIZMOS_IN_BUILD** 
