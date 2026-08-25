using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    /// <summary>
    /// Every mesh MGizmos draws from, generated in code rather than shipped as an asset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The primitives used to come off <see cref="MGizmosRendererConfig"/>: four of them were references
    /// into Unity's built-in resources (<c>guid 0000000000000000e000000000000000</c>) and the arrowhead was
    /// an authored FBX. Both are liabilities in a package that gets dropped into other people's projects -
    /// the built-in file IDs are undocumented and re-point across editor versions, and every asset the
    /// module ships is one more thing a GUID collision can sever from the config. Generating them here
    /// leaves the config holding nothing but the material, the colour and the font.
    /// </para>
    /// <para>
    /// Built once on first use and cached for the session. <c>HideAndDontSave</c> keeps them out of scenes
    /// and builds, and the editor hook below destroys them before a domain reload so a day of recompiles
    /// does not accumulate one orphaned copy of each per reload.
    /// </para>
    /// <para>
    /// Dimensions match Unity's built-in primitives exactly, because the callers were written against them:
    /// the cylinder is 2 units tall and the sphere is 1 unit across, so <c>RenderLine</c> scaling by
    /// <c>d / 2</c> and <c>RenderSphere</c> scaling by <c>radius * 2</c> stay correct. Getting either wrong
    /// halves or doubles every line in the project with nothing logged.
    /// </para>
    /// <para>
    /// Positions and normals only - no UVs, no tangents. The instanced gizmo shader reads position alone,
    /// but <c>SetMaterial</c> lets a caller swap in a lit shader, and a mesh with no normals lights up
    /// black without ever reporting an error.
    /// </para>
    /// </remarks>
    internal static class MGizmoProceduralMeshes
    {
        //one segment count for every round shape, chosen so no primitive is coarser than the asset it
        //replaced (the built-in cylinder ran 20 segments, the arrowhead FBX 32)
        private const int RadialSegments = 32;

        //latitude bands on the sphere; 32 x 16 is the silhouette people are used to from the built-in
        private const int SphereRings = 16;

        //the disc is a flat fill rather than a lit volume, so it can afford to be smoother than the rest
        private const int DiscSegments = 48;

        private static Mesh sphere;
        private static Mesh cube;
        private static Mesh cylinder;
        private static Mesh quad;
        private static Mesh cone;
        private static Mesh disc;

        /// <summary>
        /// A unit sphere centred on the origin, 1 unit in diameter - the built-in sphere's convention, so
        /// callers still scale by <c>radius * 2</c>.
        /// </summary>
        internal static Mesh Sphere
        {
            get
            {
                if (sphere == null) sphere = BuildSphere();
                return sphere;
            }
        }

        /// <summary>
        /// A 1x1x1 cube centred on the origin. Hard-edged: every face carries its own four vertices, so a
        /// lit material shades it as a box rather than a lumpy ball.
        /// </summary>
        internal static Mesh Cube
        {
            get
            {
                if (cube == null) cube = BuildCube();
                return cube;
            }
        }

        /// <summary>
        /// A cylinder about the Y axis, radius 0.5 and <b>2 units tall</b> (y from -1 to 1) - the built-in
        /// cylinder's convention, which <see cref="MGizmos.RenderLine"/> relies on when it scales by
        /// half the distance.
        /// </summary>
        internal static Mesh Cylinder
        {
            get
            {
                if (cylinder == null) cylinder = BuildCylinder();
                return cylinder;
            }
        }

        /// <summary>
        /// A 1x1 quad in the XY plane facing -Z, vertex for vertex the built-in quad.
        /// </summary>
        internal static Mesh Quad
        {
            get
            {
                if (quad == null) quad = BuildQuad();
                return quad;
            }
        }

        /// <summary>
        /// A cone standing on the origin: base circle of radius 0.5 in the XZ plane, tip 1 unit up the
        /// Y axis. Used as the arrowhead.
        /// </summary>
        /// <remarks>
        /// Base-anchored and Y-up on purpose. The FBX this replaced pointed down +Z with its base at the
        /// origin, which is why <c>RenderArrow</c> used to post-multiply a -90 degree X rotation onto an
        /// orientation it had already computed; aligning the cone with the module's own up-axis convention
        /// deletes that correction rather than reproducing it.
        /// </remarks>
        internal static Mesh Cone
        {
            get
            {
                if (cone == null) cone = BuildCone();
                return cone;
            }
        }

        /// <summary>
        /// A filled unit disc in the XZ plane, centred on the origin, radius 1 - scale it to size.
        /// </summary>
        /// <remarks>
        /// Double-sided (two independent fans, one per winding) so a disc laid on the ground reads from
        /// below as well as above. The unlit gizmo shader culls back faces by default, and a range marker
        /// that vanishes when the camera dips under the floor plane looks like a bug, not a culling mode.
        /// The two fans keep separate vertices so each side gets a real normal; sharing them would leave
        /// every normal averaging to zero.
        /// </remarks>
        internal static Mesh Disc
        {
            get
            {
                if (disc == null) disc = BuildDisc();
                return disc;
            }
        }

        private static Mesh BuildSphere()
        {
            //a lat/long grid: SphereRings + 1 rows of RadialSegments + 1 vertices, the last column
            //repeating the first so the seam closes without welding
            const int columns = RadialSegments + 1;
            var vertices = new Vector3[columns * (SphereRings + 1)];
            var normals = new Vector3[vertices.Length];

            for (int ring = 0; ring <= SphereRings; ring++)
            {
                float polar = (float)ring / SphereRings * Mathf.PI;   //0 at the north pole, PI at the south
                float sinPolar = Mathf.Sin(polar);
                float cosPolar = Mathf.Cos(polar);

                for (int column = 0; column < columns; column++)
                {
                    float azimuth = (float)column / RadialSegments * 2f * Mathf.PI;
                    var direction = new Vector3(sinPolar * Mathf.Cos(azimuth), cosPolar, sinPolar * Mathf.Sin(azimuth));

                    int index = ring * columns + column;
                    //the normal is the direction itself - exact, and free. RecalculateNormals would
                    //average face normals instead and get the seam and the poles subtly wrong
                    normals[index] = direction;
                    vertices[index] = direction * 0.5f;
                }
            }

            //every quad is two triangles except the ones touching a pole, where one of the two collapses
            var triangles = new int[(SphereRings - 1) * RadialSegments * 6];
            int t = 0;

            for (int ring = 0; ring < SphereRings; ring++)
            {
                for (int column = 0; column < RadialSegments; column++)
                {
                    int topLeft = ring * columns + column;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + columns;
                    int bottomRight = bottomLeft + 1;

                    if (ring != 0)
                    {
                        triangles[t++] = topLeft;
                        triangles[t++] = topRight;
                        triangles[t++] = bottomLeft;
                    }

                    if (ring != SphereRings - 1)
                    {
                        triangles[t++] = topRight;
                        triangles[t++] = bottomRight;
                        triangles[t++] = bottomLeft;
                    }
                }
            }

            return Create("MGizmoSphere", vertices, triangles, normals);
        }

        private static Mesh BuildCube()
        {
            //six independent faces, each (outward normal, first in-plane axis, second in-plane axis)
            //ordered so the cross product of the two axes is the outward normal
            var faces = new[]
            {
                (axisU: Vector3.up,      axisV: Vector3.forward, centre: Vector3.right),
                (axisU: Vector3.forward, axisV: Vector3.up,      centre: Vector3.left),
                (axisU: Vector3.forward, axisV: Vector3.right,   centre: Vector3.up),
                (axisU: Vector3.right,   axisV: Vector3.forward, centre: Vector3.down),
                (axisU: Vector3.right,   axisV: Vector3.up,      centre: Vector3.forward),
                (axisU: Vector3.up,      axisV: Vector3.right,   centre: Vector3.back)
            };

            var vertices = new Vector3[faces.Length * 4];
            var triangles = new int[faces.Length * 6];

            for (int f = 0; f < faces.Length; f++)
            {
                Vector3 u = faces[f].axisU * 0.5f;
                Vector3 v = faces[f].axisV * 0.5f;
                Vector3 centre = faces[f].centre * 0.5f;

                int v0 = f * 4;
                vertices[v0 + 0] = centre - u - v;
                vertices[v0 + 1] = centre + u - v;
                vertices[v0 + 2] = centre - u + v;
                vertices[v0 + 3] = centre + u + v;

                int t = f * 6;
                triangles[t + 0] = v0 + 0;
                triangles[t + 1] = v0 + 1;
                triangles[t + 2] = v0 + 2;
                triangles[t + 3] = v0 + 1;
                triangles[t + 4] = v0 + 3;
                triangles[t + 5] = v0 + 2;
            }

            //four unshared vertices per face means RecalculateNormals produces exact face normals, and
            //the cube reads as a box under a lit material instead of a rounded blob
            return Create("MGizmoCube", vertices, triangles);
        }

        private static Mesh BuildCylinder()
        {
            //side rings and cap rings are separate vertices so the rim stays a hard edge: the sides get a
            //smooth radial normal from the shared ring, the caps get a flat +/-Y one
            var vertices = new Vector3[RadialSegments * 4 + 2];
            int sideBottom = 0;
            int sideTop = RadialSegments;
            int capBottom = RadialSegments * 2;
            int capTop = RadialSegments * 3;
            int centreBottom = RadialSegments * 4;
            int centreTop = centreBottom + 1;

            for (int i = 0; i < RadialSegments; i++)
            {
                float radian = (float)i / RadialSegments * 2f * Mathf.PI;
                float x = Mathf.Cos(radian) * 0.5f;
                float z = Mathf.Sin(radian) * 0.5f;

                //2 units tall, matching Unity's built-in cylinder - see the class remarks
                vertices[sideBottom + i] = new Vector3(x, -1f, z);
                vertices[sideTop + i] = new Vector3(x, 1f, z);
                vertices[capBottom + i] = new Vector3(x, -1f, z);
                vertices[capTop + i] = new Vector3(x, 1f, z);
            }

            vertices[centreBottom] = new Vector3(0f, -1f, 0f);
            vertices[centreTop] = new Vector3(0f, 1f, 0f);

            var triangles = new int[RadialSegments * 12];
            int t = 0;

            for (int i = 0; i < RadialSegments; i++)
            {
                int next = (i + 1) % RadialSegments;

                //side quad
                triangles[t++] = sideBottom + i;
                triangles[t++] = sideTop + i;
                triangles[t++] = sideBottom + next;

                triangles[t++] = sideBottom + next;
                triangles[t++] = sideTop + i;
                triangles[t++] = sideTop + next;

                //top cap, wound to face +Y
                triangles[t++] = centreTop;
                triangles[t++] = capTop + next;
                triangles[t++] = capTop + i;

                //bottom cap, wound to face -Y
                triangles[t++] = centreBottom;
                triangles[t++] = capBottom + i;
                triangles[t++] = capBottom + next;
            }

            return Create("MGizmoCylinder", vertices, triangles);
        }

        private static Mesh BuildQuad()
        {
            //vertex for vertex the built-in quad, down to the winding: 1x1 in XY, single-sided facing -Z
            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };

            var triangles = new[] { 0, 3, 1, 3, 0, 2 };

            return Create("MGizmoQuad", vertices, triangles);
        }

        private static Mesh BuildCone()
        {
            //the sides are faceted: each segment owns its three vertices, so RecalculateNormals gives a
            //flat face per panel. A shared apex cannot carry a meaningful normal anyway - the surface
            //points every direction at once there - and the FBX this replaces was faceted too
            var vertices = new Vector3[RadialSegments * 3 + RadialSegments + 1];
            int capRing = RadialSegments * 3;
            int capCentre = capRing + RadialSegments;

            var apex = new Vector3(0f, 1f, 0f);

            for (int i = 0; i < RadialSegments; i++)
            {
                int next = (i + 1) % RadialSegments;
                Vector3 current = PointOnBaseCircle(i);

                vertices[i * 3 + 0] = current;
                vertices[i * 3 + 1] = apex;
                vertices[i * 3 + 2] = PointOnBaseCircle(next);

                vertices[capRing + i] = current;
            }

            vertices[capCentre] = Vector3.zero;

            var triangles = new int[RadialSegments * 6];
            int t = 0;

            for (int i = 0; i < RadialSegments; i++)
            {
                //side panel, wound outward
                triangles[t++] = i * 3 + 0;
                triangles[t++] = i * 3 + 1;
                triangles[t++] = i * 3 + 2;

                //base cap, wound to face -Y
                triangles[t++] = capCentre;
                triangles[t++] = capRing + i;
                triangles[t++] = capRing + (i + 1) % RadialSegments;
            }

            return Create("MGizmoCone", vertices, triangles);
        }

        private static Vector3 PointOnBaseCircle(int segment)
        {
            float radian = (float)segment / RadialSegments * 2f * Mathf.PI;
            return new Vector3(Mathf.Cos(radian) * 0.5f, 0f, Mathf.Sin(radian) * 0.5f);
        }

        private static Mesh BuildDisc()
        {
            //two fans - one up, one down - each with its own centre and rim so the two windings do not
            //share vertices and cancel each other's normals out to zero
            var vertices = new Vector3[(DiscSegments + 1) * 2];
            int topCentre = 0;
            int topRing = 1;
            int bottomCentre = DiscSegments + 1;
            int bottomRing = bottomCentre + 1;

            vertices[topCentre] = Vector3.zero;
            vertices[bottomCentre] = Vector3.zero;

            for (int i = 0; i < DiscSegments; i++)
            {
                float radian = (float)i / DiscSegments * 2f * Mathf.PI;
                var point = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian));

                vertices[topRing + i] = point;
                vertices[bottomRing + i] = point;
            }

            var triangles = new int[DiscSegments * 6];
            int t = 0;

            for (int i = 0; i < DiscSegments; i++)
            {
                int next = (i + 1) % DiscSegments;

                //top face
                triangles[t++] = topCentre;
                triangles[t++] = topRing + next;
                triangles[t++] = topRing + i;

                //bottom face - same fan, reversed winding
                triangles[t++] = bottomCentre;
                triangles[t++] = bottomRing + i;
                triangles[t++] = bottomRing + next;
            }

            return Create("MGizmoDisc", vertices, triangles);
        }

        /// <summary>
        /// Assembles one cached mesh. Pass <paramref name="normals"/> when the exact normal is known
        /// analytically; leave it null to derive face normals from the winding.
        /// </summary>
        private static Mesh Create(string name, Vector3[] vertices, int[] triangles, Vector3[] normals = null)
        {
            var mesh = new Mesh
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                triangles = triangles
            };

            if (normals != null) mesh.normals = normals;
            else mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            return mesh;
        }

#if UNITY_EDITOR
        //HideAndDontSave objects survive a domain reload while the static fields pointing at them do not,
        //so without this every recompile would strand another full set of meshes for the rest of the
        //editor session. Nothing rebuilds them here: the next property access does.
        [UnityEditor.InitializeOnLoadMethod]
        private static void DestroyOnAssemblyReload()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                Destroy(ref sphere);
                Destroy(ref cube);
                Destroy(ref cylinder);
                Destroy(ref quad);
                Destroy(ref cone);
                Destroy(ref disc);
            };
        }

        private static void Destroy(ref Mesh mesh)
        {
            if (mesh == null) return;
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = null;
        }
#endif
    }
}
